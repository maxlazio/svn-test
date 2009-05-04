using System;
using org.OpenVideoPlayer.Media;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
namespace org.OpenVideoPlayer {
	/// <summary>
	/// Defines the methods that an IMediaControl object must export.  This
	/// is one of the main interfaces implemented by the main player control
	/// some or all of these may be implemented as Scriptable Methods for
	/// easy access via JavaScript.
	/// </summary>
	public interface IMediaControl {
		/// <summary>
		/// Play current item
		/// </summary>
		void Play();

		/// <summary>
		/// Pause current item
		/// </summary>
		void Pause();

		/// <summary>
		/// Stop current play
		/// </summary>
		void Stop();

		/// <summary>
		/// Seeks to the next Chapter
		/// </summary>
		void SeekToNextChapter();

		/// <summary>
		/// Seek to the previous chapter
		/// </summary>
		void SeekToPreviousChapter();

		/// <summary>
		/// Seek to the next item in the playlist
		/// </summary>
		void SeekToNextItem();

		/// <summary>
		/// Seek to the previous item in the playlist
		/// </summary>
		void SeekToPreviousItem();

		double Volume { get; set; }

		TimeSpan Position { get; set; }

		TimeSpan Duration { get; }

		PlaylistCollection Playlist { get; set; }
		int CurrentIndex { get; set; }

		IMediaItem CurrentItem{get;}

		StartupEventArgs StartupArgs { get; }

		Panel LayoutRoot { get; }
		Panel VideoArea { get; }

		bool AdMode { get; set; }

		Size VideoResolution { get; }
		Size MediaElementSize { get; }

		//event RoutedEventHandler ItemChanged;
		event PlaylistIndexChangingEventHandler PlaylistIndexChanging;
		event EventHandler BrowserSizeChanged;
		event SizeChangedEventHandler SizeChanged;
		event EventHandler FullScreenChanged;
		event EventHandler MediaOpened;
		event EventHandler VolumeChanged;
		event EventHandler MediaCommand;
		event EventHandler MediaEnded;

		MediaElement MediaElement{get;}

		bool ControlsEnabled { get; set; }

		IDictionary<string, FrameworkElement> Containers { get; set; }

		IPlugin[] Plugins { get; }
	}

	public enum MediaCommandType {
		Play,
		Pause,
		Stop,
		Seek,
		//Next,
		//Previous,
	}

	public class MediaCommandEventArgs : EventArgs {
		public MediaCommandType Command { get; set; }
	}
}
