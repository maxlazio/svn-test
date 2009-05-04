using System;
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Browser;
using org.OpenVideoPlayer;

namespace org.OpenVideoPlayer {
	public partial class App : Application {

		public App() {
			Startup += Application_Startup;
			UnhandledException += Application_UnhandledException;

			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e) {

			if (e.InitParams.ContainsKey("type")) {
				try {
					string t = e.InitParams["type"];
					if (!t.Contains(".")) t = "OVPImplementation." + t;
					Type type = this.GetType().Assembly.GetType(t);
					ConstructorInfo c = type.GetConstructor(new Type[] { typeof(object), typeof(StartupEventArgs) });
					if (c != null) {
						RootVisual = c.Invoke(new object[] { sender, e }) as FrameworkElement;
					} else {
						RootVisual = Activator.CreateInstance(type) as FrameworkElement;
					}
				} catch (Exception ex) {
					Debug.WriteLine("Couldn't load custom page type: " + e.InitParams["type"]);
				}
			}

			if (RootVisual == null) {
				RootVisual = new Page(sender, e);
			}
			HtmlPage.RegisterScriptableObject("page", RootVisual);
		}

		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			// If the app is running outside of the debugger then report the exception using
			// the browser's exception mechanism. On IE this will display it a yellow alert 
			// icon in the status bar and Firefox will display a script error.
			if (!System.Diagnostics.Debugger.IsAttached) {

				// NOTE: This will allow the application to continue running after an exception has been thrown
				// but not handled. 
				// For production applications this error handling should be replaced with something that will 
				// report the error to the website and stop the application.
				e.Handled = true;
				Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
			}
			e.Handled = true;
		}
		private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e) {
			try {
				string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

				System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight 2 Application " + errorMsg + "\");");
			} catch (Exception) {
			}
		}

	}
}
