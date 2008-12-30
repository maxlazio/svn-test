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
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer {
	/// <summary>
	/// Defines the minimum that a plug-in must implement to be loaded by OVP
	/// </summary>
	public interface IPlugin {
		/// <summary>
		/// The official name of this PlugIn
		/// </summary>
		String PlugInName { get; }

		/// <summary>
		/// A description of what it does, etc..
		/// </summary>
		String PlugInDescription { get; }

		/// <summary>
		/// The version of this plugin, for tracking purposes
		/// </summary>
		Version PlugInVersion { get; }

		//TODO - Make this an interface also
		/// <summary>
		/// An output log, used internally for getting log messages and events to the server/database
		/// </summary>
		OutputLog Log { get; }

		IMediaControl Player {get; set;}
		//TODO - config class?
		//Initializw(IMediaControl player);
	}
}
