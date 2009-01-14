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
using org.OpenVideoPlayer.Media;

namespace org.OpenVideoPlayer {
	public interface IAlternateMediaSource : IPlugin {
		IAdaptiveSegment[] AvailableSegments(MediaStreamType streamType);

		IBufferInfo BufferInfo(MediaStreamType streamType);

		void SetBitrateRange(MediaStreamType streamType, long minBitrate, long maxBitrate);

		void SetInitialBitrate(MediaStreamType streamType, long startBitrate);

		/// <summary>
		/// The highest adaptive segement currently allowed, because of physical size caps
		/// </summary>
		int SegmentCapIndex { get; }

		ulong CurrentBitrate { get; }

		ulong PeakBitrate { get; }

		ulong CurrentBandwidth { get; }

		/// <summary>
		/// Returns true if the url was properly handled, and appropriate for this plugin
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		bool SetSource(Uri url);

		MediaStreamSource MediaSource { get; }
		/// <summary>
		/// This event is fired whenever the bitrate of the playing video is changed.
		/// </summary>
		event EventHandler<BitrateChangedEventArgs> PlayBitrateChange;
		//event EventHandler AdaptiveBitrateChanged;

		/*
		/// <summary>
		/// This event is fired at the moment when the media source gets out of buffering 
		/// state and starts serving samples
		/// </summary>
		event EventHandler<EventArgs> BufferingDone;

		/// <summary>
		/// This event is fired at the moment when media source finds itself out of 
		/// media to serve with outstanding requests
		/// </summary>
		event EventHandler<EventArgs> BufferingStarted;

		/// <summary>
		/// This event is fired whenever we start downloading a new bitrate
		/// </summary>
		event EventHandler<BitrateChangedEventArgs> DownloadBitrateChange;

		/// <summary>
		/// Called after AdPauseExpectedAt() when application has full bandwidth to start downloading ads content
		/// </summary>
		event EventHandler<DownloadPausedEventArgs> DownloadsPaused;

		/// <summary>
		/// This is a public event which is fired whenever a media chunk has finished
		/// downloading. It is exposed so that external callers can keep track of what's
		/// going on, and is useful for tracking state and regression testing. This event
		/// does not need to be handled and can safely be ignored.
		/// </summary>
		event EventHandler<MediaChunkDownloadedEventArgs> MediaChunkDownloaded;

		/// <summary>
		/// The GetSampleCompleted event, which is fired every time we get a sample
		/// </summary>
		event EventHandler<GetSampleCompletedEventArgs> GetSampleCompleted;//*/
	}
}
