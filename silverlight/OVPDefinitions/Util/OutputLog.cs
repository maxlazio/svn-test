using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;


namespace org.OpenVideoPlayer.Util {

	public delegate void OutputHandler(OutputEntry outputEntry);
	public delegate void EmptyHandler();

		/// <summary>
		/// The type of output message.
		/// </summary>
		[Flags]
		public enum OutputType {
			None = 0,
			Info = 1,
			Debug = 2,
			Error = 4,
			Critical = 8,
			//Alert
			//ExtDebug
			//Warning 
			//Trace

			All = Critical | Debug | Error | Info,
		};

	/// <summary>
	/// Helper Class to facilitate logging and output
	/// </summary>
	public class OutputLog {


		#region Members
		//used for static methods..
		private static OutputLog Log = new OutputLog();
		/// <summary>
		/// Event fired when output is created
		/// </summary>
		public event OutputHandler OutputEvent;

		/// <summary>
		/// Event fired when ANY output is created, ideal for chaining all modules back to a central output
		/// </summary>
		public static event OutputHandler StaticOutputEvent;

		/// <summary>
		/// Disable this if you want to redirect everyone elses logs to this one... OR ELSE!
		/// </summary>
		public bool EnableStaticLogging = true;

		/// <summary>
		/// Which types to log, and what to ignoe
		/// </summary>
		public static OutputType EnabledTypes = OutputType.All;// ^ OutputType.Trace;

		/// <summary>
		/// Module/Source name to automatically use
		/// </summary>
		public string ModuleName = "";

		public bool QueueEnabled;

		//public ILog MainOutputPlugin = null; 

		/// <summary>
		/// Maximum message level to let pass.
		/// </summary>
		public int MaxMessageLevel = 100;

		/// <summary>
		/// Stream to send all parsed output to
		/// </summary>
		public Stream OutputStream;

		/// <summary>
		/// Automatically flush stream after each message
		/// </summary>
		public TimeSpan FlushStreamPeriod = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Keep exceptions from bubbling up and messing up your day.
		/// </summary>
		public bool CatchErrors = true;

		/// <summary>
		/// Flags for what types to send to Console.Writeline, using provided template
		/// </summary>
		public OutputType ConsoleTypes = OutputType.None;

		/// <summary>
		/// Flags for what types to send to Debug.Writeline, using provided template
		/// </summary>
		public OutputType DebugTypes = OutputType.All;

		/// <summary>
		/// Flags for what types to send to Debug.Writeline, using provided template
		/// </summary>
		public OutputType StreamTypes = OutputType.None;

		/// <summary>
		/// Flags for what types to send to EventLog - Not Tested
		/// </summary>
		//public OutputType EventLogTypes = OutputType.None;

		public long MaxStreamLength = 0;

		//private System.Diagnostics.EventLog eventLog = null;

		public string OutputFilename {
			get { return (OutputStream is FileStream) ? ((FileStream)OutputStream).Name : "NULL"; }
		}
		#endregion

		#region Constructors
		public OutputLog() { 
			OnFlush += FlushOutput; 
		}

		public OutputLog(string moduleName) { 
			ModuleName = moduleName; 
			OnFlush += FlushOutput; 
		}
		#endregion

		#region Main Output Method
		
		private readonly object tLock = new object();

		List<DateTime> lastTimes = new List<DateTime>();

		public void SendOutput(OutputEntry entry) {
			lock (tLock) {
				if (!lastTimes.Contains(entry.Timestamp)) {
					lastTimes.Clear();
				} else {
					while (lastTimes.Contains(entry.Timestamp)) entry.Timestamp += TimeSpan.FromMilliseconds(1);
				}
				lastTimes.Add(entry.Timestamp);
			}
			try {
				//Guards
				if ((EnabledTypes & entry.OutputType) == 0) return;

				//Templates
				string text = null;

				//Streams
				if ((StreamTypes & entry.OutputType) != 0 && OutputStream != null) {
					try {
						if (text == null) text = entry + "\r\n";
						byte[] bytes = Encoding.UTF8.GetBytes(text);
						lock (OutputStream) {
							OutputStream.Write(bytes, 0, text.Length);

							if (lastFlush + FlushStreamPeriod < entry.Timestamp) {
								lastFlush = entry.Timestamp;
								OutputStream.Flush();
							} else {
								if (timer == null) timer = new System.Threading.Timer(new System.Threading.TimerCallback(Timer_Tick));
								if (timer != null) timer.Change(FlushStreamPeriod, TimeSpan.FromMilliseconds(-1));
							}

							if (MaxStreamLength > 0 && OutputStream.Length > MaxStreamLength) {
								OutputStream.Position = 0;
							}
						}
					} catch (Exception ex) {
						if (!CatchErrors) throw;
						Console.WriteLine("LOG Stream ERROR!! " + ex);
					}
				}

				//Console / Stdouts
				if ((ConsoleTypes & entry.OutputType) != 0) {
					if (text == null) text = entry + "\r\n";
					Console.Write(text);
				}

				if ((DebugTypes & entry.OutputType) != 0) {
					if (text == null) text = entry + "\r\n";
					Debug.WriteLine(text.Substring(0,text.Length-2));
				}

				//Events
				if (OutputEvent != null) {
					OutputEvent(entry);
				}

				if (EnableStaticLogging && StaticOutputEvent != null) {
					StaticOutputEvent(entry);
				}

				//final output...
				//if (MainOutputPlugin != null) {
				//    try {
				//        MainOutputPlugin.AddEntry(entry);
				//    } catch (Exception ex) {
				//        //Log.Output(OutputType.Error, "Error adding to log", ex);
				//        if (!CatchErrors) throw;
				//        Console.WriteLine("LOG Plugin ERROR!! " + ex);
				//    }
				//}
			} catch (Exception ex) {
				if (!CatchErrors) throw;
				Console.WriteLine("LOG ERROR!! " + ex);
			}
		}
		#endregion

		#region Flushing
		private static event EmptyHandler OnFlush;
		public static void FlushAllOutput() {
			if (OnFlush != null) OnFlush();
		}
		public void FlushOutput() {
			try {
				if (OutputStream != null) {
					OutputStream.Flush();
				}
			} catch(Exception ex) {
				Console.WriteLine("Error flushing" + ex);
			}
			//try{
			//    if (MainOutputPlugin != null) MainOutputPlugin.FlushOutput();
			//} catch (Exception ex) {
			//    Console.WriteLine("Error flushing" + ex);
			//}
		}
		private DateTime lastFlush = DateTime.MinValue;
		
		private System.Threading.Timer timer;
		private void Timer_Tick(object state) {
			FlushOutput();
		}
		#endregion

		#region Overloaded Incoming Output Methods
		/// <summary>
		/// Sends output to the log
		/// </summary>
		/// <param name="outputType">The category of output</param>
		/// <param name="msg">The main message in the log</param>
		public void Output(OutputType outputType, string msg) {
			Output(outputType, msg, "");
		}

		/// <summary>
		/// Sends output to the log
		/// </summary>
		/// <param name="outputType">The category of output</param>
		/// <param name="message"></param>
		/// <param name="ex">An exception, which will be automticlly formatted to include message, stacktrace and inner exception in the log</param>
		public void Output(OutputType outputType, string message, Exception ex) {
			Exception e = ex;
			string exlog = "";
			while (e.InnerException != null) {
				exlog += ", Inner: " + e.InnerException;
				e = e.InnerException;
			}
			Output(outputType, string.Format("{0} {1}, {2}", message, ex.GetType(), ex.Message), ex.StackTrace + exlog);
		}

		/// <summary>
		/// Sends output to the log
		/// </summary>
		/// <param name="outputType">The category of output</param>
		/// <param name="msg">The main message in the log</param>
		/// <param name="detailMsg">A detailed message</param>
		public void Output(OutputType outputType, string msg, string detailMsg) {
			SendOutput(new OutputEntry(DateTime.Now, ModuleName, outputType, msg, detailMsg));
		}

		#endregion

		#region Static Output Methods
		public static void StaticOutput(string moduleName, OutputType outputType, string msg) {
			Log.SendOutput(new OutputEntry(DateTime.Now, moduleName, outputType, msg, ""));
		}
		public static void StaticOutput(string moduleName, OutputType outputType, string msg, string dmsg) {
			Log.SendOutput(new OutputEntry(DateTime.Now, moduleName, outputType, msg, dmsg));
		}

		public static void StaticOutput( string moduleName, OutputType outputType, string msg, Exception ex) {
			Exception e = ex;
			string exlog = "";
			while (e.InnerException != null) {
				exlog += ", Inner: " + e.InnerException;
				e = e.InnerException;
			}

			Log.SendOutput(new OutputEntry(DateTime.Now, moduleName, outputType, string.Format("{0} {1}, {2}", msg, ex.GetType(), ex.Message),  ex.StackTrace + exlog));
		}

		public static void StaticOutput(OutputEntry oe) {
			Log.SendOutput(oe);
		}
		#endregion

	}

}
