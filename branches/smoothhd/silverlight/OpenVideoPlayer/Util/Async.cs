﻿// Copyright (c) 2008 CodeToast.com and Nicholas Brookins
//This code is free to use in any application for any use if this notice is left intact.
//Just don't sue me if it gets you fired.  Enjoy!

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;


namespace org.OpenVideoPlayer.Util {
	public static class Async {

		static Dictionary<string, object> methodLocks = new Dictionary<string, object>();
		static Dictionary<object, TimerEx> timers = new Dictionary<object, TimerEx>();

		#region Async 'Do' overloads, for ease of use
		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed. 
		/// This overload always tries the ThreadPool and DOES NOT check for reentrance.
		/// </summary>
		/// <param name="d">A delegate with a return value of some sort - can be cast to (DlgR) from an anonymous delgate with a return: Async.Do((DlgR)MyMethod);</param>
		/// <param name="getRetVal">If true, and the method/delgete returns something, it is included in the AsyncRes returned (after the method completes)</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, etc.</returns>
		public static AsyncRes Do(DlgR d, bool getRetVal) {
			return Do(d, getRetVal, ReenteranceMode.Allow);
		}

		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed. 
		/// This overload always tries the ThreadPool and DOES NOT check for reentrance.
		/// </summary>
		/// <param name="d">A void delegate - can be cast to (Dlg) from an anonymous delgate or method:  Async.Do((Dlg)MyVoidMethod)</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, etc.</returns>
		public static AsyncRes Do(Dlg d) {
			return Do(d, ReenteranceMode.Allow);
		}

		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed.
		/// </summary>
		/// <param name="d">A delegate with a return value of some sort - can be cast to (DlgR) from an anonymous delgate with a return: Async.Do((DlgR)MyMethod);</param>
		/// <param name="rMode">If true, will make sure no other instances are running your method.</param>
		/// <param name="getRetVal">If true, and the method/delgete returns something, it is included in the AsyncRes returned (after the method completes)</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, resturn and result values, etc.</returns>
		public static AsyncRes Do(DlgR d, bool getRetVal, ReenteranceMode rMode) {
			return Do(d, null, getRetVal, null, true, rMode,  true);
		}

		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed.
		/// </summary>
		/// <param name="d">A void delegate - can be cast to (Dlg) from an anonymous delgate or method:  Async.Do((Dlg)MyVoidMethod);</param>

		/// <param name="rMode">If true, will make sure no other instances are running your method.</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, result values, etc.</returns>
		public static AsyncRes Do(Dlg d, ReenteranceMode rMode) {
			return Do(null, d, false, null, true, rMode,  true);
		}

		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed.
		/// </summary>
		/// <param name="d">A delegate with a return value of some sort - can be cast to (DlgR) from an anonymous delgate with a return: Async.Do((DlgR)MyMethod);</param>
		/// <param name="state">A user object that can be tracked through the returned result</param>
		/// <param name="tryThreadPool">True to use the TP, otherwise just go to a ful lthread - good for long running tasks.</param>
		/// <param name="rMode">If true, will make sure no other instances are running your method.</param>
		/// <param name="getRetVal">If true, and the method/delgete returns something, it is included in the AsyncRes returned (after the method completes)</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, resturn and result values, etc.</returns>
		public static AsyncRes Do(DlgR d, bool getRetVal, object state, bool tryThreadPool, ReenteranceMode rMode) {
			return Do(d, null, getRetVal, state, tryThreadPool, rMode,  true);
		}

		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed.
		/// </summary>
		/// <param name="d">A void delegate - can be cast to (Dlg) from an anonymous delgate or method:  Async.Do((Dlg)MyVoidMethod);</param>
		/// <param name="state">A user object that can be tracked through the returned result</param>
		/// <param name="tryThreadPool">True to use the TP, otherwise just go to a ful lthread - good for long running tasks.</param>
		/// <param name="rMode">If true, will make sure no other instances are running your method.</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, result values, etc.</returns>
		public static AsyncRes Do(Dlg d, object state, bool tryThreadPool, ReenteranceMode rMode) {
			return Do(null, d, false, state, tryThreadPool, rMode,  true);
		}
		#endregion

		#region The Big Main private 'Do' method - called by all overloads.
		/// <summary>
		/// Fires off your delegate asyncronously, using the threadpool or a full managed thread if needed.
		/// </summary>
		/// <param name="d">A void delegate - can be cast to (Dlg) from an anonymous delgate.</param>
		/// <param name="dr">A delegate with a return value of some sort - can be cast to (DlgR) from an anonymous delgate with a return.</param>
		/// <param name="state">A user object that can be tracked through the returned result</param>
		/// <param name="getRetVal">If true, and the method/delgete returns something, it is included in the AsyncRes returned (after the method completes)</param>
		/// <param name="tryThreadPool">True to use the TP, otherwise just go to a ful lthread - good for long running tasks.</param>
		/// <param name="rMode">If true, will make sure no other instances are running your method.</param>
		/// <returns>AsyncRes with all kind o' goodies for waiting, result values, etc.</returns>
		private static AsyncRes Do(DlgR dr, Dlg d, bool getRetVal, object state, bool tryThreadPool, ReenteranceMode rMode, bool async) {
			//get a generic MethodInfo for checks..
			MethodInfo mi = ((dr != null) ? dr.Method : d.Method);
			//make a unique key for output usage
			string key = string.Format("{0}{1}{2}{3}", ((getRetVal) ? "<-" : ""), mi.DeclaringType, ((mi.IsStatic) ? ":" : "."), mi.Name);
			//our custom return value, holds our delegate, state, key, etc.
			AsyncRes res = new AsyncRes(state, ((dr != null) ? (Delegate)dr : (Delegate)d), key, rMode);

			//Create a delegate wrapper for what we will actually invoke..
			Dlg dlg = (Dlg)delegate {
				if (!BeforeInvoke(res)) return; //checks for reentrance issues and sets us up
				try {
					if (res.IsCompleted) return;
					if (dr != null) {
						res.retVal = dr();//use this one if theres a return
					} else {
						d();//otherwise the simpler dlg
					}
				} catch (Exception ex) { //we never want a rogue exception on a random thread, it can't bubble up anywhere
					Console.WriteLine("Async Exception:" + ex);
				} finally {
					FinishInvoke(res);//this will fire our callback if they used it, and clean up
				}
			};

			//if (control != null) {
			//    res.control = control;
			//    res.result = AsyncAction.ControlInvoked;
			//    if (!async) {
			//        if (!control.InvokeRequired) {
			//            res.completedSynchronously = true;
			//            dlg();
			//        } else {
			//            control.Invoke(dlg);
			//        }
			//    } else {
			//        control.BeginInvoke(dlg);
			//    }
			//    return res;
			//} //don't catch these errors - if this fails, we shouldn't try a real thread or threadpool!

			if (tryThreadPool) { //we are going to use the .NET threadpool
				try {
					//get some stats - much better than trying and silently failing or throwing an expensive exception
					//int minThreads, minIO, threads, ioThreads, totalThreads, totalIO;
					//ThreadPool.GetMinThreads(out minThreads, out minIO);
					//ThreadPool.GetAvailableThreads(out threads, out ioThreads);
					//ThreadPool.GetMaxThreads(out totalThreads, out totalIO);

					////check for at least our thread plus one more in ThreadPool
					//if (threads > minThreads) {
						//this is what actually fires this task off..
						bool result = ThreadPool.QueueUserWorkItem(delegate { dlg(); });
						if (result) {
							res.result = AsyncAction.ThreadPool;
							//this means success in queueing and running the item
							return res;
						} else {
							//according to docs, this "won't ever happen" - exception instead, but just for kicks.
							Console.WriteLine( "Failed to queue in threadpool. Method: " + key);
						}
					//} else {
					//    Console.WriteLine(String.Format("Insufficient idle threadpool threads: {0} of {1} - min {2}, Method: {3}", threads, totalThreads, minThreads, key));
					//}
				} catch (Exception ex) {
					Console.WriteLine("Failed to queue in threadpool: " + ex.Message, "Method: " + key);
				}
			}

			//if we got this far, then something up there failed, or they wanted a dedicated thread
			Thread t = new Thread((ThreadStart)delegate { dlg(); }) {IsBackground = true, Name = ("Async_" + key)};
			res.result = AsyncAction.Thread;
			t.Start();

			return res;
		}
		#endregion

		#region Before and after - helper methods

		private static bool BeforeInvoke(AsyncRes res) {
			//if marked as completed then we abort.
			if (res.IsCompleted) return false;
			//if mode is 'allow' there is nothing to check.  Otherwise...
			if (res.RMode != ReenteranceMode.Allow) {
				//be threadsafe with our one and only member field
				lock (methodLocks) {
					if (!methodLocks.ContainsKey(res.Method)) {
						//make sure we have a generic locking object in the collection, it will already be there if we are reentering
						methodLocks.Add(res.Method, new object());
					}
					//if bypass mode and we can't get or lock, we dump out.
					if (res.RMode == ReenteranceMode.Bypass) {
						if (!Monitor.TryEnter(methodLocks[res.Method])) {
							res.result = AsyncAction.Reenterant;
							return false;
						}
					} else {
						//Otherwise in 'stack' mode, we just wait until someone else releases it...
						Monitor.Enter(methodLocks[res.Method]);
					}

					//if we are here, all is good.  
					//Set some properties on the result class to show when we started, and what thread we are on
					res.isStarted = true;
					res.startTime = DateTime.Now;
					res.thread = Thread.CurrentThread;
				}
			}

			return true;
		}

		private static void FinishInvoke(AsyncRes res) {
			if (res == null ) return;
			try {
				//finish a few more properties
				res.isCompleted = true;
				res.completeTime = DateTime.Now;
				//set the resetevent, in case someone is using the waithandle to know when we have completed.
				res.mre.Set();
			} catch (Exception ex) {
				Console.WriteLine("Error setting wait handle on " + (res.Method ?? "NULL") + ex);
			}

			if (res.RMode != ReenteranceMode.Allow) {
				//if mode is bypass or stack, then we must have a lock that needs releasing
				try {
					if (res.Method != null) {
						if (methodLocks.ContainsKey(res.Method)) {
							Monitor.Exit(methodLocks[res.Method]);
						}
					}
				} catch (Exception ex) {
					Console.WriteLine("Error releasing reentrant lock on " + (res.Method ?? "NULL")+ ex);
				}
			}
		}

		#endregion

		#region Timers
		public static void SetTimer(Dlg d, object key, ReenteranceMode rMode, TimeSpan initial, TimeSpan frequency) {
			TimerEx t = null;
			lock (timers) {
				if (timers.ContainsKey(key)) {
					t = timers[key];
					if (d == null) {
						t.Close();
						timers.Remove(key);
					} else {
						t.d = d;
						t.Timer.Change(initial, frequency);
						t.Mode = rMode;
					}
				} else {
					if (d != null) {
						t = new TimerEx(d, rMode, initial, frequency);
						timers.Add(key, t);
					}
				}
			}
		}

		public static void StopTimer(object key) {
			SetTimer(null, key, ReenteranceMode.Bypass, TimeSpan.Zero, TimeSpan.Zero);
		}

		private class TimerEx {
			public System.Threading.Timer Timer = null;
			public ReenteranceMode Mode = ReenteranceMode.Bypass;
			public Dlg d = null;
			private readonly object lck = new object();

			public TimerEx(Dlg d, ReenteranceMode rMode, TimeSpan initial, TimeSpan frequency) {
				this.d = d;
				this.Mode = rMode;
				Timer = new System.Threading.Timer(TimerMethod, null, initial, frequency);
			}

			public void Close() {
				d = null;
				if (Timer != null) Timer.Dispose();
			}

			private void TimerMethod(object o) {
				if (d == null) {
					Console.WriteLine("Empty Delegate, disposing timer.");
					if (Timer != null) Timer.Dispose();
					return;
				}
				ReenteranceMode m = Mode;
				if (m == ReenteranceMode.Bypass) {
					if (!Monitor.TryEnter(lck)) return;
				} else if (m == ReenteranceMode.Stack) {
					Monitor.Enter(lck);
				}
				try {
					//Do stuff
					d();

				} catch (Exception ex) {
					Console.WriteLine("Async Timer Exception:"+ ex);
				} finally {
					if (m != ReenteranceMode.Allow) {
						Monitor.Exit(lck);
					}
				}
			}
		}

		#endregion
	}

	#region AsyncRes class
	/// <summary>
	/// Used with the Async helper class, This class is mostly a holder for a lot of tracking fields and properties, with a few things mandated by the IAsyncResult interface.
	/// </summary>
	public class AsyncRes : IAsyncResult {

		internal AsyncRes(object state, Delegate d, string key, ReenteranceMode rMode) {
			this.state = state;
			this.asyncDelegate = d;
			this.key = key;
			this.RMode = rMode;
		}

		internal ReenteranceMode RMode = ReenteranceMode.Allow;

		internal Thread thread = null;

		private string key = null;
		public string Method { get { return key; } }

		private Delegate asyncDelegate = null;
		public Delegate AsyncDelegate { get { return asyncDelegate; } }

		internal AsyncAction result = AsyncAction.Unknown;
		public AsyncAction Result { get { return result; } }

		internal Control control = null;
		public Control Control { get { return control; } }

		internal DateTime createTime = DateTime.Now;
		public DateTime TimeCreated { get { return createTime; } }

		internal DateTime completeTime = DateTime.MinValue;
		public DateTime TimeCompleted { get { return completeTime; } }

		internal DateTime startTime = DateTime.Now;
		public DateTime TimeStarted { get { return startTime; } }

		public TimeSpan TimeElapsed {
			get { return ((completeTime > DateTime.MinValue) ? completeTime : DateTime.Now) - createTime; }
		}

		public TimeSpan TimeRunning {
			get {return (startTime == DateTime.MinValue) ? TimeSpan.Zero : ((completeTime > DateTime.MinValue) ? completeTime : DateTime.Now) - startTime;}
		}

		internal object retVal = null;
		public object ReturnValue { get { return retVal; } }

		internal bool isStarted = false;
		public bool IsStarted { get { return isStarted; } }

		private object state = null;
		public object AsyncState { get { return state; } }

		/// <summary>
		/// Aborts a running associated thread.  If possible it will cancel if not yet started
		/// </summary>
		/// <returns>True if the thread could be cancelled before it started.</returns>
		public bool CancelOrAbort() {
			isCompleted = true;
			if (!isStarted) return true;//cancelled

			if (thread != null && thread.IsAlive) {
				thread.Abort();
			}

			return false;
		}

		internal ManualResetEvent mre = new ManualResetEvent(false);
		public WaitHandle AsyncWaitHandle { get { return mre; } }

		internal bool completedSynchronously = false;
		public bool CompletedSynchronously { get { return completedSynchronously; } }

		internal bool isCompleted = false;
		public bool IsCompleted { get { return isCompleted; } }
	}
	#endregion

	#region Definitions of enums and delegates

	/// <summary>
	/// Abreviated Empty Delegate for use in anonymous casting
	/// </summary>
	public delegate void Dlg();

	/// <summary>
	/// Abreviated Empty Delegate for use in anonymous methods when a return is needed
	/// </summary>
	/// <returns>Umm, anything you want.</returns>
	public delegate object DlgR();

	public enum AsyncAction {
		Unknown = 0,
		ThreadPool = 1,
		Thread = 2,
		Failed = 4,
		Reenterant = 8,
		ControlInvoked = 16
	}

	public enum ReenteranceMode {
		Allow = 1,
		Bypass = 2,
		Stack = 4,
	}
	#endregion
}
