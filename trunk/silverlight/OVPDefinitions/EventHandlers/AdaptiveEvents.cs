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
using System.Reflection;

namespace org.OpenVideoPlayer {
	public class BitrateChangedEventArgs : EventArgs {
		/// <summary>
		/// Initializes a new instance of the BitrateChangedEventArgs class
		/// </summary>
		/// <param name="streamType">the type that is changing</param>
		/// <param name="bitrate">the bitrate we changed to, in kbps</param>
		/// <param name="timestamp">the timestamp of the change</param>
		public BitrateChangedEventArgs(MediaStreamType streamType, ulong bitrate, DateTime timestamp) {
			StreamType = streamType;
			Bitrate = bitrate;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets or sets the stream type that changed
		/// </summary>
		public MediaStreamType StreamType { get; set; }

		/// <summary>
		/// Gets or sets the new bitrate, in kbps
		/// </summary>
		public ulong Bitrate { get; set; }

		/// <summary>
		/// Gets or sets the timestamp of the bitrate change
		/// </summary>
		public DateTime Timestamp { get; set; }
	}

	public class PluginEventArgs : EventArgs {
		public IPlugin Plugin {get; set;}
		public Assembly Assembly{get;set;}
		public Type PluginType { get; set; }
	}

	public delegate void ConnectionEventHandler(object sender, EventArgs e);

	public delegate void PlaylistIndexChangingEventHandler(object sender, PlaylistIndexChangingEventArgs args);

	public class PlaylistIndexChangingEventArgs : EventArgs {
		public int CurrentIndex { get; set; }
		public int NewIndex { get; set; }
		public bool Cancel { get; set; }
	}
}
