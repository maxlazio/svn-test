using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using org.OpenVideoPlayer.Util;
using System.Windows.Browser;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Windows.Resources;
using System.Security.Cryptography;

namespace org.OpenVideoPlayer {
	public static class PluginManager {

		private static OutputLog log = new OutputLog("PluginManager");

		public static event PluginEventHandler PluginLoaded;
		public delegate void PluginEventHandler(object sender, PluginEventArgs args);

		public static Dictionary<string, Type> PluginTypes = new Dictionary<string, Type>();

		public static List<string> RequiredPlugins = new List<string>();

		public static void LoadPlugin(Uri uri, string type) {
			WebClient wc = new WebClient();
			wc.OpenReadCompleted += OnAssemblyDownloaded;//"AdaptiveStreaming.dll"), "Microsoft.Expression.Encoder.AdaptiveStreaming.AdaptiveStreamingSource");
			wc.OpenReadAsync(uri, new DownloadInfo(){ Type=type, Uri=uri, WebClient=wc});
		}

		public static void LoadPlugins(string initValue) {
			if (!string.IsNullOrEmpty(initValue)) {
				foreach (string s in initValue.Split(' ', ';')) {
					bool required = required = s.EndsWith("!");
					Uri u = (Uri.IsWellFormedUriString(s.Substring(0, s.Length - (required?1:0)), UriKind.Absolute) ? new Uri(s) : new Uri(HtmlPage.Document.DocumentUri, s));
					if(required) {
						RequiredPlugins.Combine(u.ToString());
					}
					PluginManager.LoadPlugin(u, null);
				}
			}
		}

		public static bool RequiredPluginsLoaded {
			get { 
				foreach(string s in RequiredPlugins) {
					if(!PluginFiles.ContainsKey(s)) return false;
				}
				return true;
			}
		}

		public static IPlugin CreatePlugin(string name){
			return ((PluginTypes.ContainsKey(name)) ? CreatePlugin(PluginTypes[name]) : null) as IPlugin;
		}

		public static IPlugin CreatePlugin(Type type){
			return Activator.CreateInstance(type) as IPlugin;
		}

		public static Dictionary<string, byte[]> PluginFiles = new Dictionary<string, byte[]>();

		public static HashAlgorithm Hash = new SHA1Managed();

		static void OnAssemblyDownloaded(object sender, OpenReadCompletedEventArgs e) {
			DownloadInfo dl = e.UserState as DownloadInfo;
			try {
				//TODO - do we crash, or just skip this content?  How do we inform user?
				if (e.Cancelled) {
					throw new Exception("Assembly load cancelled");
				}
				if (e.Error != null) {
					throw e.Error;
				}
				if (e.Result == null) {
					throw new Exception("Invalid result from Assembly request");
				}
				lock (Hash) {
					PluginFiles.Add(dl.Uri.ToString(), Hash.ComputeHash(e.Result));
				}

				if(dl!=null && dl.Uri.ToString().ToLower().Contains(".dll")){
					LoadDLL(e.Result, dl);
				}else{
					LoadXAP(e.Result, dl);
				}

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error loading plugin: " + ((dl!=null)?dl.Uri.ToString():"NULL"), ex);
			}
		}

		public static void LoadXAP(Stream xap, DownloadInfo info) {
			string appManifest = new StreamReader(Application.GetResourceStream(new StreamResourceInfo(xap, null), new Uri("AppManifest.xaml", UriKind.Relative)).Stream).ReadToEnd();

			XElement deploymentRoot = XDocument.Parse(appManifest).Root;
			// Here is the Linq to parse the appmanifest xaml into a generic list
			List<XElement> deploymentParts = (from assemblyParts in deploymentRoot.Elements().Elements() select assemblyParts).ToList();

			List<Assembly> asms = new List<Assembly>();
			foreach (XElement xElement in deploymentParts) {
				string source = "";
				try {
					source = xElement.Attribute("Source").Value;
					AssemblyPart asmPart = new AssemblyPart();
					info.ElementUri = new Uri(source, UriKind.Relative);
					StreamResourceInfo streamInfo = Application.GetResourceStream(new StreamResourceInfo(xap, "application/binary"), info.ElementUri);
					Assembly asm = PreLoadDLL(streamInfo.Stream, info);
					if (asm != null) asms.Add(asm);
				} catch (Exception ex) {
					log.Output(OutputType.Debug, "Can't load " + source, ex);
				}
			}

			//Cycle through all the assembly parts and load into memory
			foreach (Assembly asm in asms) {
				try {
					if (asm == null) continue;
					LoadAssembly(asm, info);
				} catch (Exception ex) {
					log.Output(OutputType.Debug, "Can't load " + asm.FullName, ex);
				}
			}
		}

		public static Assembly PreLoadDLL(Stream dll, DownloadInfo info) {
			AssemblyPart assemblyPart = new AssemblyPart();
			return assemblyPart.Load(dll);
		}

		public static void LoadDLL(Stream dll, DownloadInfo info) {
			LoadAssembly(PreLoadDLL(dll, info), info);
		}

		public static void LoadAssembly(Assembly asm, DownloadInfo info) {
			if (asm == null) {
				throw new Exception("Invalid plugin dll");
			}

			string typeName = (info != null) ? info.Type : "";

			Type[] pTypes = (string.IsNullOrEmpty(typeName)) ? asm.GetTypes() : new Type[] { asm.GetType(typeName) };
			foreach (Type pType in pTypes) {
				if (pType == null || !ReflectionHelper.TypeHasInterface(pType, typeof(IPlugin)) || pType.IsInterface) { //todo - allow specifying a derived interface,class
					//log.Output(OutputType.Debug, "Invalid plugin type: " + pType);
					continue;
				}

				//create one to make sure it works, get name, etc.  Maybe skip this eventually?
				IPlugin plugin = Activator.CreateInstance(pType) as IPlugin;

				if (plugin != null) {
					PluginTypes.Add(plugin.PlugInName, pType);

					log.Output(OutputType.Info, string.Format("Loaded Plugin: {0}, {1} ({2} v{3})", info.Uri, plugin.PlugInName, pType, plugin.PlugInVersion));
					if (PluginLoaded != null) {
						PluginLoaded(null, new PluginEventArgs() { Assembly = asm, Plugin = plugin, PluginType = pType });
					}
				}
			}
		}

		public static void DoPluginLoaded(Assembly asm, IPlugin plugin, Type pType) {
			if (PluginLoaded != null) {
				PluginEventArgs args = (asm!=null && plugin!=null && pType !=null) ? new PluginEventArgs() { Assembly = asm, Plugin = plugin, PluginType = pType } : null;
				PluginLoaded(null, args);
			}
		}
	}

	public class DownloadInfo {
		public string Type;
		public Uri Uri;
		public Uri ElementUri;
		public WebClient WebClient;
	}
}
