using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using org.OpenVideoPlayer.Util;
using org.OpenVideoPlayer.Media;
using System.Windows.Threading;

namespace org.OpenVideoPlayer.Advertising.MAST {

	public class OvpMastAdapter : IMastAdapter {

		#region MAST Events

		public event EventHandler OnPlay;

		public event EventHandler OnStop;

		public event EventHandler OnPause;

		public event EventHandler OnMute;

		public event EventHandler OnVolumeChange;

		public event EventHandler OnEnd;//TODO - needs player change

		public event EventHandler OnItemStart;

		public event EventHandler OnItemEnd;

		public event EventHandler OnSeek;

		public event EventHandler OnFullScreenChange;

		public event EventHandler OnError;

		public event EventHandler OnMouseOver; //TODO - more definition

		public event EventHandler OnPlayerSizeChanged;

		#endregion

		#region MAST Properties

		public TimeSpan Duration {
			get {return player.Duration;}
		}

		public TimeSpan Position {
			get {
					return player.Position;
			}
		}

		//TODO - Implement a watch timer
		public TimeSpan WatchedTime {
			get { throw new NotImplementedException(); }
		}

		//TODO - Implement a watch timer
		public TimeSpan TotalWatchedTime {
			get { throw new NotImplementedException(); }
		}

		public DateTime SystemTime {
			get { return DateTime.Now; }
		}

		public bool FullScreen {
			get { return Application.Current.Host.Content.IsFullScreen; }
		}

		public bool IsPlaying {
			get { return player.MediaElement.CurrentState == MediaElementState.Playing; }
		}

		public bool IsPaused {
			get { return player.MediaElement.CurrentState == MediaElementState.Paused; }
		}

		public bool IsStopped {
			get { return player.MediaElement.CurrentState == MediaElementState.Stopped; }
		}

		//TODO - implement captions
		public bool CaptionsActive {
			get { throw new NotImplementedException(); }
		}
		//TODO - implement captions
		public bool HasCaptions {
			get { throw new NotImplementedException(); }
		}

		public bool HasVideo {
			get { return player.MediaElement.NaturalVideoHeight > 0; }
		}

		public bool HasAudio {
			get { return player.MediaElement.AudioStreamCount > 0; }
		}

		private int itemCount = 0;
		public int ItemsPlayed {
			get { return itemCount; }
		}

		public int PlayerWidth {
			get { return (int)player.MediaElementSize.Width; }
		}

		public int PlayerHeight {
			get { return (int)player.MediaElementSize.Height; }
		}

		public int ContentWidth {
			get { return (int)player.VideoResolution.Width; }
		}

		public int ContentHeight {
			get { return (int)player.VideoResolution.Height; }
		}

		public long ContentBitrate {
			get { throw new NotImplementedException(); }
		}

		public string ContentTitle {
			get { return player.CurrentItem.Title; }
		}

		public string ContentUrl {
			get {  return player.CurrentItem.Url; }
		}

		public TimeSpan PollingFrequency { get; set; }

		#endregion

		IMediaControl player = null;
		OutputLog log = new OutputLog("OvpMastAdapter");

		public OvpMastAdapter(IMediaControl player) {
			if (player == null) throw new NullReferenceException("Player cannot be null.");
			this.player = player;
			PollingFrequency = TimeSpan.FromMilliseconds(500);
			HookPlayerEvents();
		}

		#region Active trigger handling

		public List<IMastPayload> ActiveUnits = new List<IMastPayload>();

		public void ActivateTrigger(Trigger t) {
			foreach (IMastSource s in t.Sources) {
				IMastPayload unit = HandleSource(t.id, s);
				if (unit == null) {
					log.Output(OutputType.Error, string.Format("No valid handler found for trigger {0}, source {1} / {2} {3}", t.id, s.format, s.uri??"", s.altReference??""));
				}
			}
		}

		private IMastPayload HandleSource(string id, IMastSource s) {
			//handle each peer source.  Child sources will get handled when the event let's us know these were placed.
			IMastPayload unit = null;
			//look for a loaded plugin that understands this trigger/format
			foreach (IPlugin ip in player.Plugins) {
				if (ip is IMastPayloadHandler) {
					unit = ((IMastPayloadHandler)ip).Handle(s);
					if (unit != null) {
						unit.TriggerId = id;
						unit.Activated += new EventHandler(AdUnit_Activated);
						//if it returns us a payload, it handled it. 
						ActiveUnits.Add(unit);
						return unit;
					}
				}
			}

			return null;
		}

		void AdUnit_Activated(object sender, EventArgs e) {
			IMastPayload mp = sender as IMastPayload;
			if (mp.Source != null && mp.Source.Sources != null) {
				foreach (IMastSource s in mp.Source.Sources) {
					HandleSource(mp.TriggerId, s);
				}
			}
		}

		public void DeactivateTrigger(Trigger t) {
			List<IMastPayload> temp = new List<IMastPayload>();
			//cycle through all the active units, matching the id
			foreach (IMastPayload unit in ActiveUnits) {
				if (unit != null && unit.TriggerId == t.id) {
					unit.Deactivate(); //this will fire an event that it's handler can track 
					temp.Add(unit);
				}
			}
			//now remove them from the list
			foreach (IMastPayload unit in temp) ActiveUnits.Remove(unit);
		}

		#endregion

		#region MAST Event Handling

		void HookPlayerEvents() {
			player.PlaylistIndexChanging += new PlaylistIndexChangingEventHandler(player_PlaylistIndexChanging);
			player.SizeChanged += new SizeChangedEventHandler(player_SizeChanged);
			player.FullScreenChanged += new EventHandler(player_FullScreenChanged);
			player.VolumeChanged += new EventHandler(player_VolumeChanged);
			player.MediaOpened += new EventHandler(player_MediaOpened);
			player.MediaCommand += new EventHandler(player_MediaCommand);
			player.MediaEnded += new EventHandler(player_MediaEnded);
		}

		void player_MediaEnded(object sender, EventArgs e) {
			if (OnItemEnd != null) {
				OnItemEnd(this, e);
			}
		}

		void player_MediaCommand(object sender, EventArgs e) {
			MediaCommandEventArgs mce = e as MediaCommandEventArgs;
			if (mce != null) {
				if (mce.Command == MediaCommandType.Pause && OnPause !=null) {
					OnPause(this, e);
				}
				if (mce.Command == MediaCommandType.Play && OnPlay != null) {
					OnPlay(this, e);
				}
				if (mce.Command == MediaCommandType.Seek && OnSeek != null) {
					OnSeek(this, e);
				}
				if (mce.Command == MediaCommandType.Stop && OnStop != null) {
					OnStop(this, e);
				}
			}
		}

		void player_MediaOpened(object sender, EventArgs e) {
			if (OnItemStart != null) {
				OnItemStart(this, e);
				itemCount++;
			}
		}

		void player_VolumeChanged(object sender, EventArgs e) {
			if (OnVolumeChange != null) {
				OnVolumeChange(this, e);
				if (player.Volume <= 0) {
					OnMute(this, e);
				}
			}
		}

		void player_FullScreenChanged(object sender, EventArgs e) {
			if (OnFullScreenChange != null) {
				OnFullScreenChange(this, e);
			}
		}

		void player_SizeChanged(object sender, SizeChangedEventArgs e) {
			if (OnPlayerSizeChanged != null) {
				OnPlayerSizeChanged(this, e);
			}
		}

		void player_PlaylistIndexChanging(object sender, PlaylistIndexChangingEventArgs args) {

		}
		#endregion

	}
}