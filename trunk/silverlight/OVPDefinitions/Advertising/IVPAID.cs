using System;

namespace org.OpenVideoPlayer.Advertising.VPAID {
	/// <summary>
	/// NOTE: - This is based on pre-release information from the IAB, and is subject to change!
	/// 
	/// Interface for VPAID which enables Rich interactive ads 
	/// to communicate with their container.
	/// This interface can be used to communicate with overlay ads as
	/// well as companion rich media ad.
	/// Ad will implement this interface to be VPAID complaint ad.
	/// Player will check the ad to see if ad implements this interface 
	/// and will determine if the ad is VPAID complaint.
	/// </summary>
	public interface IVPAID {

		#region VPAID Methods

		/// <summary>
		/// The player calls handshakeVersion immediately after loading the ad to indicate to the ad that VPAID will be used. The player passes in its latest VPAID version string. The ad returns a version string minimally set to “1.0”, and of the form “major.minor.patch”. The player must verify that it supports the particular version of VPAID or cancel the ad. All VPAID versions are backwards compatible within the same major version number (but not forward compatible). So if the player supports “2.1.05” and the ad indicates “2.0.23”, the player can run the ad, but not in the reverse situation. Static interface definition implementations may require an external agreement for version matching. Dynamic implementations may use the handshakeVersion method call to determine if an ad supports VPAID
		/// </summary>
		string handshakeVersion(string version);

		/// <summary>
		/// After the ad is loaded and the player calls handshakeVersion, the player calls initAd to initialize the ad experience. The player may pre-load the ad and delay calling initAd until nearing the ad playback time, however, the ad does not load its assets until initAd is called. The ad sends the AdLoaded event to notify the player that its assets are loaded and it is ready for display. 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="viewMode">normal, fullscreen, or thumbnail</param>
		/// <param name="desiredBitrate"></param>
		/// <param name="creativeData">optional parameter that can be used for passing in additional ad initialization data.</param>
		/// /// <param name="otherParameters"></param>
		void initAd(uint width, uint height, string viewMode, int desiredBitrate, string creativeData, string otherParameters);

		/// <summary>
		/// startAd is called by the player is called when the player wants the ad to start displaying. The ad responds by sending an AdStarted event notifying the player the ad is now playing. 
		/// </summary>
		void startAd();
		
		/// <summary>
		/// stopAd is called by the player when it will no longer display the ad. stopAd is also called if the player needs to cancel an ad. However, the ad may take some time to close the ad and clean up resources before sending an AdStopped event to the player. 
		/// </summary>
		void stopAd();
		
		/// <summary>
		/// Following a resize of the ad UI container, the player calls resizeAd to allow the ad to scale or reposition itself within its display area. The width and height always matches the maximum display area allotted for the ad, and resizeAd only called when the player changes its video content container sizing. For ads that expand or go into linear mode, the entire video content display area is given in the width height as these ads may take up that entire area when in linear or expanded modes. Also, the player should avoid using the built-in scaling and sizing properties or methods for the particular implementation technology
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="viewMode">normal, fullscreen, or thumbnail</param>
		void resizeAd(uint width, uint height, string viewMode);
		
		/// <summary>
		/// pauseAd is called to pause ad playback. The ad sends an AdPaused event when the ad has been paused. The ad must turn off all audio and suspend any animation or video
		/// </summary>
		void pauseAd();

		/// <summary>
		/// resumeAd is called to continue ad playback following a call to pauseAd. The ad sends an AdPlaying event when the ad has resumed playing. 
		/// </summary>
		void resumeAd();

		/// <summary>
		/// expandAd is called by the player to request that the ad switch to its larger UI size. For example, the player may implement an open button that calls expandAd when clicked. 
		/// </summary>
		void expandAd();

		/// <summary>
		/// collapseAd is called by the player to request that the ad return to its smallest UI size. For example, the player may implement a close button that calls collapseAd when clicked and is displayed only when the ad is in expanded state 
		/// </summary>
		void collapseAd();


		#endregion

		#region VPAID Properties

		/// <summary>
		/// The linearAd Boolean indicates the ad’s current linear vs. non-linear mode of operation. linearAd when true indicates the ad is in a linear playback mode, false nonlinear
		/// </summary>
		bool linearAd { get; }

		/// <summary>
		/// The expandedAd Boolean value indicates whether the ad is in a state where it occupies more UI area than its smallest area. If the ad has multiple expanded states, all expanded states show expandedAd being true. 
		/// </summary>
		bool expandedAd { get; }

		/// <summary>
		/// The player may use the remainingTimeAd property to update player UI during ad playback. The remainingTimeAd property is in seconds and is relative to the time the property is accessed. 
		/// </summary>
		TimeSpan remainingTimeAd { get; }

		/// <summary>
		/// The player uses the volumeAd property to attempt to set or get the ad volume. The volumeAd value is between 0 and 1 and is linear. The player is responsible for maintaining mute state and setting the ad volume accordingly. If not implemented the get always returns -1. If set is not implemented, does nothing.
		/// </summary>
		float volumeAd { get; set; }

		#endregion

		#region VPAID Events
		/// <summary>
		/// The AdLoaded event is sent by the ad to notify the player that the ad has finished any loading of assets and is ready for display. The ad does not attempt to load assets until the player calls the init method. 
		/// </summary>
		event EventHandler AdLoaded;

		/// <summary>
		/// The AdStarted event is sent by the ad to notify the player that the ad is displaying. 
		/// </summary>
		event EventHandler AdStarted;

		/// <summary>
		/// The AdStopped event is sent by the ad to notify the player that the ad has stopped displaying, and all ad resources have been cleaned up. 
		/// </summary>
		event EventHandler AdStopped;

		/// <summary>
		/// The ad has been paused, in response to pauseAd
		/// </summary>
		event EventHandler AdPaused;

		/// <summary>
		/// The ad has been resumed
		/// </summary>
		event EventHandler AdResumed;

		/// <summary>
		/// The ad has completed
		/// </summary>
		event EventHandler AdComplete;

		/// <summary>
		/// The expand mode of the ad has changed
		/// </summary>
		event EventHandler AdExpandModeChanged;

		/// <summary>
		/// The ad's nature, linear vs. non-linear, has changed
		/// </summary>
		event EventHandler AdLinearModeChanged;

		/// <summary>
		/// The ad is requesting a resize.. note - not sure when this is needed -NB
		/// </summary>
		event EventHandler AdResizeRequest;

		/// <summary>
		/// The ad content has changed size
		/// </summary>
		event EventHandler AdResized;

		/// <summary>
		/// The AdVolumeChange event is sent by the ad to notify the player that the ad has changed its volume, if the ad supports volume. The player may get the volumeAd property and update its UI accordingly.
		/// </summary>
		event EventHandler AdVolumeChanged;

		/// <summary>
		/// An ad video has started rendering
		/// </summary>
		event EventHandler AdVideoStart;

		/// <summary>
		/// A video ad has reached 25%
		/// </summary>
		event EventHandler AdVideoFirstQuartile;

		/// <summary>
		/// A video ad has reached 50%
		/// </summary>
		event EventHandler AdVideoMidPoint;

		/// <summary>
		/// A video ad has reached 75%
		/// </summary>
		event EventHandler AdVideoThirdQuartile;

		/// <summary>
		/// A video ad has rendered fully to 100%
		/// </summary>
		event EventHandler AdVideoComplete;

		/// <summary>
		/// The AdUserAcceptInvitation, AdUserMinimize and AdUserClose events are sent by the ad when it meets the requirement of the same names as set in Digital Video In-Stream Ad Metrics Definitions
		/// </summary>
		event EventHandler AdAcceptInvitation;

		/// <summary>
		/// The user has closed the ad
		/// </summary>
		event EventHandler AdClose;

		/// <summary>
		/// The user has minimized the ad
		/// </summary>
		event EventHandler AdMinimize;

		/// <summary>
		/// The AdClickThru event is sent by the ad when a click thru occurs. Parameters can be included to give the player the option for handling the event. Three parameters are included with the event, String url, String id and Boolean playerHandles.
		/// </summary>
		event EventHandler<ClickThroughArgs> AdClickThru;

		/// <summary>
		/// The AdError event is sent when the ad has experienced a fatal error. Before the ad sends AdError it must clean up all resources and cancel any pending ad playback. The player must remove any ad UI, and recover to its regular content playback state. The parameter String message is included for more specific information to be passed to the player. 
		/// </summary>
		event EventHandler<StringEventArgs> AdError;

		/// <summary>
		/// The AdLog event is optionally sent by the ad to the player to relay debugging information, in a parameter String message. It is not required that the ad provide any AdLog events, but may be convenient for player engineers to help debug particular ads. 
		/// </summary>
		event EventHandler<StringEventArgs> AdLog;

		/// <summary>
		/// The AdRemainingTimeChange event is sent by the ad to notify the player that the ad’s remaining playback time has changed. The player may get the remainingTimeAd property and update its UI accordingly.
		/// </summary>
		event EventHandler AdRemainingTimeChange;

		/// <summary>
		/// The AdImpression event is used to notify the player that the user-visible phase of the ad has begun. The AdImpression event may be sent using different criteria depending on the type of ad format the ad is implementing. For a linear mid-roll the impression should coincide with the AdStart event. However, for a non-linear overlay ad, the impression will occur when the invitation banner is displayed, which is normally before the ad video is shown
		/// </summary>
		event EventHandler AdImpression;

		#endregion

	}

	#region Event Argument classes

	public class StringEventArgs : EventArgs {
		string Message { get; set; }
	}

	public class ClickThroughArgs : EventArgs {
		public string Url { get; set; }
		public string Id { get; set; }
		public bool PlayerHandles { get; set; }
	}
	#endregion
}