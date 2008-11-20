using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using org.OpenVideoPlayer.Player;

namespace OVPImplementation {
	public partial class Page : UserControl {

		//TODO - working on a dynamic load system, so manual changes aren't needed to enable Adaptive.  -NB
		/// <summary>
		/// Provide a place for the player to go to generate a heuristics stream-source class
		/// </summary>
		//private class AdaptiveStreamingSourceFactory : IAdaptiveStreamSourceFactory {
		//    public virtual MediaStreamSource GetMediaStreamSource(MediaElement mediaElement, Uri uri) {
		//        //UNComment this line, and add a reference to AdaptiveStreaming.dll in order to enable AdaptiveEdge streaming.  -NB
		//        //return new Microsoft.Expression.Encoder.AdaptiveStreaming.AdaptiveStreamingSource(mediaElement, uri);
		//        return null;
		//    }
		//}

		public Page(object sender, StartupEventArgs e) {
			InitializeComponent();
			//Player.AdaptiveStreamSourceFactory = new AdaptiveStreamingSourceFactory();
			Player.OnStartup(sender, e);
		}
	}
}
