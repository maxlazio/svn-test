using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using org.OpenVideoPlayer.Util;
using System.Windows;
using System.Windows.Browser;

namespace org.OpenVideoPlayer.Advertising.MAST {
	/// <summary>
	/// The MAST Engine, also complies as an OVP Silverlight player plug-in
	/// </summary>
	public class Mainsail : IPlugin {

		#region IPlugin Members
		public string PlugInName { get; protected set; }
		public string PlugInDescription { get; protected set; }
		public Version PlugInVersion { get; protected set; }
		public OutputLog Log { get; protected set; }
		#endregion

		#region Other Members

		private IMastAdapter mastInterface;
		protected IMastAdapter MastInterface {
			get { return mastInterface; }
			set { 
				mastInterface = value;
				PollingFrequency = mastInterface.PollingFrequency;
			}
		}

		IMediaControl player = null;
		public IMediaControl Player {
			get { return player; }
			set {
				//this happens on plugin instantiation - we get a reference set to our player
				if (player != value) {
					player = value;
					//set a ref to our interface - this is how triggers will get the vars and events
					if (player is IMastAdapter) {
						MastInterface = player as IMastAdapter;
					} else {
						//if the player doesn't implement the interface, we have an adapter
						MastInterface = new OvpMastAdapter(player);
					}

					//check initparams for anything 'namespaced' to MAST
					foreach (string key in player.StartupArgs.InitParams.Keys) {
						if (!key.ToLower().StartsWith("mast:")) continue;
						//sourceUri will give us the address of a MAST doc
						if (key.ToLower() == "mast:sourceuri") {
							string s = player.StartupArgs.InitParams[key];
							Uri uri = Conversion.GetAbsoluteUri(s);
							AddMASTDoc(uri);
						}
					}
				}
			}
		}

		/// <summary>
		/// The list of triggers we're monitoring
		/// </summary>
		public List<TriggerManager> Triggers = new List<TriggerManager>();

		/// <summary>
		/// Timer to allow us to check property conditions periodically
		/// </summary>
		private Timer timer = null;

		private TimeSpan pollingFrequency = TimeSpan.Zero;
		/// <summary>
		/// Check frequency of the timer
		/// </summary>
		public TimeSpan PollingFrequency {
			get { return pollingFrequency; }
			set {
				if (pollingFrequency != value) {
					pollingFrequency = value;
					//check for crazy values.. - plus send to logs.
					if (pollingFrequency < TimeSpan.FromMilliseconds(100) || pollingFrequency > TimeSpan.FromSeconds(1)) {
						Log.Output(OutputType.Debug, string.Format("Warning - polling frequencies lower than 100ms or higher than 1s are not reccomended.  Current: {0}", pollingFrequency));
					}
					//use this as an opportunity to update the timer
					if (timer == null) {
						timer = new Timer(new TimerCallback(OnTimer));
					}
					if (value > TimeSpan.Zero) {
						timer.Change(value, value);
					} else {
						timer.Change(Timeout.Infinite, Timeout.Infinite);
					}
				}
			}
		}

		public bool UseDispatcherThread { get; set; }

		#endregion

		public Mainsail() {
			//plug-in stuff
			PlugInName = "Mainsail";
			PlugInDescription = "An implementation of a MAST Sequencing Engine";
			PlugInVersion = ReflectionHelper.GetAssemblyVersion();
			Log = new OutputLog(PlugInName);
			UseDispatcherThread = true;
		}

		private void OnTimer(object state) {
			//avoid stacking up threads here
			if (!Monitor.TryEnter(timer)) return;
			try {
				foreach (TriggerManager tm in Triggers) {
					if (UseDispatcherThread) {
						Async.UI(delegate { Evaluate(tm); }, Player as DependencyObject, true, ReenteranceMode.Bypass);
					} else {
						Evaluate(tm);
					}
				}
			} catch (Exception ex) {
				Log.Output(OutputType.Error, "Timer Error", ex);
			} finally {
				Monitor.Exit(timer);
			}
		}

		private void Evaluate(TriggerManager tm) {
			try {
				//evaluate each trigger.   - we're just giving it the context to execute
				//If it succeeds, it will take care of itself and fire event if ready
				tm.Evaluate();

			} catch (Exception ex) {
				//TODO - throttle this, and some other messages
				//important to catch these exceptions, so one trigger failing doesn't muss the whole thing up
				Log.Output(OutputType.Error, "Error in trigger evaluation: " + tm.Trigger.id, ex);
			}
		}

		void OnTriggerActivate(object sender, EventArgs e) {
			//a trigger is letting us know it's ready.  Send it to the MAST/Player interface
			TriggerManager tm = sender as TriggerManager;
			MastInterface.ActivateTrigger(tm.Trigger);
			Log.Output(OutputType.Debug, string.Format("MAST Trigger activated: {0}, {1}", tm.Trigger.id, tm.Trigger.description));
		}

		void OnTriggerDeactivate(object sender, EventArgs e) {
			TriggerManager tm = sender as TriggerManager;
			MastInterface.DeactivateTrigger(tm.Trigger);
		}

		#region Add/remove MAST docs and triggers

		public void AddMASTDoc(Uri mastUri) {
			if (mastUri == null) throw new NullReferenceException("Mast URI cannot be null");
			WebClient wc = new WebClient();
			wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(MastDownloadCompleted);
			wc.DownloadStringAsync(mastUri);
		}

		void MastDownloadCompleted(object sender, DownloadStringCompletedEventArgs e) {
			AddMastDoc(e.Result);
		}

		public void AddMastDoc(string xml) {
			if (xml == null) {
				throw new NullReferenceException("Mast doc cannot be null");
			}

			MAST mast;
			Exception ex;
			if (MAST.Deserialize(xml, out mast, out ex) && mast != null) {
				AddMastDoc(mast);
				return;
			}

			if (ex != null) {
				throw new Exception("Failed to deserialize Mast doc.", ex);
			}
			throw new Exception("Unknown error deserializing doc");
		}

		public void AddMastDoc(MAST mast) {
			if (mast == null || mast.triggers == null) {
				throw new NullReferenceException("Mast doc/triggers cannot be null");
			}

			foreach (Trigger t in mast.triggers) {
				AddMastTrigger(t);
			}
		}

		public void AddMastTrigger(Trigger t) {
			TriggerManager tm = new TriggerManager(t, MastInterface);
			Log.Output(OutputType.Debug, "Added new MAST trigger: " + t.id + ", " + t.description);
			Triggers.Add(tm);
			HookUpTrigger(tm);
		}

		public void RemoveTrigger(string id) {
			lock (Triggers) {
				for (int x = Triggers.Count - 1; x >= 0; x++) {
					if (Triggers[x].Trigger.id == id) {
						RemoveTrigger(Triggers[x]);
					}
				}
			}
		}

		public void RemoveTrigger(Trigger t) {
			lock (Triggers) {
				for (int x = Triggers.Count - 1; x >= 0; x++) {
					if (Triggers[x].Trigger == t) {
						RemoveTrigger(Triggers[x]);
					}
				}
			}
		}

		public void RemoveTrigger(TriggerManager tm) {
			lock (Triggers) {
				if (Triggers.Contains(tm)) {
					Triggers.Remove(tm);
					UnHookTrigger(tm);
				}
			}
		}

		protected void HookUpTrigger(TriggerManager tm) {
			tm.Activate += new EventHandler(OnTriggerActivate);
			tm.Deactivate += new EventHandler(OnTriggerDeactivate);
		}		

		protected void UnHookTrigger(TriggerManager tm) {
			tm.Activate -= new EventHandler(OnTriggerActivate);
			tm.Deactivate -= new EventHandler(OnTriggerDeactivate);
			tm.Dispose();
		}

		#endregion		
	}
}
