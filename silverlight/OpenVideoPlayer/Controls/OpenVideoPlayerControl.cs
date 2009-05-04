using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using org.OpenVideoPlayer.Controls.Visuals;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Util;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Reflection;
using System.Net;

namespace org.OpenVideoPlayer.Controls {
	[ScriptableType]
	public class OpenVideoPlayerControl : ControlBase, IMediaControl {

		#region Constructor
		/// <summary>
		/// Creates a new instance of the OpenVideoPlayer control
		/// </summary>
		public OpenVideoPlayerControl() {
			Application.Current.UnhandledException += Current_UnhandledException;
			log.ModuleName = "Player";
			/***
			 * Setup all our default values and initialize all internal properties.
			 */
			DefaultStyleKey = typeof(OpenVideoPlayerControl);
			SetValue(PlaylistProperty, new PlaylistCollection());

			CurrentIndex = -1;
			PlaybackDurationText = PlaybackPositionText = "00:00";
			lastUsedVolume = START_VOLUME;

			stretchMode = Stretch.None;
			AutoScaling = AutoScalingType.None;
			AutoPlay = true;

			CheckEdgeServerIP = true;
			AutoHideNextAndPrevious = true;

			isMuted = false;
			Containers = new Dictionary<string, FrameworkElement>();

			Version = ReflectionHelper.GetAssemblyVersion();
			ControlBinding = ControlBindingFlags.BindFields | ControlBindingFlags.BindEvents | ControlBindingFlags.Recursive;

			System.Reflection.Assembly ass = Assembly.GetExecutingAssembly();

			PlaybackPositionText = PlaybackDurationText = "00:00";
			
			//WebClient wc = new WebClient();
			//wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
			//Uri u = new Uri(HtmlPage.Document.DocumentUri, HtmlPage.Plugin.GetProperty("Source").ToString());
			//wc.OpenReadAsync(u,u);

		}

		//void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e) {
		//    WebClient wc = (WebClient)sender;
		//    //ServerTime = DateTime.Parse(wc.Headers[HttpRequestHeader.Date]);
		//    lock (PluginManager.Hash) {
		//        PluginManager.PluginFiles.Combine(((Uri) e.UserState).ToString(), PluginManager.Hash.ComputeHash(e.Result));
		//        //log.Output(OutputType.Debug, "Hashed player from : " + ((Uri)e.UserState).ToString());
		//        PluginManager.DoPluginLoaded(null, null, GetType());
		//    }
		//}

		/// <summary>
		/// Fired when the active playlist item changes.  There are also events available on the playlist itself
		/// </summary>
		//public event RoutedEventHandler ItemChanged;

		[ScriptableMember]
		/// <summary>
		/// Occurs when the actual browser (ie/firefox,etc) window size changes
		/// </summary>
		public event EventHandler BrowserSizeChanged;

		[ScriptableMember]
		public new event SizeChangedEventHandler SizeChanged;

		[ScriptableMember]
		public event EventHandler FullScreenChanged;

		[ScriptableMember]
		public event EventHandler MediaOpened;

		[ScriptableMember]
		public event EventHandler MediaCommand;

		/// <summary>
		/// Needed internally to know why a player size is being adjusted
		/// </summary>
		enum PlayerSizeChangeType { Bitrate, Size };
		//TODO - we need to carry through many of the media events and properties
		#endregion

		#region internal UI Element fields - bound to XAML Template
		//Note - these are bound dynamically by our base control, ControlBase.  They must match the name in the xaml template

		/// <summary>
		/// Our toplevel control in the template
		/// </summary>
		protected internal Panel layoutRoot;

		protected internal Panel videoArea;
		/// <summary>
		/// The border that surrounds the player
		/// </summary>
		protected internal Border mainBorder;

		/// <summary>
		/// The border highlight that surrounds the player
		/// </summary>
		protected internal Rectangle highlightBorder;
		/// <summary>
		/// grid that seperated the player and controls
		/// </summary>
		protected internal Grid mainGrid;
		/// <summary>
		/// The media element
		/// </summary>
		protected internal MediaElement mediaElement;
		/// <summary>
		/// The slider bars for volume and position
		/// </summary>
		protected internal ScrubberBar sliderVolume, scrubberBar;
		/// <summary>
		/// A special control for the embed tag and link url
		/// </summary>
		protected internal ContentLinkEmbedBox linkEmbed;
		/// <summary>
		/// IelementLists that are used for playlist, chapters, and logs but all have similar characteristics
		/// </summary>
		protected internal IElementList playlist, chapters;
		
		protected internal LogViewer logViewer;
		/// <summary>
		/// The special qualityguage for adaptive content
		/// </summary>
		protected internal QualityGauge qualityGauge;
		/// <summary>
		/// The control box that houses all of our controls
		/// </summary>
		//protected internal Panel controlBox;
		protected internal ControlsPanel controlBox;
		/// <summary>
		/// Buttons in the control box
		/// </summary>
		protected internal Button buttonPlayPause, buttonPrevious, buttonNext, buttonStop, buttonLinkEmbed, buttonFullScreen, buttonMute, buttonMenu,
			buttonPlaylist, buttonChapters;
		/// <summary>
		/// icons for play/pause button and messagebox in center, referenced because they sometimes need to be swapped out
		/// </summary>
		protected internal FrameworkElement playToggle, pauseToggle, playSymbol;
		/// <summary>
		/// Our menu with advanced options, like captions, chapters, debug, etc..
		/// </summary>
		protected internal Menu menuOptions;
		/// <summary>
		/// the textblock that shows our statistics
		/// </summary>
		//protected internal TextBlock stats;
		/// <summary>
		/// the border around the messagebox in the center (when paused, error, etc)
		/// </summary>
		protected internal Border messageBox;
		/// <summary>
		/// The text in the center, incase there is an error or message to show
		/// </summary>
		protected internal TextBlock messageBoxText;
		/// <summary>
		/// the tooltop, we use a custom one so we can have more control over when it renders and moves with the cursor
		/// </summary>
		protected internal CustomToolTip toolTip;

		protected internal BufferIcon bufferIcon;

		#endregion

		#region private instance variables

		#region Constants
		private const Double START_VOLUME = 0.5;
		private const Double LOWER_VOLUME_THRESHOLD = 0.01;
		public const string PLAYER_CONTROL_NAME = "OpenVideoPlayerControl";
		#endregion

		/// <summary>
		/// the timer used for many updates - it runs on the dispatcher (ui) thread, so we have to be careful not to block it 
		/// </summary>
		protected DispatcherTimer mainTimer;
		/// <summary>
		/// A second timer on a seperate thread for anything we can offload that doesn't need to update the ui
		/// </summary>
		protected Timer threadTimer;

		/// <summary>
		/// The initial startup args from initparams that the app sends us
		/// </summary>
		public StartupEventArgs StartupArgs { get; protected set; }

		/// <summary>
		/// the backing store for our logs
		/// </summary>
		protected  LogCollection logList = new LogCollection();

		protected StopWatch sw = new StopWatch();

		/// <summary>
		/// Make sure we don't update the debug screen to often and hurt performance
		/// </summary>
		protected DateTime lastDebugUpdate;
		//internal/private tracking vars
		protected bool timerIsUpdating, isPlaying, /*updateBuffering, updateDownloading,*/ isMuted, waitOnClick;

		protected DateTime lastClick, lastMouseMove = DateTime.MinValue;
		protected Double lastUsedVolume, lastMediaHeight = 0;
		protected Double lastDownloadProgress, lastBufferProgress;

		protected string lastSource, lastCommand, bitrateString = "";
		protected HorizontalAlignment lastHAlighn;
		protected VerticalAlignment lastVAlighn;

		protected MediaElementState lastMediaState = MediaElementState.Closed;
		protected Stretch lastStretchMode;
		protected Thickness lastMargin, lastPadding;

		protected bool overrideStretch = false;
		protected AdaptiveStats currentStats;
		protected string lastQualityTag;
		protected string lastStats;

		private readonly object timerLock = new object();
		private readonly object uiTimerLock = new object();

		private TimeSpan lastDur;

		protected Size lastSize = new Size(0, 0);//848 + 16, 480 + 200);
		public Size AutoScalingCalculatedPlayerSize { get; protected set; }
		public Size AutoScalingMinimumSize { get; set; }// = new Size(848, 480);
		public Size AutoScalingForcedAspect { get; set; }
		public AutoScalingType AutoScaling { get; set; }

		public enum AutoScalingType {
			None,
			ScalePlayer,
			ScaleObjectTag,
			ScaleParent
		}

		/// <summary>
		/// The amount of margin that the player must be larger than the playable content size for the player to upsize itself, when in adaptive resize mode.
		/// For example, if the content is 848x480 now and the next size is 1280x720, then the next outside player size will be 1296x920 (assuming the default margin of 16x200)
		/// </summary>
		public Size AutoScalingPlayerMargin { get; set; }// = new Size(16, 200);//TODO - configgable
		/// <summary>
		/// The broswer margin for resizing, this additional area must be available beyond the anticipated size of the player, in order to resize
		/// The is to accomodate for other aspects of the page
		/// </summary>
		public Size AutoScalingBrowserMargin { get; set; }// = new Size(60, 80);



		#endregion

		#region Template Methods

		/// <summary>
		/// Overrides the controls OnApplyTemplate Method to capture and wire things up
		/// </summary>
		public override void OnApplyTemplate() {
			try {
				lastSize = new Size(Width, Height);
				lastMargin = Margin;
				lastHAlighn = HorizontalAlignment;
				lastVAlighn = VerticalAlignment;
				lastPadding = Padding;

				base.OnApplyTemplate();

				if (menuOptions != null) {
					menuOptions.ApplyTemplate();
				}
				
				ApplyConfiguration();

				HookHandlers();

			} catch (Exception ex) {
				// Debug.WriteLine("Failed to load theme : " + ", " + ex);
				log.Output(OutputType.Error, "Error in Apply Template", ex);
			} 
		}


		/// <summary>
		/// Wires up all the event handlers to the controls
		/// </summary>
		protected void HookHandlers() {
			OutputLog.StaticOutputEvent += OutputLog_StaticOutputEvent;

			//Use this instead of our timer - for better control and option of background priority
			Async.Do(AsyncLoop, null, false, ReenteranceMode.Bypass);

			if (Application.Current != null) {
				Application.Current.Host.Content.FullScreenChanged += OnFullScreenChanged;
				Application.Current.Host.Content.Resized += OnFullScreenChanged;
			}

			MouseMove += On_MouseMove;
			MouseLeftButtonDown += On_MouseLeftButtonDown;
			base.SizeChanged += new SizeChangedEventHandler(On_SizeChanged);
			PluginManager.PluginLoaded += On_PluginLoaded;
			this.BindingValidationError += On_BindingValidationError;

			Box.Closed += new RoutedEventHandler(OnBoxClosed);

			if (Playlist != null) {
				Playlist.LoadComplete += Playlist_LoadComplete;
			}
		}

		PlayerInitParameterParser playerInitParser;

		/// <summary>
		/// Applies the configuration of the properties to the template
		/// </summary>
		protected void ApplyConfiguration() {
			if (logViewer != null) {
				logViewer.Source = logList;
			}

			if (StartupArgs != null) {
				//Import our initialization values via the init parser
				playerInitParser = new PlayerInitParameterParser();
				playerInitParser.ImportInitParams(StartupArgs, this);
			}

			//TODO: apply Markers from playlist.
			//TODO: ApplyConfiguration the markers to the video item

			if (playlist != null) {
				playlist.Source = Playlist;
			}

			if (string.IsNullOrEmpty(EmbedTag) && string.IsNullOrEmpty(LinkUrl)) {
				//check embed 
				if (buttonLinkEmbed != null) buttonLinkEmbed.Visibility = Visibility.Collapsed;
			} else {
				if (linkEmbed != null) {
					linkEmbed.EmbedText.Text = EmbedTag ?? "";
					linkEmbed.LinkText.Text = LinkUrl ?? "";
				}
				if (buttonLinkEmbed != null) buttonLinkEmbed.Visibility = Visibility.Visible;
			}

			//Call the fullscreen support for if we're starting in fullscreen
			PerformResize();

			if (mediaElement != null) {
				mediaElement.AutoPlay = AutoPlay;
				mediaElement.Stretch = stretchMode;

				if (sliderVolume != null) {
					sliderVolume.Minimum = 0;
					sliderVolume.Maximum = 1;

					if (isMuted) {
						sliderVolume.Value = 0;
						mediaElement.Volume = 0;
					} else {
						sliderVolume.Value = START_VOLUME;
					}
					mediaElement.Volume = sliderVolume.Value;
				}
				StartAutoPlay();
			}

			if (menuOptions != null) {
				menuOptions.SetCheckState((object)StretchMode.ToString(), true);
				menuOptions.SetCheckState("Statistics", ShowStatistics);
				menuOptions.SetCheckState("Logs", ShowLogViewer);
			}

			if (AutoHideNextAndPrevious) {
				buttonNext.Visibility = buttonPrevious.Visibility = Visibility.Collapsed;
			}

			if (scrubberBar != null) {
				scrubberBar.Throttle = TimeSpan.FromSeconds(1);
			}
			UpdateDebugPanel();
		}

		#endregion

		#region properties

		/// <summary>
		/// the adaptive plugin currently in use, if available
		/// </summary>
		public IAlternateMediaSource AlternateMediaSource { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The number of frames currently rendered per second
		/// </summary>
		public Double FPSRendered { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The number of frames per second that had to be dropped - because of timing or cpu most likely
		/// </summary>
		public Double FPSDropped { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The total number of FPS of this content
		/// </summary>
		public Double FPSTotal { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The resolution of the currently playing content
		/// </summary>
		public Size VideoResolution { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The size of this player's media element
		/// </summary>
		public Size MediaElementSize { get; protected set; }

		[ScriptableMember]
		/// <summary>
		/// The size of the bwser window that we are hosted in
		/// </summary>
		public Size BrowserSize { get; protected set; }

		private bool adMode;

		/// <summary>
		/// Controls whether the player is in a special mode for showing Linear Ads
		/// </summary>
		[ScriptableMember]
		public bool AdMode {
			get { return adMode; }
			set {
				if (adMode != value) {
					adMode = value;
					log.Output(OutputType.Debug, "AdMode set: " + value);
					if (!value) {
						mediaElement.Play();
						SetButtonStatus();
						scrubberBar.IsEnabled = true;
						if (buttonPlaylist != null) buttonPlaylist.IsEnabled = true;
					} else {
						mediaElement.Pause();
						bufferIcon.Active = false;
						if (buttonNext != null) buttonNext.IsEnabled = false;
						if (buttonPrevious != null) buttonPrevious.IsEnabled = false;
						if (buttonPlaylist != null) buttonPlaylist.IsEnabled = false;
						scrubberBar.IsEnabled = false;

						playToggle.Visibility = Visibility.Collapsed;
						pauseToggle.Visibility = Visibility.Visible;
					}
				}
			}
		}

		[ScriptableMember]
		/// <summary>
		/// The position into the content that we are currently playing
		/// </summary>
		public TimeSpan Position { 
			get { return mediaElement.Position; } 
			set { Seek(value); } 
		}

		[ScriptableMember]
		/// <summary>
		/// The duration of our current content
		/// </summary>
		public TimeSpan Duration { get { return mediaElement.NaturalDuration.TimeSpan; } }

		[ScriptableMember]
		/// <summary>
		/// The media element that this player is built around.  Shouldn't typically be used directly
		/// </summary>
		public MediaElement MediaElement { get { return mediaElement; } }
		//public bool InAd { get; set; }


		public IDictionary<string, FrameworkElement> Containers { get; set; }

		[ScriptableMember]
		/// <summary>
		/// If true, the player will autoplay when loaded
		/// </summary>
		public bool AutoPlay { get; set; }

		[ScriptableMember]
		/// <summary>
		/// sets whether this player is muted, keeps track of last used volume
		/// TODO - do these properties need to make sure they are on the correct thread, for use from javascript?
		/// </summary>
		public bool Muted {
			get { return isMuted; }
			set {
				if (isMuted == value) return;
				isMuted = value;
				if (!isMuted) {
					mediaElement.Volume = lastUsedVolume;
					sliderVolume.Value = lastUsedVolume;
					buttonMute.Opacity = 1;
					ToolTipService.SetToolTip(buttonMute, "Mute");
				} else {
					lastUsedVolume = mediaElement.Volume;
					mediaElement.Volume = 0;
					sliderVolume.Value = 0;
					buttonMute.Opacity = .5;
					ToolTipService.SetToolTip(buttonMute, "UnMute");
				}
			}
		}

		/// <summary>
		/// The control box that houses all of our controls
		/// </summary>
		//protected internal Panel controlBox;
		public ControlsPanel ControlBox {
			get { return controlBox; }
		}

		protected Stretch stretchMode = Stretch.Uniform;
		/// <summary>
		/// The type of player scaling to use; Fit, native, stretch ,etc...
		/// </summary>
		public Stretch StretchMode {
			get { return stretchMode; }
			set {
				stretchMode = value;
				if (mediaElement != null) mediaElement.Stretch = StretchMode;
				if (menuOptions != null) menuOptions.SetCheckState((object)StretchMode.ToString(), true);
			}
		}

		private Stretch stretchModeFullScreen = Stretch.Uniform;
		public Stretch StretchModeFullScreen {
			get { return stretchModeFullScreen; }
			set { stretchModeFullScreen = value; }
		}

		/// <summary>
		/// Controls whether we show the chapters box
		/// </summary>
		[ScriptableMember]
		public bool ShowChapters {
			get { return ((FrameworkElement)chapters.Parent).Visibility == Visibility.Visible; }
			set { ((FrameworkElement)chapters.Parent).Visibility = (value && chapters.Count > 0) ? Visibility.Visible : Visibility.Collapsed; }
		}

		/// <summary>
		/// Controls whether we show the playlist
		/// </summary>
		[ScriptableMember]
		public bool ShowPlaylist {
			get { return ((FrameworkElement)playlist.Parent).Visibility == Visibility.Visible; }
			set {
				if (value != ShowPlaylist) {
					((FrameworkElement)playlist.Parent).Visibility = (value && (Playlist.Count > 1)) ? Visibility.Visible : Visibility.Collapsed;
					AdjustPlaylist();
				}
			}
		}

		/// <summary>
		/// the main border around the player
		/// </summary>
		public Border MainBorder { get { return mainBorder; } }

		/// <summary>
		/// The main panel at the root of our template
		/// </summary>
		public Panel LayoutRoot { get { return layoutRoot; } }

		public Panel VideoArea { get { return videoArea; } }

		/// <summary>
		/// The menu for adavanced options
		/// </summary>
		public Menu OptionsMenu {
			get { return menuOptions; }
			set { menuOptions = value; }
		}
	
		[ScriptableMember]
		/// <summary>
		/// The index of the currently playing item
		/// </summary>
		public int CurrentIndex { get; set; }

		[ScriptableMember]
		/// <summary>
		/// The item currently playing
		/// </summary>
		public IMediaItem CurrentItem {
			get {
				try {
					return Playlist[CurrentIndex];
				} catch {
					return null;
				}
			}
		}

		[ScriptableMember]
		/// <summary>
		/// The index of the currently set chapter
		/// </summary>
		public int CurrentChapterIndex { get; set; }

		[ScriptableMember]
		/// <summary>
		/// The item currently playing
		/// </summary>
		public IChapterItem CurrentChapter {
			get { return this.CurrentItem.Chapters[CurrentChapterIndex]; }
		}

		private Brush highlight = new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
		/// <summary>
		/// similar to the background and foreground properties, used for highlight colors in the player
		/// </summary>
		public Brush Highlight {
			get { return highlight; }
			set { highlight = value; }
		}
		public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(Brush), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Get's the object's playlist
		/// </summary>
		[System.ComponentModel.Category("Items")]
		public PlaylistCollection Playlist {
			get { return (PlaylistCollection)GetValue(PlaylistProperty); }
			set {
				SetValue(PlaylistProperty, value);
				playlist.Source = value;
			}
		}
		/// <summary>
		/// Dependency Property storage for the playlist
		/// </summary>
		public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register("Playlist", typeof(PlaylistCollection), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public IElementList Chapters {
			get { return chapters; }
			set { chapters = value; }
		}

		public String PlaybackPositionText {
			get { return (String)GetValue(PlaybackPositionTextProperty); }
			set { 
				SetValue(PlaybackPositionTextProperty, value);
				if (controlBox != null) controlBox.SetValue(ControlsPanel.PlaybackPositionTextProperty, value); 
			}
		}
		/// <summary>
		/// Dependency Property stores the current playback position text value
		/// </summary>
		public static readonly DependencyProperty PlaybackPositionTextProperty = DependencyProperty.Register("PlaybackPositionText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public String PlaybackDurationText {
			get { return (String)GetValue(PlaybackDurationTextProperty); }
			set { SetValue(PlaybackDurationTextProperty, value);
			if (controlBox != null) controlBox.SetValue(ControlsPanel.PlaybackDurationTextProperty, value); 
			}
		}
		/// <summary>
		/// Depencency Property stores the total media duration text value
		/// </summary>
		public static readonly DependencyProperty PlaybackDurationTextProperty = DependencyProperty.Register("PlaybackDurationText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public static readonly DependencyProperty CurrentItemProperty = DependencyProperty.Register("CurrentItem", typeof(IMediaItem), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));


        /// <summary>
		/// Depencency Property stores the url for the overlay image to display when buffering
		/// </summary>
		public static readonly DependencyProperty OptionsMenuProperty = DependencyProperty.Register("menuOptions", typeof(Menu), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

	
		public String CaptionText {
			get { return (String)GetValue(CaptionTextProperty); }
			set { SetValue(CaptionTextProperty, value); }
		}
		/// <summary>
		/// Depencency Property stores any caption text
		/// </summary>
		public static readonly DependencyProperty CaptionTextProperty = DependencyProperty.Register("CaptionText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public String MessageText {
			get { return (String)GetValue(MessageTextProperty); }
			set { SetValue(MessageTextProperty, value); }
		}
		/// <summary>
		/// Depencency Property stores the bitrate/diagnostics string.
		/// </summary>
		public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register("MessageText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));


		public bool RandomPlayback {
			get { return (bool)GetValue(RandomPlaybackProperty); }
			set { SetValue(RandomPlaybackProperty, value); }
		}
		/// <summary>
		/// Depencency Property stores the bitrate/diagnostics string.
		/// </summary>
		public static readonly DependencyProperty RandomPlaybackProperty = DependencyProperty.Register("RandomPlayback", typeof(bool), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public bool ShowStatistics {
			get { return logViewer.Parent.Visibility == Visibility.Visible; }
			set {
				//logViewer.ExpandLogs = false;
				logViewer.Parent.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public bool ShowLogViewer {
			get { return logViewer.Parent.Visibility == Visibility.Visible && logViewer.ExpandLogs; }
			set {
				if (value && !ShowStatistics) ShowStatistics = true;
				logViewer.ExpandLogs = value;
			}
		}

		/// <summary>
		/// Only show next and previous buttons if there are itms in the playlist to go to.
		/// </summary>
		public bool AutoHideNextAndPrevious {
			get;set;
		}

		/// <summary>
		/// Make a call on each item to get info about the server and client ips
		/// only supported by certain edge servers
		/// </summary>
		public bool CheckEdgeServerIP {
			get;set;
		}

		#endregion

		#region IMediaControl (Scriptable) Members

		/// <summary>
		/// Causes the mediaElement to begin playing
		/// </summary>
		[ScriptableMember]
		public void Play() {
			string command = "Play";
			//prevent duplicate calls
			if (command == lastCommand && mediaElement.CurrentState == MediaElementState.Playing) return;
			lastCommand = command;

			isPlaying = true;
			AutoPlay = false;

			if (mediaElement != null) {
				if (Playlist.Count > 0 && CurrentIndex == -1) {
					lastCommand = "Seek";
					SeekToPlaylistItem(0);
					return;
				}
				if (mediaElement.CurrentState == MediaElementState.Stopped) {
					mediaElement.Position = new TimeSpan(0);
				}
				
				if (!AdMode) {
					//if (mediaElement.Source == null) {
					//	mediaElement.Source = CurrentSource;
					//}
					mediaElement.Play();
				} else {
					//override these during ads
					playToggle.Visibility = Visibility.Collapsed;
					pauseToggle.Visibility = Visibility.Visible;
				}

				if (MediaCommand != null) {
					MediaCommand(this, new MediaCommandEventArgs() { Command = MediaCommandType.Play });
				}

				log.Output(OutputType.Debug, "Command: Play");
			}
			ToolTipService.SetToolTip(buttonPlayPause, "Pause");
			messageBox.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Pause's the mediaElement
		/// </summary>
		[ScriptableMember]
		public void Pause() {
			if (mediaElement == null) return;
		string command = "Pause";

			if (!AdMode) {
				if (mediaElement.CanPause) {
					mediaElement.Pause();
				}else{
					//this happens with live content.
					Stop();
					return;
				}
			} else {
				//override these during ads
				playToggle.Visibility = Visibility.Visible;
				pauseToggle.Visibility = Visibility.Collapsed;
			}

			//was causing race
			if (command == lastCommand) return;
			lastCommand = command;

			if (MediaCommand != null) {
				MediaCommand(this, new MediaCommandEventArgs(){ Command = MediaCommandType.Pause});
			}

			log.Output(OutputType.Debug, "Command: " + command);
			isPlaying = false;
			ToolTipService.SetToolTip(buttonPlayPause, "Play");
			SetPausedMessageBox();
		}

		/// <summary>
		/// Sets paused box in ccenter of video window
		/// </summary>
		private void SetPausedMessageBox() {
			messageBox.Visibility = Visibility.Visible;
			messageBox.Opacity = .5;
			MessageText = "";
			playSymbol.Visibility = Visibility.Visible;
			messageBoxText.FontSize = 18;
			messageBox.Height = messageBoxText.ActualHeight + 30;
			messageBoxText.Foreground = new SolidColorBrush(Colors.White);
		}

		/// <summary>
		/// Starts playing if there are playlist items, we haven't already started once
		/// </summary>
		public void StartAutoPlay() {
			if (!PluginManager.RequiredPluginsLoaded) {
				log.Output(OutputType.Debug, string.Format("Holding autostart until all required plugins load."));
				return;
			}
			if (Playlist.Count > 0) {
				if (AutoPlay) {
					log.Output(OutputType.Debug, "Autostart");
					SeekToPlaylistItem(0);
				} else {
					SetPausedMessageBox();
				}
			}
		}

		/// <summary>
		/// Toggles the Play or Pause state
		/// </summary>
		[ScriptableMember]
		public void TogglePlayPause() {
			if (AdMode) {
				if (playToggle.Visibility == Visibility.Collapsed) Pause(); else Play();
			} else {
				if (mediaElement != null) {
					//if (mediaElement.CurrentState == MediaElementState.Closed) {
					//	StartAutoPlay();
					//} else 
					if (mediaElement.CurrentState != MediaElementState.Playing) {
						Play();
					} else {
						Pause();
					}
				}
			}
		}

		//TODO - dep property
		[ScriptableMember]
		public bool PlayListOverlay { get; set; }

		/// <summary>
		/// Stops the mediaElement
		/// </summary>
		[ScriptableMember]
		public void Stop() {
			string command = "Stop";
			if (command == lastCommand) return;
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			if (MediaCommand != null) {
				MediaCommand(this, new MediaCommandEventArgs() { Command = MediaCommandType.Stop });
			}

			if (mediaElement != null) {
				mediaElement.Stop();
				mediaElement.AutoPlay = false;
				//mediaElement.Source = null;
			}

			isPlaying = false;
			ToolTipService.SetToolTip(buttonPlayPause, "Play");
			SetPausedMessageBox();
		}

		/// <summary>
		/// Attempts to seek to the next chapter or the first chapter of the next
		/// item in the playlist.
		/// </summary>
		[ScriptableMember]
		public void SeekToNextChapter() {
			string command = "NextChapter";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			if (ChapterChanged != null) {
				ChapterChanged(this, new EventArgs());
			}

			if (!SeekToChapterPoint(CurrentChapterIndex + 1)) {
				SeekToPlaylistItem(CurrentIndex + 1);
			}
		}

		/// <summary>
		/// Attempts to seek to the previous chapter or the last chapter of the
		/// previous playlist item.
		/// </summary>
		[ScriptableMember]
		public void SeekToPreviousChapter() {
			string command = "PrevChapter";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			if (ChapterChanged != null) {
				ChapterChanged(this, new EventArgs());
			}

			if (!SeekToChapterPoint(CurrentChapterIndex - 1)) {
				SeekToPlaylistItem(CurrentIndex - 1);
				SeekToChapterPoint(CurrentItem.Chapters.Count - 1);
			}
		}

		/// <summary>
		/// Attempts to seek to the next item in the playlist
		/// </summary>
		[ScriptableMember]
		public void SeekToNextItem() {
			string command = "Next";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			SeekToPlaylistItem(CurrentIndex + 1);
		}

		public event PlaylistIndexChangingEventHandler PlaylistIndexChanging;
		public event EventHandler ChapterChanged;
	
		/// <summary>
		/// Attempts to seek to the previous item in the playlist
		/// </summary>
		[ScriptableMember]
		public void SeekToPreviousItem() {
			string command = "Prev";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);
			
			SeekToPlaylistItem(CurrentIndex - 1);
		}

		public event EventHandler VolumeChanged;

		[ScriptableMember]
		public double Volume{
			get { return mediaElement.Volume; }
			set{
				double currentVolume = Math.Min(1.0, Math.Max(0.0, value));
			
				if(mediaElement.Volume != currentVolume) {

					mediaElement.Volume = currentVolume;
					sliderVolume.Value = currentVolume;
					lastUsedVolume = currentVolume;
					isMuted = sliderVolume.Value < LOWER_VOLUME_THRESHOLD;

					mediaElement.Volume = value;
					if (VolumeChanged != null) {
						VolumeChanged(this, new EventArgs());
					}
				}
			}
		}

		[ScriptableMember]
		public bool ShowControls {
			get { return (controlBox != null) ? controlBox.Visibility == Visibility.Visible : false; }
			set {
				if (controlBox != null) {
					controlBox.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
					if (value && Application.Current.Host.Content.IsFullScreen) {
						controlBox.SetValue(Grid.RowProperty, 0);
						controlBox.VerticalAlignment = VerticalAlignment.Bottom;
						controlBox.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
					} else {
						controlBox.SetValue(Grid.RowProperty, 2);
						controlBox.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
					}
				}
				//mainGrid.RowDefinitions[2].Height = (value) ? new GridLength(32) : new GridLength(0); 
			}
		}

		[ScriptableMember]
		public string CurrentSource {
			get {
				//string plsource = (Playlist != null && Playlist.Count > currentlyPlayingItem && Playlist[currentlyPlayingItem] != null) ? Playlist[currentlyPlayingItem].Url : null;
				string source = (mediaElement.Source == null) ? lastSource : mediaElement.Source.ToString(); //TODO - add support for non-playlisted adapative sources?
				return lastSource;
			}
		}

		/// <summary>
		/// The provided url to link to his video
		/// </summary>
		[ScriptableMember]
		public string LinkUrl { get; set; }

		/// <summary>
		/// A embed tag to show users
		/// </summary>
		[ScriptableMember]
		public string EmbedTag { get; set; }

		[ScriptableMember]
		public Version Version { get; protected set; }

		[ScriptableMember]
		public void ToggleFullscreen() {
			lock (this) {
				bool full = Application.Current.Host.Content.IsFullScreen;
				Debug.WriteLine("FS clicked is " + full);
				if (!full) {
					lastSize = new Size(Width, Height);
					lastMargin = Margin;
					lastHAlighn = HorizontalAlignment;
					lastVAlighn = VerticalAlignment;
				}
				Application.Current.Host.Content.IsFullScreen = !full;

				if (!full) {
					if (buttonFullScreen != null) ToolTipService.SetToolTip(buttonFullScreen, "Restore Screen");
					Width = Double.NaN;
					Height = Double.NaN;
					Margin = new Thickness(0.0);
					HorizontalAlignment = HorizontalAlignment.Stretch;
					VerticalAlignment = VerticalAlignment.Stretch;
				} else {
					if (buttonFullScreen != null) ToolTipService.SetToolTip(buttonFullScreen, "FullScreen");
					Width = lastSize.Width;
					Height = lastSize.Height;
					Margin = lastMargin;
				}

				CollapseMenus();
			}
			Debug.WriteLine("FS complete, is " + Application.Current.Host.Content.IsFullScreen);
		}

		#endregion

		#region Player Methods

		/// <summary>
		/// Moves to the given item in the playlist.  If the item does not exist it does nothing.
		/// </summary>
		/// <param name="newIndex">The item index to seek to</param>
		public void SeekToPlaylistItem(int newIndex) {
			//if (newIndex == CurrentIndex) return;

			if (PlaylistIndexChanging != null) {
				PlaylistIndexChangingEventArgs args = new PlaylistIndexChangingEventArgs() {NewIndex = newIndex, CurrentIndex = this.CurrentIndex};
				PlaylistIndexChanging(this, args);
				if (args.Cancel) return;
				newIndex = args.NewIndex;
			}

			if (newIndex < 0 || newIndex >= Playlist.Count) {
				return;
			}

			//we have something to play.
			CurrentChapterIndex = 0;
			CurrentIndex = newIndex;
			//if (ItemChanged != null) ItemChanged(this, new RoutedEventArgs());

			if (chapters != null) {
				chapters.Source = CurrentItem.Chapters;
			}

			// Bring adaptive heuristics in..
			// NOTE: ADAPTIVE IS DISABLED IN THE PUBLIC BUILD, THE DLL MUST BE PROVIDED BY AKAMAI/MICROSOFT
			bool found = false;
			foreach (IPlugin p in plugins) {
				IAlternateMediaSource alt = p as IAlternateMediaSource;
				if (alt == null) continue;
				if (alt.SetSource(new Uri(CurrentItem.Url))) {
					AlternateMediaSource = alt;
					mediaElement.SetSource(AlternateMediaSource.MediaSource);
					found = true;

					alt.SetInitialBitrate(MediaStreamType.Video, 1100000);

					//found the one
					break;
				}
			}

			if (!found) {
				//Assign the source directly from the playlist url
				mediaElement.Source = new Uri(CurrentItem.Url);
				// Set altMediaStreamSource to null, it is not used for non-adaptive streams
				AlternateMediaSource = null;
			}

			lastSource = CurrentItem.Url;

			if (menuOptions != null) menuOptions.SetEnabled("Chapters", CurrentItem.Chapters.Count > 0);

			if (isPlaying || AutoPlay) {
				Play();
			}
		}

		void OnAdaptiveSourcePlayBitrateChange(object sender, BitrateChangedEventArgs e) {
			log.Output(OutputType.Debug, "Bitrate in use: " + e.StreamType + ", " + StringTools.FriendlyBitsPerSec((int)e.Bitrate));
			UpdateAdaptiveSegments();
			AdjustPlayerSize(PlayerSizeChangeType.Bitrate);
		}

		internal void OnPlaylistSizeChanged(object sender, SizeChangedEventArgs args) {
			AdjustPlaylist();
		}

		internal void OnQualityGaugeSizeChanged(object sender, SizeChangedEventArgs args) {
			OnControlBoxSizeChanged(null, null);
		}

		protected List<IPlugin> plugins = new List<IPlugin>();
		public IPlugin[] Plugins { get { return plugins.ToArray(); } }

		internal void On_PluginLoaded(object sender, PluginEventArgs args) {
			if (args == null) return;
			try {
				foreach (IPlugin ip in plugins) {
					if (ip.GetType() == args.PluginType) {
						plugins.Remove(ip);
						break;
					}
				}
				plugins.Add(args.Plugin);

				args.Plugin.Player = this;

				playerInitParser.RetryUnmatched(this);
				StartAutoPlay();

				//Special Case//
				if (args.Plugin as IAlternateMediaSource != null) {
					((IAlternateMediaSource)args.Plugin).PlayBitrateChange += OnAdaptiveSourcePlayBitrateChange;
					//go back where we left off now that the plugin is loaded
					SeekToPlaylistItem(CurrentIndex);
					return;
				}

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Failed to load plugin " + args.PluginType + ", ", ex);
			}
		}

		/// <summary>
		/// Returns the relative chapter index given a postition on the timeline.
		/// </summary>
		/// <param name="position">The position on the timeline to reference</param>
		/// <returns>the relative chapter position</returns>
		protected int ChapterIndexFromPosition(TimeSpan position) {
			double seconds = position.TotalSeconds;

			int indexChapter = 0;
			while (indexChapter < CurrentItem.Chapters.Count && CurrentItem.Chapters[indexChapter].Position < seconds) {
				indexChapter++;
			}
			return indexChapter;
		}

		/// <summary>
		/// Attempts to seek to the given chapter index and returns success.
		/// </summary>
		/// <param name="chapterIndex">The chapter index of the current item to seek to</param>
		/// <returns>True if successful</returns>
		protected bool SeekToChapterPoint(int chapterIndex) {
			if (chapterIndex >= 0 && chapterIndex < CurrentItem.Chapters.Count) {
				CurrentChapterIndex = chapterIndex;
				chapters.SelectedIndex = CurrentChapterIndex;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called for fullscreen support to stretch the player out
		/// </summary>
		private void PerformResize() {
			if ((Application.Current != null)) {
				if (Application.Current.Host.Content.IsFullScreen) {
					//start timer to close controls
					ShowControls = false;
					lastPadding = Padding;
					this.Padding = new Thickness(0);
					mainGrid.RowDefinitions[1].Height = mainGrid.RowDefinitions[2].Height = new GridLength(0);
				} else {
					ShowControls = true;
					ToolTipService.SetToolTip(buttonFullScreen, "FullScreen");
					Width = lastSize.Width;
					Height = lastSize.Height;
					Margin = lastMargin;
					HorizontalAlignment = lastHAlighn;
					VerticalAlignment = lastVAlighn;
					Padding = lastPadding;
					mainGrid.RowDefinitions[1].Height = mainGrid.RowDefinitions[2].Height = new GridLength(32);
					//OnControlBoxSizeChanged(this, null);
				}
			}
		}

		private void SetCustomToolTip(Point pt, string text, object tag) {
			toolTip.Text = text;
			toolTip.Visibility = Visibility.Visible;
			toolTip.Tag = tag;
			toolTip.SetValue(Canvas.LeftProperty, pt.X - (toolTip.ActualWidth));
			toolTip.SetValue(Canvas.TopProperty, pt.Y - toolTip.ActualHeight - 8);
		}

		public bool ControlsEnabled {
			get { return controlBox.Visibility == Visibility.Visible; }
			set { controlBox.Visibility = ((value) ? Visibility.Visible : Visibility.Collapsed); }
		}

		[ScriptableMember]
		/// <summary>
		/// IelementLists that are used for playlist, chapters, and logs but all have similar characteristics
		/// </summary>
		public LogViewer LogViewer {
			get { return logViewer; }
		}

		/// <summary>
		/// Internal method for adjusting the player size based on conditions, such as adaptive content in use, and browser window size
		/// TODO - consider using this with non-adaptive, just based on changing size of playlist items..
		/// TODO - make the divs that are also resized configgable or move to page implementation
		/// </summary>
		/// <param name="type"></param>
		void AdjustPlayerSize(PlayerSizeChangeType type) {
			if (AutoScaling == AutoScalingType.None) return; // || AlternateMediaSource == null || AlternateMediaSource.CurrentBitrate <= 0) return;

			List<Size> sizes = new List<Size>();
			IAdaptiveSegment[] segs = null;
			if (AlternateMediaSource != null && (segs = AlternateMediaSource.AvailableSegments(MediaStreamType.Video)) != null) {
				foreach (IAdaptiveSegment seg in AlternateMediaSource.AvailableSegments(MediaStreamType.Video)) sizes.Add(seg.Resolution);
			}
			if (sizes.Count == 0) sizes.Add(VideoResolution);

			for (int x = sizes.Count - 1; x >= 0; x--) {
				Size contentRes = sizes[x];
				//no empties//
				if (contentRes.Sum() <= 0) continue;

				//force 16x9 or other aspect?
				if (AutoScalingForcedAspect != Size.Empty && AutoScalingForcedAspect.Width != 0 && AutoScalingForcedAspect.Height != 0) {
					contentRes = new Size((int)(contentRes.Height * (AutoScalingForcedAspect.Width / AutoScalingForcedAspect.Height)), contentRes.Height); //1.77777777
				}

				//force over 480 lines, or other size
				if (contentRes.Sum() < AutoScalingMinimumSize.Sum() && x <= sizes.Count - 1) {
					contentRes = AutoScalingMinimumSize;
				}

				//get new size of player and browser for this stage - including margins
				Size plSize = new Size(contentRes.Width + AutoScalingPlayerMargin.Width, contentRes.Height + AutoScalingPlayerMargin.Height);
				Size bSize = new Size(plSize.Width + AutoScalingBrowserMargin.Width, plSize.Height + AutoScalingBrowserMargin.Height);

				//only consider HD resolutions, otherwise we force 848x480 for this site
				if (contentRes.Sum() > AutoScalingMinimumSize.Sum()) {
					//extra needed if sizing upward, to keep from awkward middle spots and flashing between sizes
					int extra = (plSize.Width > AutoScalingCalculatedPlayerSize.Width) ? 20 : 0;
					//compare with browser + margin
					if (BrowserSize.Width < bSize.Width + extra || BrowserSize.Height < bSize.Height + extra) continue;
					//make sure we've reached the bitrate below this level
					if (segs != null && x > 0 && (long)AlternateMediaSource.CurrentBitrate < segs[x - 1].Bitrate) continue;
					//if it drops through then we need to set our minimum
				}

				//make sure we aren't setting the same value again
				if (plSize == AutoScalingCalculatedPlayerSize) return;
				//only size upward from bitrate changes, not down.
				if (type == PlayerSizeChangeType.Bitrate && plSize.Width < AutoScalingCalculatedPlayerSize.Width) return;

				// :: we are officially going to change something :: //
				Debug.WriteLine("Setting size to: " + plSize + " from " + AutoScalingCalculatedPlayerSize + ", Browser is: " + BrowserSize);
				AutoScalingCalculatedPlayerSize = plSize;

				Dispatcher.BeginInvoke(delegate {
					//we're golden, set player size? or object size?
					if (AutoScaling == AutoScalingType.ScalePlayer) {
						Width = plSize.Width;
						Height = plSize.Height;
					} else {
						if (AutoScaling == AutoScalingType.ScaleObjectTag) {
							HtmlPage.Plugin.SetStyleAttribute("width", plSize.Width + "px");
							HtmlPage.Plugin.SetStyleAttribute("height", plSize.Height + "px");

						} else if (AutoScaling == AutoScalingType.ScaleParent) {
							HtmlPage.Plugin.Parent.SetStyleAttribute("width", plSize.Width + "px");
							HtmlPage.Plugin.Parent.SetStyleAttribute("height", plSize.Height + "px");
						}

						if (SizeChanged != null) SizeChanged(this, null);
					}

					//go native if we match our resolution
					if (segs != null && StretchMode != Stretch.None && plSize.Width > segs[segs.Length - 1].Resolution.Width) {
						lastStretchMode = StretchMode;
						StretchMode = Stretch.None;
						Debug.WriteLine("Switched mode to native");

					} else if (StretchMode == Stretch.None) {
						StretchMode = lastStretchMode;
						Debug.WriteLine("Switched mode from native to " + StretchMode);
					}
				});
				return;
			}
		}

		#endregion

		#region EventHandlers

		/// <summary>
		/// Starts up the player.  This is usually the last thing called from inside the host
		/// application.
		/// </summary>
		/// <param name="sender">The object calling the startup event</param>
		/// <param name="e">The Import parameters to use for loading the player config</param>
		public void OnStartup(object sender, StartupEventArgs e) {
			// Register this object to the control so the select methods are available
			// to javascript
			HtmlPage.RegisterScriptableObject(PLAYER_CONTROL_NAME, this);

			StartupArgs = e;
		}

		void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			//Debug.WriteLine("Exception: " + e.ExceptionObject);
			log.Output(OutputType.Error, "Error: ", e.ExceptionObject);
			e.Handled = true;
		}

		/// <summary>
		/// Adjust everything when the playlist is changed from overlay to external
		/// </summary>
		private void AdjustPlaylist() {
			double w = playlist.Parent.FullSize().Width;
			Thickness pm = playlist.Parent.Margin;

			//adjust margin when playlist shown
			if (playlist != null) {
				if (ShowPlaylist && !PlayListOverlay) {
					if (mainBorder.Margin.Right != w) {
						mainBorder.Margin = new Thickness(0, 0, w, 0);
					}
					if (pm.Bottom != 0) {
						playlist.Parent.Margin = new Thickness(pm.Left, pm.Top, pm.Right, 0);
					}
				} else if (!ShowPlaylist) {
					mainBorder.Margin = new Thickness(0);
					if (pm.Bottom != controlBox.ActualHeight) {
						playlist.Parent.Margin = new Thickness(pm.Left, pm.Top, pm.Right, controlBox.ActualHeight);
					}
				}
			}
		}

		public event EventHandler MediaEnded;

		internal void OnMediaElementCurrentStateChanged(object sender, RoutedEventArgs e) {
			if (mediaElement.CurrentState == lastMediaState) return;
			if (AdMode) return;

			lastMediaState = mediaElement.CurrentState;

			// If we're playing make the play element invisible and the pause element visible, otherwise invert.
			if (mediaElement.CurrentState == MediaElementState.Playing) {

				playToggle.Visibility = Visibility.Collapsed;
				pauseToggle.Visibility = Visibility.Visible;
				messageBox.Visibility = Visibility.Collapsed;

				//	isPlaying = true;
				ToolTipService.SetToolTip(buttonPlayPause, "Pause");
			} else if (mediaElement.CurrentState == MediaElementState.Opening) {
			} else if (mediaElement.CurrentState == MediaElementState.Closed) {
			} else {
				if (mediaElement.CurrentState == MediaElementState.Stopped) {
					if(Position >= Duration) {
					    if(MediaEnded !=null) {
							MediaEnded(this, new EventArgs());
					    }
					}
				}
				//if (!AdMode) {
					playToggle.Visibility = Visibility.Visible;
					pauseToggle.Visibility = Visibility.Collapsed;

					//	isPlaying = false;
					ToolTipService.SetToolTip(buttonPlayPause, "Play");
				//}
			}

			if (bufferIcon != null) {
				bool buffering = (mediaElement.CurrentState == MediaElementState.Buffering && AlternateMediaSource == null);
				if (buffering != bufferIcon.Active) {
					Dispatcher.BeginInvoke((Action)delegate { bufferIcon.Active = buffering; });
				}
			}

			//update button visibility
			if (AutoHideNextAndPrevious) {
				if (buttonNext != null && buttonPrevious != null) {
					buttonNext.Visibility = buttonPrevious.Visibility = (playlist.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
					OnControlBoxSizeChanged(this, null);
				}
			}

			//Debug.WriteLine("State: " + mediaElement.CurrentState);
			log.Output(OutputType.Debug, "State: " + mediaElement.CurrentState);
		}

		internal void OnMediaElementMediaOpened(object sender, RoutedEventArgs e) {
			if (AdMode) return;

			if (isPlaying) {
				Play();
			} else {
				Pause();//race condition?
				return;
			}

			//see if we can get info about the server and client ips, only supported by certain edge servers
			if (CheckEdgeServerIP) {
				WebClient wc = new WebClient();
				wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(OnCheckServerIP_DownloadStringCompleted);
				Uri u = new Uri(CurrentSource);
				Uri sIp = new Uri(string.Format("{0}://{1}{2}/ServerIP", u.Scheme, u.Host, (u.Port>0?":"+u.Port:"")));
				wc.DownloadStringAsync(sIp);
			}

			PerformResize();

			scrubberBar.DownloadPercent = 0.0;

			if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.NaturalDuration.TimeSpan > TimeSpan.Zero) {
				TimeSpan dur = mediaElement.NaturalDuration.TimeSpan;
				//PlaybackDuration = dur.TotalSeconds;
				scrubberBar.Maximum = dur.TotalSeconds;

				PlaybackDurationText = (dur.Hours >= 1) ? string.Format("{0}:{1}:{2}", dur.Hours.ToString("00"), dur.Minutes.ToString("00"), dur.Seconds.ToString("00"))
					: string.Format("{0}:{1}", dur.Minutes.ToString("00"), dur.Seconds.ToString("00"));
			} else {
				PlaybackDurationText = "(Live)";
			}

			playlist.SelectedIndex = CurrentIndex;
			messageBox.Visibility = Visibility.Collapsed;

			SetButtonStatus();

			if (menuOptions != null) menuOptions.SetVisible("Playlist", Playlist.Count > 1);
			if (menuOptions != null) menuOptions.SetVisible("Chapters", (CurrentItem.Chapters != null && CurrentItem.Chapters.Count > 0));
			if (buttonPlaylist != null) buttonPlaylist.Visibility = (Playlist.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
			if (buttonChapters != null) buttonChapters.Visibility = (CurrentItem.Chapters != null && CurrentItem.Chapters.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
			OnControlBoxSizeChanged(null, null);

			log.Output(OutputType.Info, "Opened: " + CurrentIndex + ", " + CurrentSource);

			VideoResolution = new Size(mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight);

			if (AlternateMediaSource != null) {
				UpdateAdaptiveSegments();
				//AdaptiveFigureMaxResolution();
			}

			if (MediaOpened != null) {
				MediaOpened(this, new EventArgs());
			}
		}

		void OnCheckServerIP_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if (!e.Cancelled && e.Error == null && e.Result !=null) {
				if (e.Result.Contains("<diagnostics>")) {
					log.Output(OutputType.Debug, string.Format("Diagnostics: Client IP={0}, Edge Server IP={1} ",
					                                           e.Result.Section("<clientip>", "</clientip>"),
					                                           e.Result.Section("<serverip>", "</serverip>")));
				}
			}
		}

		private void SetButtonStatus() {
			if (buttonNext != null) {
				if (playlist.SelectedIndex < playlist.Count - 1) {
					buttonNext.IsEnabled = true;
					buttonNext.Opacity = 1.0;
				} else {
					buttonNext.IsEnabled = false;
					buttonNext.Opacity = .5;
				}
			}
			if (buttonPrevious != null) {
				if (playlist.SelectedIndex > 0) {
					buttonPrevious.IsEnabled = true;
					buttonPrevious.Opacity = 1.0;
				} else {
					buttonPrevious.IsEnabled = false;
					buttonPrevious.Opacity = .5;
				}
			}
		}

		private void UpdateAdaptiveSegments() {
			string brs = "";
			IAdaptiveSegment[] AdaptiveSegments = AlternateMediaSource.AvailableSegments(MediaStreamType.Video);
			foreach (IAdaptiveSegment seg in AdaptiveSegments) {
				brs += seg.Bitrate / 1024 + ",";
				if (seg.Selected = (seg.Bitrate == (long)AlternateMediaSource.CurrentBitrate)) {
					VideoResolution = seg.Resolution;
				}
			}
			if (brs.Length > 1) bitrateString = brs.Substring(0, brs.Length - 1);
		}

		void OutputLog_StaticOutputEvent(OutputEntry outputEntry) {
			logList.Add(outputEntry);
			while (logList.Count > 999) logList.RemoveAt(0);
		}

		internal void OnMediaElementMediaEnded(object sender, RoutedEventArgs e) {
			SeekToNextItem();
		}

		internal void OnMediaElementMediaFailed(object sender, ExceptionRoutedEventArgs e) {//RoutedEventArgs e) {
			//Debug.WriteLine("Content Failed! ");
			string error = (e as ExceptionRoutedEventArgs != null) ? "Message: " + ((ExceptionRoutedEventArgs)e).ErrorException.Message : null;
			MessageText = string.Format("Error opening {0}\n{1}{2}", CurrentSource, (error ?? "(Unknown Error)"), (Playlist.Count > CurrentIndex + 1) ? "\n\nTrying next playlist item..." : "");
			log.Output(OutputType.Error, MessageText);

			playSymbol.Visibility = Visibility.Collapsed;
			messageBox.Visibility = Visibility.Visible;
			messageBoxText.FontSize = 12;
			messageBox.Opacity = .8;
			messageBoxText.Foreground = new SolidColorBrush(Colors.Red);
			messageBox.Height = messageBoxText.ActualHeight + 30;
			SeekToNextItem();
		}

		internal void OnMediaElementBufferingProgressChanged(object sender, RoutedEventArgs e) {
			//updateBuffering = true;
			lastBufferProgress = mediaElement.BufferingProgress;
		}

		internal void OnMediaElementDownloadProgressChanged(object sender, RoutedEventArgs e) {
			//updateDownloading = true;
			
			lastDownloadProgress = mediaElement.DownloadProgress;
			if (scrubberBar.DownloadPercent == 0 && lastDownloadProgress == 1) {
			} else {
				scrubberBar.DownloadPercent = lastDownloadProgress;
			}
		}

		void OnBoxClosed(object sender, RoutedEventArgs e) {
			if (playlist != null && sender == playlist.Parent) {
				AdjustPlaylist();
			}

			if (sender == logViewer.Parent) {
				OptionsMenu.SetCheckState("Logs", false);
				OptionsMenu.SetCheckState("Statistics", false);
			}
		}


		void On_SizeChanged(object sender, SizeChangedEventArgs e) {
			if (SizeChanged != null) SizeChanged(sender, e);
		}

		void On_BindingValidationError(object sender, ValidationErrorEventArgs e) {
			throw new NotImplementedException();
		}

		internal void On_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			if (Application.Current.Host.Content.IsFullScreen) {
				//Debug.WriteLine("Move: " + e.GetPosition(mainBorder) + ", " + mainBorder.ActualHeight);
				lastMouseMove = DateTime.Now;
				if (!ShowControls) {
					if (e.GetPosition(mediaElement).Y > mediaElement.ActualHeight || e.GetPosition(mainBorder).Y > mainBorder.ActualHeight - 25) {
						ShowControls = true;
					}
				} else {
					if (e.GetPosition(controlBox).Y < controlBox.ActualHeight - 100) {
						ShowControls = false;
					}
				}
			}
		}

		internal void OnButtonClickPause(object sender, RoutedEventArgs e) {
			Pause();
			CollapseMenus();
		}

		internal void OnButtonClickPlay(object sender, RoutedEventArgs e) {
			Play();
			CollapseMenus();
		}

		internal void OnButtonPlayPauseClick(object sender, RoutedEventArgs e) {
			TogglePlayPause();
			CollapseMenus();
		}

		internal void OnButtonStopClick(object sender, RoutedEventArgs e) {
			Stop();
		}

		internal void OnButtonPlaylistClick(object sender, RoutedEventArgs e) {
			ShowPlaylist = !ShowPlaylist;
		}

		internal void OnButtonChaptersClick(object sender, RoutedEventArgs e) {
			ShowChapters = !ShowChapters;
		}

		/// <summary>
		/// Event callback, wraps the Scriptable SeekToPreviousChapter() method
		/// </summary>
		/// <param name="sender">The object calling the event</param>
		/// <param name="e">Args</param>
		internal void OnButtonPreviousClick(object sender, RoutedEventArgs e) {
			SeekToPreviousChapter();
		}

		/// <summary>
		/// Event callback, supports a Next button by seeking to the next chapter or the
		/// next Item if no further chapters are available.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnButtonNextClick(object sender, RoutedEventArgs e) {
			if (CurrentItem.Chapters.Count > 1) {
				SeekToNextChapter();
			} else {
				SeekToNextItem();
			}
		}

		/// <summary>
		/// Event callback to update the mediaElement with the volume's current value
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnSliderVolumeValueChanged(object sender, RoutedPropertyChangedEventArgs<Double> e) {
			mediaElement.Volume = sliderVolume.Value;
			if (!isMuted) lastUsedVolume = sliderVolume.Value;
			Muted = sliderVolume.Value < LOWER_VOLUME_THRESHOLD;
			CollapseMenus();
		}

		/// <summary>
		/// Event callback, supports a Mute button by storing off the last used volume
		/// and setting the volume to 0 or vice-versa.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnButtonMuteClick(object sender, RoutedEventArgs e) {
			Muted = !Muted;
			CollapseMenus();
		}


		/// <summary>
		/// Event callback, supports fullscreen toggling by wrapping the PerformResize method.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnFullScreenChanged(object sender, EventArgs e) {
			PerformResize();
			if (FullScreenChanged != null) FullScreenChanged(this, new RoutedEventArgs());

			if (Application.Current.Host.Content.IsFullScreen) {
				highlightBorder.Stroke.Opacity = 0.0;
				if (StretchModeFullScreen != StretchMode) {
					mediaElement.Stretch = StretchModeFullScreen;
				} else {
					//special logic to stay native if the screen is that size, and then go to normal scaling for FS mode
					if (mediaElement.Stretch == Stretch.None && StretchMode != Stretch.None) {
						//StretchMode = lastStretchMode;
						//lastStretchMode = Stretch.None;
						//overrideStretch = true;
						mediaElement.Stretch = StretchMode;
						Debug.WriteLine("Switched mode from native to " + StretchMode + " for FS");
					}
				}
			} else {
				highlightBorder.Stroke.Opacity = 1.0;
				if (StretchModeFullScreen != StretchMode && !overrideStretch) {
					mediaElement.Stretch = StretchMode;
				} else {
					//back to native
					if (overrideStretch && mediaElement.Stretch != Stretch.None){// && lastStretchMode == Stretch.None) {
						//lastStretchMode = StretchMode;
						//StretchMode = Stretch.None;
						mediaElement.Stretch = Stretch.None;
						Debug.WriteLine("Switched mode back to native");
					}
				}
			}
		}

		/// <summary>
		/// Event callback, causes the player to go fullscreen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnButtonFullScreenClick(object sender, RoutedEventArgs e) {
			ToggleFullscreen();
			CollapseMenus();
		}

		private void CollapseMenus() {
			if (menuOptions != null) menuOptions.Hide();
		}

		/// <summary>
		/// Event callback, fires when the user changes the playlist item
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnListBoxPlaylistSelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (playlist.SelectedIndex < 0) return;
			if (playlist.SelectedIndex != CurrentIndex) {
				SeekToPlaylistItem(playlist.SelectedIndex);
			}
			if (PlayListOverlay) {
				if (isPlaying) ShowPlaylist = false; // hides the playlist
				AdjustPlaylist();
			}
		}

		/// <summary>
		/// Event callback, fires when the user changes the current chapter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void OnListBoxChaptersSelectionChanged(object sender, SelectionChangedEventArgs e) {
			CurrentChapterIndex = chapters.SelectedIndex;
			if (CurrentChapterIndex >= 0) {
				Seek(TimeSpan.FromSeconds(CurrentChapter.Position));
			}
		}

		/// <summary>
		/// Event callback, fires when the mediaElement hits a marker.  Markers may or may not be
		/// defined for each item in the playlist.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Param</param>
		internal void OnMediaElementMarkerReached(object sender, TimelineMarkerRoutedEventArgs e) {
			// Marker types could trigger add points, captions, interactions or chapters
			switch (MarkerTypeConv.StringToMarkerType(e.Marker.Type)) {
				case MarkerTypes.Chapter:
					if (chapters != null) {
						CurrentChapterIndex = ChapterIndexFromPosition(e.Marker.Time);
					}
					break;

				case MarkerTypes.Caption:
					break;

				case MarkerTypes.Interrupt:
					break;

				case MarkerTypes.Unknown:
					Debug.WriteLine("Unknown marker type:" + e.Marker.Type);
					break;
			}

		}

		void Playlist_LoadComplete(object sender, RoutedEventArgs e) {
			//try this here?
			StartAutoPlay();
		}

		internal void OnControlBoxMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			lastMouseMove = DateTime.Now;
		}

		internal void OnControlBoxSizeChanged(object sender, SizeChangedEventArgs e) {
			Double sum = 0;
			foreach (FrameworkElement fe in controlBox.Children) {
				if (fe != null && fe.Name != "scrubberBar" && fe.Visibility == Visibility.Visible) sum += fe.FullSize().Width;
			}
			double w = MainBorder.ActualWidth - sum - scrubberBar.Margin.Right - scrubberBar.Margin.Right - (MainBorder.BorderThickness.Right + MainBorder.BorderThickness.Left);
			if (scrubberBar != null) {
				if (w > scrubberBar.MinWidth) {
					scrubberBar.Width = w;
				} else {
					scrubberBar.Width = scrubberBar.MinWidth;
				}
			}
		}

		internal void On_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			CollapseMenus();
		}

		internal void OnMenuOptionsItemCheckedChanged(object sender, RoutedEventArgs e) {
			MenuItem m = sender as MenuItem;

			if (m == null) return;
			MenuItem t = (m.Menu.Target != null) ? m.Menu.Target as MenuItem : null;
			Debug.WriteLine("Item changed: " + m.Text + ", " + m.Checked);

			if (m.Checked) {
				if (t != null && t.Text == "Scaling") {
					StretchMode = (Stretch)Enum.Parse(typeof(Stretch), m.Tag.ToString(), true);
				}
			}

			if (m.Text == "Statistics") ShowStatistics = (m.Checked);// ? Visibility.Visible : Visibility.Collapsed;
			if (m.Text == "Logs") ShowLogViewer = (m.Checked);// ? Visibility.Visible : Visibility.Collapsed;
		}

		internal void OnMenuOptionsItemClick(object sender, RoutedEventArgs e) {
			MenuItem m = sender as MenuItem;
			if (m == null) return;

			if (m.Text == "Playlist") ShowPlaylist = !ShowPlaylist;//TogglePlaylist();
			if (m.Text == "Chapters") ShowChapters = !ShowChapters;//ToggleChapters();
		}

		internal void OnMessageBoxMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			SurfaceClick();
		}

		internal void OnMediaElementMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			SurfaceClick();
		}

		/// <summary>
		/// should be called from handlers anywhere we receive clicks that should be used for pause/play toggle and fullscreen doubleclick actions
		/// </summary>
		private void SurfaceClick() {
			//Debug.WriteLine("Click received from : " + sender.GetType());

			if (menuOptions != null && menuOptions.Visibility == Visibility.Visible) {
				menuOptions.Hide();
				return;
			}

			if (DateTime.Now - lastClick < TimeSpan.FromMilliseconds(10)) {
				//duplicate
				return;
			}
			if (DateTime.Now - lastClick < TimeSpan.FromMilliseconds(250)) {
				//this means a double click..
				lastClick = DateTime.MinValue;
				waitOnClick = false;
				OnButtonFullScreenClick(this, new RoutedEventArgs());
			} else {
				//if there isnt another click, this will get picked up by our dispatcher timer to make a single click
				waitOnClick = true;
				lastClick = DateTime.Now;
			}
		}

		internal void OnScrubberBarValueChanged(object sender, RoutedPropertyChangedEventArgs<Double> e) {
			if (!timerIsUpdating) {
				Seek(TimeSpan.FromSeconds(scrubberBar.Value));
			}
		}

		internal void OnScrubberBarValueChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			if (!timerIsUpdating) {
				Seek(TimeSpan.FromSeconds(e.Value));
			}
		}

		public void Seek(TimeSpan t) {
			mediaElement.Position = t;

			if(MediaCommand!=null){
				MediaCommand(this, new MediaCommandEventArgs(){ Command = MediaCommandType.Seek});
			}
		}

		internal void OnSliderVolumeValueChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			sliderVolume.Value = e.Value;
		}

		internal void OnSliderVolumeMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			Point pt = e.MouseArgs.GetPosition(this);
			Double val = (e.Value > 1) ? 1 : e.Value;

			string text = string.Format("Volume: {0}%", (int)(val * 100));

			SetCustomToolTip(pt, text, sender);

			if (e.MousePressed) {
				sliderVolume.Value = val;
			}
		}

		internal void OnSliderVolumeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		private void HideTooltip() {
			//if (customToolTip.Opacity > 0) customToolTip.Opacity = 0;
			toolTip.Visibility = Visibility.Collapsed;
		}

		internal void OnScrubberBarMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		internal void OnQualityGaugeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		internal void OnQualityGaugeMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			if (qualityGauge.Tag == null || qualityGauge.Tag.ToString() == "") qualityGauge.Tag = "Standard-Definition (loading)\n\n\n";
			SetCustomToolTip(e.GetPosition(this), qualityGauge.Tag.ToString(), qualityGauge);
		}

		internal void OnScrubberBarMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			if (!scrubberBar.IsEnabled || AdMode) return;

			Point pt = e.MouseArgs.GetPosition(this);
			TimeSpan ts = TimeSpan.FromSeconds(e.Value);
			string text = "Seek to: " + string.Format("{0}:{1}:{2}", ts.Hours.ToString("00"), ts.Minutes.ToString("00"), ts.Seconds.ToString("00"));

			SetCustomToolTip(pt, text, sender);

			if (e.MousePressed && e.Value < scrubberBar.Maximum && e.Value >= 0) {
				scrubberBar.Value = e.Value;
			}
		}

		internal void OnButtonLinkEmbed_Click(object sender, RoutedEventArgs e) {
			FrameworkElement fe = linkEmbed.Parent as FrameworkElement;
			if (fe != null) fe.Visibility = (fe.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
		}

		internal void OnButtonMenuClick(object sender, RoutedEventArgs e) {
			if (menuOptions == null) return;

			menuOptions.Target = buttonMenu;
			menuOptions.Toggle();
			HideTooltip();
		}

		#endregion

		#region Timers and their associated methods

		public void AsyncLoop() {
			while (true) {

				if (sw.Throttle("threadTimer", 3)) {//every 3 sec
					OnThreadTimerTick();
				}

				Dispatcher.BeginInvoke(OnUiTimerTick);

				Thread.Sleep(500);
				//Thread.Sleep(100);
			}
		}


		/// <summary>
		/// Fires when our non-ui timer ticks, do as much here as possible vs. the other timer
		/// </summary>
		/// <param name="sender"></param>
		protected void OnThreadTimerTick() {
			if (!Monitor.TryEnter(timerLock)) return;
			try {

				UpdateStatistics();

			} catch (Exception ex) {
				Dispatcher.BeginInvoke((Action)delegate {log.Output(OutputType.Error, "Timer Error: ", ex);});

			} finally {
				Monitor.Exit(timerLock);
			}
		}

		private void UpdateStatistics() {
			//check adaptive rate, right now just put in our rendered fps
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("OpenVideoPlayer v{0} ({1})", Version, lastMediaState);

			if (lastDownloadProgress > 0 && lastDownloadProgress < 1) {
				sb.AppendFormat("\nDownload progress: {0}%", (int)(100 * lastDownloadProgress));
			}

			int dropPerc = (FPSTotal > 0) ? (int)Math.Round(100 * (FPSDropped / FPSTotal), 0) : 0;
			if (isPlaying) sb.AppendFormat("\n{3} - Framedrop: {0}% ({1}/{2} FPS)", dropPerc, FPSDropped, FPSTotal, StringTools.SizetoString(VideoResolution));
			
			if (AlternateMediaSource != null) {
				sb.AppendFormat("\nCurrent Bitrate: {0}", StringTools.FriendlyBitsPerSec((int)AlternateMediaSource.CurrentBitrate));

				if (!string.IsNullOrEmpty(bitrateString)) sb.AppendFormat("\n{0} kbps", bitrateString);

				sb.AppendFormat("\nAvailable bandwidth:  {0}", StringTools.FriendlyBitsPerSec((int)AlternateMediaSource.CurrentBandwidth));
				IBufferInfo buffer = AlternateMediaSource.BufferInfo(MediaStreamType.Video);
				sb.AppendFormat("\nCurrent Buffer: {0}, {1} sec", StringTools.FriendlyBytes((long)buffer.Size), Math.Round(buffer.Time.TotalSeconds, 1));

				AdaptiveStats newStats = new AdaptiveStats();

				newStats.Segments = AlternateMediaSource.AvailableSegments(MediaStreamType.Video);
				if (newStats.Segments.Length > 0) {

					newStats.MaxBitrate = newStats.Segments[newStats.Segments.Length - 1].Bitrate; // -bitrates[0];
					newStats.CapBitrate = (AlternateMediaSource.SegmentCapIndex > -1) ? newStats.Segments[AlternateMediaSource.SegmentCapIndex].Bitrate : 0;
					newStats.BitratePercent = (newStats.MaxBitrate > 0) ? (double)AlternateMediaSource.CurrentBitrate / ((newStats.CapBitrate > 0 && newStats.CapBitrate < newStats.MaxBitrate) ? newStats.CapBitrate : newStats.MaxBitrate) : 0.0;
					newStats.CapBitrateSize = newStats.Segments[AlternateMediaSource.SegmentCapIndex].Resolution;
			
					int qualPercent = (int)Math.Round(newStats.BitratePercent * 100, 0);

					string maxStr = (newStats.CapBitrate > 0 && newStats.CapBitrate < newStats.MaxBitrate) ? string.Format("Capped at {0} ({1})\n", StringTools.SizetoString(newStats.CapBitrateSize), StringTools.FriendlyBitsPerSec((int)newStats.CapBitrate)) : "";

					lastQualityTag = string.Format("{0} :: {1}x{2}\n{8}Bitrate: {3} ({4}% of max)\nAvailable bandwidth: {9}\nFramedrop: {5}% ({6}/{7} FPS)",
											   ((VideoResolution.Height >= 480) ? "High-Definition".ToUpper() : "Standard-Definition".ToUpper()),
											   VideoResolution.Width, VideoResolution.Height,
											   StringTools.FriendlyBitsPerSec((int)AlternateMediaSource.CurrentBitrate),
											   qualPercent, dropPerc, FPSDropped, FPSTotal, maxStr, StringTools.FriendlyBitsPerSec((int)AlternateMediaSource.CurrentBandwidth));
				}

				currentStats = newStats;

			} else {
				lastQualityTag = string.Format("{0} :: {1}x{2}\nFramedrop: {3}% ({4}/{5} FPS)",
				                           ((VideoResolution.Height >= 480) ? "High-Definition".ToUpper() : "Standard-Definition".ToUpper()),
				                           VideoResolution.Width,
				                           VideoResolution.Height,
				                           dropPerc, FPSDropped, FPSTotal
					);
				currentStats = null;
			}

			lastStats = sb.ToString();
			//Debug.WriteLine(sb.ToString());
		}

		public void OnUiTimerTick(){//(object sender, EventArgs e) {
			if (!Monitor.TryEnter(uiTimerLock)) return;
			try {
				if (mediaElement == null) return;
				DateTime start = DateTime.Now;
				timerIsUpdating = true;

				UpdateFrameRates();
				CheckMouseActions();

				//every loop (250ms) unless framedrop is high, then every 4 sec
				if (FPSDropped < .15 || sw.Throttle("uiTimer", 3)) {
					UpdatePosition();
					SyncChapters();
					UpdateDebugPanel();
				}

				//Debug.WriteLine("Update time: " + (DateTime.Now - start).TotalMilliseconds);
			} catch (Exception ex) {
				log.Output(OutputType.Error, "UI Timer Error: ", ex);

			} finally {
				Monitor.Exit(uiTimerLock);
			}
			timerIsUpdating = false;
		}

		private void UpdateFrameRates() {
			FPSRendered = mediaElement.RenderedFramesPerSecond;
			FPSDropped = mediaElement.DroppedFramesPerSecond;
			FPSTotal = FPSDropped + FPSRendered;
		}

		private void CheckMouseActions() {
			//Catch single clicks for play pause
			if (waitOnClick && DateTime.Now - lastClick >= TimeSpan.FromMilliseconds(250)) {
				waitOnClick = false;
				TogglePlayPause();
			}

			//hide controls if fullscreeen and mouse stops moving
			if (ShowControls && Application.Current.Host.Content.IsFullScreen && DateTime.Now - lastMouseMove > TimeSpan.FromSeconds(5)) {
				ShowControls = false;
			}
		}

		private void UpdatePosition() {
			//set scrubber position
			TimeSpan pos = mediaElement.Position;
			scrubberBar.Value = pos.TotalSeconds;

			if (sw.Throttle("sizeCheck",2)) {

				CheckSizing();

				//check for non-live..
				if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.NaturalDuration.TimeSpan > TimeSpan.Zero) {
					lastDur = mediaElement.NaturalDuration.TimeSpan;
					scrubberBar.IsEnabled = (!AdMode && lastDur != TimeSpan.Zero);
					if (lastDur.TotalSeconds != scrubberBar.Maximum) scrubberBar.Maximum = lastDur.TotalSeconds;

					//hack - sometimes gets into a wierd state.  need to look more at this
					if (pos.TotalSeconds > lastDur.TotalSeconds) {
						if (FPSRendered == 0) {
							if (CurrentIndex < Playlist.Count - 1) {
								SeekToNextItem();
							} else {
								Position = TimeSpan.Zero;
								Pause();
							}
						}
					}
				}
			}

			//set position text
			PlaybackPositionText = (lastDur.Hours >= 1) 
				? string.Format("{0}:{1}:{2}", pos.Hours.ToString("00"), pos.Minutes.ToString("00"), pos.Seconds.ToString("00"))
			    : string.Format("{0}:{1}", pos.Minutes.ToString("00"), pos.Seconds.ToString("00"));
		}

		private void CheckSizing() {
			
			MediaElementSize = new Size(mediaElement.ActualWidth, mediaElement.ActualHeight);

			//check browser size, in case our parent wants to resize us dynamically
			Size newBrowserSize = new Size(BrowserScreenInfo.ClientWidth, BrowserScreenInfo.ClientHeight);
			if (newBrowserSize != BrowserSize) {
				BrowserSize = newBrowserSize;
				if (BrowserSizeChanged != null) BrowserSizeChanged(this, new EventArgs());
				AdjustPlayerSize(PlayerSizeChangeType.Size);

			} else if (mediaElement.ActualHeight > this.ActualHeight) {
				AdjustPlayerSize(PlayerSizeChangeType.Size);
			}

			if (lastMediaHeight != MediaElement.ActualHeight) {
				lastMediaHeight = MediaElement.ActualHeight;
				if (SizeChanged != null) SizeChanged(this, null);
			}

			//check controlbox width (event isnt reliable)
			if (controlBox.Visibility == Visibility.Visible && controlBox.ActualWidth != MainBorder.ActualWidth - (MainBorder.BorderThickness.Right + MainBorder.BorderThickness.Left)) {
				OnControlBoxSizeChanged(null, null);
			}
		}

		private void SyncChapters() {
			//make sure playlist is synced
			if (chapters != null && CurrentChapterIndex >= 0 && CurrentChapterIndex < chapters.Count && chapters.SelectedIndex != CurrentChapterIndex) {
				// set the currently playing chapter on the list box without triggering our events
				chapters.SelectionChanged -= OnListBoxChaptersSelectionChanged;
				chapters.SelectedIndex = CurrentChapterIndex;
				chapters.SelectionChanged += OnListBoxChaptersSelectionChanged;

				// move that into view
				chapters.ScrollIntoView(CurrentItem.Chapters[CurrentChapterIndex]);//listBoxChapters.Items[currentlyPlayingChapter]);
			}
		}

		private void UpdateDebugPanel() {
			if ((currentStats == null || currentStats.LastUpdated > lastDebugUpdate) && sw.Throttle("debugUpdate",3)) {
				//lastDebugUpdate = DateTime.Now;
				try {

					if (qualityGauge != null) {
						int dropPerc = (FPSTotal > 0) ? (int)Math.Round(100 * (FPSDropped / FPSTotal), 0) : 0;

						if (AlternateMediaSource != null && currentStats != null) {
							//TODO - option for update here
							qualityGauge.Levels = currentStats.Segments.Length;
							qualityGauge.Value = currentStats.BitratePercent;
							qualityGauge.Foreground = (/*(VideoResolution.Height < 300 && currentStats.CapBitrateSize.Height > 300) || */dropPerc >= 25) ? qualityGauge.Red : (dropPerc >= 14) ? qualityGauge.Yellow : (VideoResolution.Height >= 480 || VideoResolution.Height >= currentStats.CapBitrateSize.Height) ? qualityGauge.Green : qualityGauge.White;

							double qw = (qualityGauge.Levels * qualityGauge.GridThickness) + qualityGauge.Margin.Left + qualityGauge.Margin.Right;
							//if (qw < 24) qw = 24;
							if (qw != qualityGauge.ActualWidth) qualityGauge.Width = qw;

						} else {
							qualityGauge.Levels = 1;
							qualityGauge.Width = 24;
							qualityGauge.Foreground = (/*(VideoResolution.Height < 300) || */dropPerc >= 25) ? qualityGauge.Red : (dropPerc >= 14) ? qualityGauge.Yellow : (VideoResolution.Height >= 480) ? qualityGauge.Green : qualityGauge.White;
						}

						qualityGauge.Tag = lastQualityTag;
					}

					//update tooltip in action 
					if (toolTip.Visibility == Visibility.Visible && toolTip.Tag == qualityGauge) {
						if (lastQualityTag != null) toolTip.Text = lastQualityTag;
					}

					if (lastStats != null) LogViewer.Stats = lastStats;//stats.Text = lastStats;

					//log.Output(OutputType.Debug, "Update debug took : " + (DateTime.Now - lastDebugUpdate).TotalMilliseconds + "ms");
				} catch (Exception ex) {
					log.Output(OutputType.Debug, "Update error", ex);
				}
			}
		}


		#endregion
	}
}
