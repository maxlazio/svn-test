using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Browser;
using org.OpenVideoPlayer.Util;
using OVPUtility;
using System.Threading;
using System.Windows.Threading;

namespace org.OpenVideoPlayer {
	public partial class OVPUtility : Application {

		//string target;
		IDictionary<string, string> InitParams = null;
		DispatcherTimer timer = null;

		public OVPUtility() {
			Debug.WriteLine("TEST");
			this.Startup += this.Application_Startup;
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e) {
			this.RootVisual = new Page();
			InitParams = e.InitParams;
			string property = null;
			
			ProcessLoop();

			TimeSpan looptime = TimeSpan.FromMilliseconds(InitParams.ContainsKey("looptime") ? Convert.ToInt32(InitParams["looptime"]) : 0);
			if (looptime > TimeSpan.Zero) {
				if (timer == null) {
					timer = new DispatcherTimer();
					timer.Interval = looptime;
					timer.Tick+=new EventHandler(timer_Tick);
					timer.Start();
				}
			}
		}

		void  timer_Tick(object sender, EventArgs e){
 			ProcessLoop();
		}

		private void ProcessLoop(){
			foreach (string key in InitParams.Keys) {
				if (key.Contains("_")) continue;
				Process(key);
			}
		}

		void Process(string key){
			try {
				string property = InitParams.ContainsKey(key + "_target_property") ? InitParams[key + "_target_property"] : "value";

				if (key.StartsWith("url")) {
					WebClient wc = new WebClient();
					wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
					StringParamDlg d = new StringParamDlg(delegate(string val) { HtmlPage.Document.GetElementById(InitParams[key + "_target"]).SetProperty(property, val); });
					wc.DownloadStringAsync(new Uri(InitParams[key]), d);
				}

				if (key.StartsWith("property")) {
					string type = InitParams[key].Substring(0, InitParams[key].LastIndexOf('.'));
					string prop = InitParams[key].Substring(InitParams[key].LastIndexOf('.') + 1);
					object o = ReflectionHelper.GetValue(Type.GetType(type), prop);
					if (o != null) {
						HtmlPage.Document.GetElementById(InitParams[key + "_target"]).SetProperty(property, o);
					}
				}
			} catch (Exception ex) {
				Debug.WriteLine("Error: " + ex);
			}
		}

		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			try {
				if (e.UserState is StringParamDlg) {
					((StringParamDlg)e.UserState).DynamicInvoke(e.Result);
				}
			} catch (Exception ex) {
				Debug.WriteLine("Error: " + ex);
			}
		}


		public delegate void StringParamDlg(String val);
	}
}
