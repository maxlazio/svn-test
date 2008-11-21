using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Browser;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Util;
using org.OpenVideoPlayer.Player.Visuals;

namespace org.OpenVideoPlayer.Player {
	[ScriptableType]
	public class OpenVideoPlayerControl : Control, IMediaControl {

		#region Constructor
		/// <summary>
		/// Creates a new instance of the OpenVideoPlayer control
		/// </summary>
		public OpenVideoPlayerControl() {
			Application.Current.UnhandledException += Current_UnhandledException;
			/***
			 * Setup all our default values and initialize all internal properties.
			 */
			DefaultStyleKey = typeof(OpenVideoPlayerControl);
			SetValue(PlaylistProperty, new PlaylistCollection());

			currentlyPlayingItem = -1;

			PlaybackPosition = 0;
			PlaybackDuration = 0;
			PlaybackDurationText = PlaybackPositionText = "00:00:00";

			BufferingControlVisibility = Visibility.Collapsed;
			DebugVisibility = Visibility.Collapsed;

			BufferingPercent = 0;
			DownloadOffsetPercent = 0;
			DownloadPercent = 0;

			lastUsedVolume = START_VOLUME;

			stretchMode = Stretch.None;

			backgroundColor = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

			autoplaySetting = true;
			isMuted = false;

			string fn = Assembly.GetExecutingAssembly().FullName;
			if (fn != null) {
				int iv = fn.IndexOf("Version=");
				version = new Version(fn.Substring(iv+8, fn.IndexOf(" ", iv + 1) - iv -9));
			}
		}
		#endregion

		#region Constants

		//Supported marker type names
		private const Double START_VOLUME = 0.5;
		private const Double LOWER_VOLUME_THRESHOLD = 0.01;
		public const string PLAYER_CONTROL_NAME = "OpenVideoPlayerControl";

		#endregion

		#region private instance variables

		/// <summary>
		/// Stores any alternate media source objects to send directly to the
		/// video screen.
		/// </summary>
		private MediaStreamSource altMediaStreamSource;

		protected Border mainBorder;
		protected Grid mainGrid;

		protected ListBox optionsMenu;

		protected FrameworkElement playToggle;
		protected FrameworkElement pauseToggle;
		protected MediaElement mainMediaElement;

		protected Button playPauseButton;
		protected Button previousButton;
		protected Button nextButton;
		protected Button stopButton;
		protected Button buttonLinkEmbed;

		protected Grid itemsButton;
		protected Grid chaptersButton;
		protected Grid menuScaling;
		protected Grid menuDebug;

		protected Grid menuLogs;
		protected Grid menuStats;
		protected Grid menuStretch;
		protected Grid menuFit;
		protected Grid menuNative;
		protected Grid menuNativeSmaller;

		protected Border subMenuDebugBox;
		protected Border subMenuScalingBox;
		protected ListBox subMenuDebug;
		protected ListBox subMenuScaling;

		protected Button fullscreenButton;
		protected Button muteButton;
		protected Button menuButton;

		protected ScrubberBar volumeSlider;
		protected ScrubberBar scrubberBar;
		protected ListBox itemListBox;
		protected Border itemsContainer;
		protected ListBox chapterListBox;
		protected Border chaptersContainer;
		protected Border closePlaylist;
		protected Border closeChapters;
		protected Border closeLinkEmbed;

		protected Grid controlBox;
		protected Border debugBox;
		protected Border customToolTip;
		protected Border messageBox;
		protected TextBlock messageBoxText;

		protected Path playSymbol;

		protected TextBox embedText;
		protected TextBox linkText;

		protected Border linkEmbedBox;

		private Version version;

		protected bool autoplaySetting;
		protected bool timerIsUpdating;
		protected bool fullscreenSetting;

		protected Stretch stretchMode;
		protected Color backgroundColor;

		protected DispatcherTimer mainTimer;

		protected int currentlyPlayingItem;
		protected int currentlyPlayingChapter;
		protected bool isPlaying;

		protected bool updateBuffering;
		protected bool updateDownloading;

		// Volume
		protected bool isMuted;
		protected Double lastUsedVolume;

		//mouse clicks
		private bool waitOnClick;
		protected DateTime lastClick = DateTime.MinValue;

		private DateTime lastDebugUpdate = DateTime.Now;

		//cache these for better performance, although getting away from reflection would be nice
		private MethodInfo methodBitrates;
		private PropertyInfo propCurrentBitrate;
		private PropertyInfo propCurrentBandwidth;
		private MethodInfo methodBufferSize;
		private MethodInfo methodBufferTime;

		private StartupEventArgs startupArgs;

		private bool adaptiveInit;
		private ConstructorInfo adaptiveConstructor;

		#endregion

		#region properties

		public bool AutoPlay {
			get { return autoplaySetting; }
			set { autoplaySetting = value; }
		}

		public bool Muted {
			get { return isMuted; }
			set { 
				isMuted = value;
				if (!isMuted) {
					mainMediaElement.Volume = lastUsedVolume;
					volumeSlider.Value = lastUsedVolume;
					muteButton.Opacity = 1;
					ToolTipService.SetToolTip(this.muteButton, "Mute");
				} else {
					lastUsedVolume = mainMediaElement.Volume;
					mainMediaElement.Volume = 0;
					volumeSlider.Value = 0;
					muteButton.Opacity = .5;
					ToolTipService.SetToolTip(this.muteButton, "UnMute");
				}
			}
		}

		public Stretch StretchMode {
			get { return stretchMode; }
			set { 
				stretchMode = value;
				if(mainMediaElement!=null) mainMediaElement.Stretch = StretchMode;
				CheckMenuHighlights(); 
			}
		}

		public Color BackgroundColor {
			get { return backgroundColor; }
			set { backgroundColor = value; }
		}

		/// <summary>
		/// Gets/sets the factory for creating a custom media stream source for adaptive streaming
		/// </summary>
		public IAdaptiveStreamSourceFactory AdaptiveStreamSourceFactory { get; set; }

		/// <summary>
		/// Get's the object's playlist
		/// </summary>
		[System.ComponentModel.Category("Items")]
		public PlaylistCollection Playlist {
			get { return (PlaylistCollection)GetValue(PlaylistProperty); }
		}

		public Double PlaybackPosition {
			get { return (Double)GetValue(PlaybackPositionProperty); }
			set {
				SetValue(PlaybackPositionProperty, value);
				if(scrubberBar!=null)scrubberBar.Value = value;
			}
		}

		public String PlaybackPositionText {
			get { return (String)GetValue(PlaybackPositionTextProperty); }
			set { SetValue(PlaybackPositionTextProperty, value); }
		}

		public Double PlaybackDuration {
			get { return (Double)GetValue(PlaybackDurationProperty); }
			set { SetValue(PlaybackDurationProperty, value); }
		}

		public String PlaybackDurationText {
			get { return (String)GetValue(PlaybackDurationTextProperty); }
			set { SetValue(PlaybackDurationTextProperty, value); }
		}

		public Double BufferingPercent {
			get { return (Double)GetValue(BufferingPercentProperty); }
			set { SetValue(BufferingPercentProperty, value); }
		}

		public String BufferingImageSource {
			get { return (String)GetValue(BufferingImageSourceProperty); }
			set { SetValue(BufferingImageSourceProperty, value); }
		}

		public Visibility BufferingControlVisibility {
			get { return (Visibility)GetValue(BufferingControlVisibilityProperty); }
			set { SetValue(BufferingControlVisibilityProperty, value); }
		}

		public Double DownloadOffsetPercent {
			get { return (Double)GetValue(DownloadOffsetPercentProperty); }
			set { SetValue(DownloadOffsetPercentProperty, value); }
		}

		public Double DownloadPercent {
			get { return (Double)GetValue(DownloadPercentProperty); }
			set { SetValue(DownloadPercentProperty, value); }
		}

		public Visibility DownloadProgressControlVisibility {
			get { return (Visibility)GetValue(DownloadProgressControlVisibilityProperty); }
			set { SetValue(DownloadProgressControlVisibilityProperty, value); }
		}

		public String CaptionText {
			get { return (String)GetValue(CaptionTextProperty); }
			set { SetValue(CaptionTextProperty, value); }
		}

		public String BitRateText {
			get { return (String)GetValue(BitRateTextProperty); }
			set { SetValue(BitRateTextProperty, value); }
		}

		public String MessageText {
			get { return (String)GetValue(MessageTextProperty); }
			set { SetValue(MessageTextProperty, value); }
		}

		public String CustomToolTipText {
			get { return (String)GetValue(CustomToolTipTextProperty); }
			set { SetValue(CustomToolTipTextProperty, value); }
		}

		public String DebugLogText {
			get { return (String)GetValue(DebugLogTextProperty); }
			set { SetValue(DebugLogTextProperty, value); }
		}

		public Visibility DebugVisibility {
			get { return (Visibility)GetValue(DebugVisibilityProperty); }
			set {
				SetValue(DebugVisibilityProperty, value);
			}
		}

		#endregion

		#region Dependency Property fields
		/**
         * We register dependency property's as a storage for many of the control objects properties.
         */

		/// <summary>
		/// Dependency Property storage for the playlist
		/// </summary>
		public static readonly DependencyProperty PlaylistProperty =
			DependencyProperty.Register("Playlist", typeof(PlaylistCollection), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Dependency Property stores the current playback position
		/// </summary>
		public static readonly DependencyProperty PlaybackPositionProperty =
			DependencyProperty.Register("PlaybackPosition", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Dependency Property stores the current playback position text value
		/// </summary>
		public static readonly DependencyProperty PlaybackPositionTextProperty =
			DependencyProperty.Register("PlaybackPositionText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the total media duration
		/// </summary>
		public static readonly DependencyProperty PlaybackDurationProperty =
			DependencyProperty.Register("PlaybackDuration", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the total media duration text value
		/// </summary>
		public static readonly DependencyProperty PlaybackDurationTextProperty =
			DependencyProperty.Register("PlaybackDurationText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the percent-complete of buffering
		/// </summary>
		public static readonly DependencyProperty BufferingPercentProperty =
			DependencyProperty.Register("BufferingPercent", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the url for the overlay image to display when buffering
		/// </summary>
		public static readonly DependencyProperty BufferingImageSourceProperty =
			DependencyProperty.Register("BufferingImageSource", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the visibility level of the buffering control
		/// </summary>
		public static readonly DependencyProperty BufferingControlVisibilityProperty =
			DependencyProperty.Register("BufferingControlVisibility", typeof(Visibility), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the download offset
		/// </summary>
		public static readonly DependencyProperty DownloadOffsetPercentProperty =
			DependencyProperty.Register("DownloadOffsetPercent", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the download complete percentage
		/// </summary>
		public static readonly DependencyProperty DownloadPercentProperty =
			DependencyProperty.Register("DownloadPercent", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the visibility level of the download progress bar
		/// </summary>
		public static readonly DependencyProperty DownloadProgressControlVisibilityProperty =
			DependencyProperty.Register("DownloadProgressControlVisibility", typeof(Visibility), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores any caption text
		/// </summary>
		public static readonly DependencyProperty CaptionTextProperty =
			DependencyProperty.Register("CaptionText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the bitrate/diagnostics string.
		/// </summary>
		public static readonly DependencyProperty BitRateTextProperty =
			DependencyProperty.Register("BitRateText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));


		/// <summary>
		/// Depencency Property stores the bitrate/diagnostics string.
		/// </summary>
		public static readonly DependencyProperty CustomToolTipTextProperty =
			DependencyProperty.Register("CustomToolTipText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));


		/// <summary>
		/// Depencency Property stores the bitrate/diagnostics string.
		/// </summary>
		public static readonly DependencyProperty MessageTextProperty =
			DependencyProperty.Register("MessageText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property 
		/// </summary>
		public static readonly DependencyProperty DebugLogTextProperty =
			DependencyProperty.Register("DebugLogText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property 
		/// </summary>
		public static readonly DependencyProperty DebugVisibilityProperty =
			DependencyProperty.Register("DebugVisibility", typeof(Visibility), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		#endregion

		#region Template Methods

	/// <summary>
		/// Binds all the protected properties of the object into the template
		/// </summary>
		protected void BindTemplate() {
			mainBorder = GetTemplateChild("mainBorder") as Border;
			mainGrid = GetTemplateChild("mainGrid") as Grid;

			mainMediaElement = GetTemplateChild("mediaElement") as MediaElement;
			playPauseButton = GetTemplateChild("buttonPlayPause") as Button;
			pauseToggle = GetTemplateChild("pauseToggle") as FrameworkElement;
			playToggle = GetTemplateChild("playToggle") as FrameworkElement;
			previousButton = GetTemplateChild("buttonPrevious") as Button;
			nextButton = GetTemplateChild("buttonNext") as Button;
			stopButton = GetTemplateChild("buttonStop") as Button;
			fullscreenButton = GetTemplateChild("buttonFullScreen") as Button;
			scrubberBar = GetTemplateChild("scrubberBar") as ScrubberBar;
			muteButton = GetTemplateChild("buttonMute") as Button;
			menuButton = GetTemplateChild("buttonMenu") as Button;

			volumeSlider = GetTemplateChild("sliderVolume") as ScrubberBar;

			itemsButton = GetTemplateChild("buttonPlaylistItems") as Grid;
			chaptersButton = GetTemplateChild("buttonChapter") as Grid;

			itemListBox = GetTemplateChild("listBoxItems") as ListBox;
			itemsContainer = GetTemplateChild("borderItems") as Border;
			chapterListBox = GetTemplateChild("listBoxChapters") as ListBox;
			chaptersContainer = GetTemplateChild("borderChapters") as Border;

			debugBox = GetTemplateChild("debugBox") as Border;
			customToolTip = GetTemplateChild("customToolTip") as Border;
			messageBox = GetTemplateChild("messageBox") as Border;
			messageBoxText = GetTemplateChild("messageBoxText") as TextBlock;
			controlBox = GetTemplateChild("controlBox") as Grid;

			optionsMenu = GetTemplateChild("optionsMenu") as ListBox;
			closePlaylist = GetTemplateChild("closePlaylist") as Border;
			closeChapters = GetTemplateChild("closeChapters") as Border;
			closeLinkEmbed = GetTemplateChild("closeLinkEmbed") as Border;

			menuScaling = GetTemplateChild("menuScaling") as Grid;
			menuDebug = GetTemplateChild("menuDebug") as Grid;

			menuLogs = GetTemplateChild("menuLogs") as Grid;
			menuStats = GetTemplateChild("menuStats") as Grid;
			menuStretch = GetTemplateChild("menuStretch") as Grid;
			menuFit = GetTemplateChild("menuFit") as Grid;
			menuNative = GetTemplateChild("menuNative") as Grid;
			menuNativeSmaller = GetTemplateChild("menuNativeSmaller") as Grid;

			subMenuDebugBox = GetTemplateChild("subMenuDebugBox") as Border;
			subMenuScalingBox = GetTemplateChild("subMenuScalingBox") as Border;
			subMenuDebug = GetTemplateChild("subMenuDebug") as ListBox;
			subMenuScaling = GetTemplateChild("subMenuScaling") as ListBox;

			linkEmbedBox = GetTemplateChild("linkEmbedBox") as Border;
			buttonLinkEmbed = GetTemplateChild("buttonLinkEmbed") as Button;

			linkText = GetTemplateChild("linkText") as TextBox;
			embedText = GetTemplateChild("embedText") as TextBox;

			playSymbol = GetTemplateChild("playSymbol") as Path;
		}

		/// <summary>
		/// Overrides the controls OnApplyTemplate Method to capture and wire things up
		/// </summary>
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			UnhookHandlers();
			BindTemplate();
			HookHandlers();

			ApplyConfiguration();

			CheckMenuHighlights();
		}

		/// <summary>
		/// Wires up all the event handlers to the controls
		/// </summary>
		protected void HookHandlers() {
			if (mainTimer == null) {
				mainTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 0, 0, (6*1001/30))};
				mainTimer.Tick += OnTimerTick;
			}

			mainTimer.Start();

			if (Application.Current != null) {
				Application.Current.Host.Content.FullScreenChanged += OnFullScreenChanged;
				Application.Current.Host.Content.Resized += OnFullScreenChanged;
			}

			MouseMove += On_MouseMove;

			if (mainMediaElement != null) {
				mainMediaElement.MediaFailed += OnMediaElementMediaFailed;
				mainMediaElement.MediaOpened += OnMediaElementMediaOpened;
				mainMediaElement.MediaEnded += OnMediaElementMediaEnded;
				mainMediaElement.CurrentStateChanged += OnMediaElementCurrentStateChanged;
				mainMediaElement.MarkerReached += OnMediaElementMarkerReached;
				mainMediaElement.BufferingProgressChanged += OnMediaElementBufferingProgressChanged;
				mainMediaElement.DownloadProgressChanged += OnMediaElementDownloadProgressChanged;
				mainMediaElement.MouseLeftButtonDown += OnMediaElement_MouseLeftButtonDown;
			}

			if (playPauseButton != null) {
				playPauseButton.Click += OnButtonClickPlayPause;
			}

			if (stopButton != null) {
				stopButton.Click += OnButtonClickStop;
			}

			if (previousButton != null) {
				previousButton.Click += OnButtonClickPrevious;
			}

			if (nextButton != null) {
				nextButton.Click += OnButtonClickNext;
			}

			if (muteButton != null) {
				muteButton.Click += OnButtonClickMute;
			}

			if (menuDebug != null) menuDebug.MouseLeftButtonDown += OnMenuDebug_MouseLeftButtonDown;
			if (menuScaling != null) menuScaling.MouseLeftButtonDown += OnMenuScaling_MouseLeftButtonDown;

			if (menuLogs != null) menuLogs.MouseLeftButtonDown += OnMenuLogs_MouseLeftButtonDown;
			if (menuStats != null) menuStats.MouseLeftButtonDown += OnMenuStats_MouseLeftButtonDown;

			if (menuFit != null) menuFit.MouseLeftButtonDown += OnMenuFit_MouseLeftButtonDown;
			if (menuNative != null) menuNative.MouseLeftButtonDown += OnMenuNative_MouseLeftButtonDown;
			//if (menuNativeSmaller != null) menuNativeSmaller.MouseLeftButtonDown += OnMenuNativeSmaller_MouseLeftButtonDown;
			if (menuStretch != null) menuStretch.MouseLeftButtonDown += OnMenuStretch_MouseLeftButtonDown;
			
			if (menuButton != null) {
				menuButton.Click += OnButtonClickMenu;
			}

			if (fullscreenButton != null) {
				fullscreenButton.Click += OnButtonClickFullScreen;
			}

			if (itemsButton != null) {
				itemsButton.MouseLeftButtonUp += OnButtonClickPlaylistItems;
			}

			if (itemListBox != null) {
				itemListBox.SelectionChanged += OnItemListSelectionChanged;
			}

			if (closePlaylist != null) {
				closePlaylist.MouseLeftButtonUp += OnClosePlaylist_Click;
			}

			if (closeLinkEmbed != null) {
				closeLinkEmbed.MouseLeftButtonUp += OnCloseLinkEmbed_Click;
			}

			if (closeChapters != null) {
				closeChapters.MouseLeftButtonUp += OnCloseChapters_Click;
			}

			if (chapterListBox != null) {
				chapterListBox.SelectionChanged += OnChapterListSelectionChanged;
			}

			if (chaptersButton != null) {
				chaptersButton.MouseLeftButtonUp += OnButtonClickChapter;
			}

			if (scrubberBar != null) {
				scrubberBar.ValueChanged += OnScrubberChanged;
				scrubberBar.ValueChangeRequest += OnScrubberChangeRequest;
				scrubberBar.MouseOver += OnScrubberMouseOver;
				scrubberBar.MouseLeave += OnScrubberMouseLeave;
			}

			if (volumeSlider != null && mainMediaElement != null) {
				volumeSlider.Minimum = 0;
				volumeSlider.Maximum = 1;

				if (isMuted) {
					volumeSlider.Value = 0;
					mainMediaElement.Volume = 0;
				}
				else {
					volumeSlider.Value = START_VOLUME;
				}
				mainMediaElement.Volume = volumeSlider.Value;

				volumeSlider.ValueChanged += OnSliderVolumeChanged;
				volumeSlider.ValueChangeRequest += OnVolumeChangeRequest;
				volumeSlider.MouseOver += OnVolumeMouseOver;
				volumeSlider.MouseLeave += OnVolumeMouseLeave;
			}

			if (itemListBox != null) {
				itemListBox.ItemsSource = Playlist;
			}

			if(messageBox!=null) {
				messageBox.MouseLeftButtonDown += OnMediaElement_MouseLeftButtonDown;
			}

			if (mainBorder != null) {
			//	mainBorder.MouseLeftButtonDown += OnMediaElement_MouseLeftButtonDown;
			}

			if(buttonLinkEmbed!=null) {
				buttonLinkEmbed.Click += OnButtonLinkEmbed_Click;
			}

			if(linkText!=null) {
				//linkText.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(linkText_MouseLeftButtonDown);
				linkText.GotFocus += OnLinkText_GotFocus;
			}
			if(embedText!=null) {
				embedText.GotFocus += OnEmbedText_GotFocus;
			}
		}


		/// <summary>
		/// Unwires all event handlers
		/// </summary>
		private void UnhookHandlers() {

			MouseMove -= On_MouseMove;

			if (mainMediaElement != null) {
				mainMediaElement.MediaFailed -= OnMediaElementMediaFailed;
				mainMediaElement.MediaOpened -= OnMediaElementMediaOpened;
				mainMediaElement.MediaEnded -= OnMediaElementMediaEnded;
				mainMediaElement.CurrentStateChanged -= OnMediaElementCurrentStateChanged;
				mainMediaElement.MarkerReached -= OnMediaElementMarkerReached;
				mainMediaElement.BufferingProgressChanged -= OnMediaElementBufferingProgressChanged;
				mainMediaElement.DownloadProgressChanged -= OnMediaElementDownloadProgressChanged;
			}

			if (playPauseButton != null) {
				playPauseButton.Click -= OnButtonClickPlayPause;
			}

			if (stopButton != null) {
				stopButton.Click -= OnButtonClickStop;
			}

			if (previousButton != null) {
				previousButton.Click -= OnButtonClickPrevious;
			}

			if (nextButton != null) {
				nextButton.Click -= OnButtonClickNext;
			}

			if (muteButton != null) {
				muteButton.Click -= OnButtonClickMute;
			}

			if (menuButton != null) {
				menuButton.Click -= OnButtonClickMenu;
			}

			if (closeChapters != null) {
				closeChapters.MouseLeftButtonUp -= OnCloseChapters_Click;
			}

			if (fullscreenButton != null) {
				fullscreenButton.Click -= OnButtonClickFullScreen;
			}

			if (menuDebug != null) menuDebug.MouseLeftButtonDown -= OnMenuDebug_MouseLeftButtonDown;
			if (menuScaling != null) menuScaling.MouseLeftButtonDown -= OnMenuScaling_MouseLeftButtonDown;

			if (menuLogs != null) menuLogs.MouseLeftButtonDown -= OnMenuLogs_MouseLeftButtonDown;
			if (menuStats != null) menuStats.MouseLeftButtonDown -= OnMenuStats_MouseLeftButtonDown;

			if (menuFit != null) menuFit.MouseLeftButtonDown -= OnMenuFit_MouseLeftButtonDown;
			if (menuNative != null) menuNative.MouseLeftButtonDown -= OnMenuNative_MouseLeftButtonDown;
		//	if (menuNativeSmaller != null) menuNativeSmaller.MouseLeftButtonDown -= OnMenuNativeSmaller_MouseLeftButtonDown;
			if (menuStretch != null) menuStretch.MouseLeftButtonDown -= OnMenuStretch_MouseLeftButtonDown;

			if(closePlaylist!=null) {
				closePlaylist.MouseLeftButtonUp -= OnClosePlaylist_Click;
			}

			if (itemsButton != null) {
				itemsButton.MouseLeftButtonUp -= OnButtonClickPlaylistItems;
			}

			if (itemListBox != null) {
				itemListBox.SelectionChanged -= OnItemListSelectionChanged;
			}

			if (chapterListBox != null) {
				chapterListBox.SelectionChanged -= OnChapterListSelectionChanged;
			}

			if (chaptersButton != null) {
				chaptersButton.MouseLeftButtonUp -= OnButtonClickChapter;
			}

			if (scrubberBar != null) {
				scrubberBar.ValueChanged -= OnScrubberChanged;
				scrubberBar.ValueChangeRequest -= OnScrubberChangeRequest;
				scrubberBar.MouseOver -= OnScrubberMouseOver;
				scrubberBar.MouseLeave -= OnScrubberMouseLeave;
			}

			if (volumeSlider != null) {
				volumeSlider.ValueChanged -= OnSliderVolumeChanged;
				volumeSlider.ValueChangeRequest -= OnVolumeChangeRequest;
				volumeSlider.MouseOver -= OnVolumeMouseOver;
				volumeSlider.MouseLeave -= OnVolumeMouseLeave;
			}

			if (Application.Current != null) {
				Application.Current.Host.Content.FullScreenChanged -= OnFullScreenChanged;
				Application.Current.Host.Content.Resized -= OnFullScreenChanged;
			}

			if (mainTimer != null) {
				mainTimer.Stop();
			}

		}


		/// <summary>
		/// Applies the configuration of the properties to the template
		/// </summary>
		protected void ApplyConfiguration() {
			if (startupArgs != null) {
				//Import our initialization values via the init parser
				PlayerInitParameterParser playerInitParser = new PlayerInitParameterParser();
				playerInitParser.ImportInitParams(startupArgs, this);
			}

			//TODO: apply Markers from playlist.
			//TODO: ApplyConfiguration the markers to the video item

			if (itemsContainer != null) {
				itemsContainer.Visibility = Visibility.Collapsed;
			}

			if (chaptersContainer != null) {
				chaptersContainer.Visibility = Visibility.Collapsed;
			}

			if (string.IsNullOrEmpty(EmbedUrl) && string.IsNullOrEmpty(LinkUrl)) {//check embed 
				controlBox.ColumnDefinitions[7].Width = new GridLength(0);
			} else {
				if (linkText != null && LinkUrl!=null) linkText.Text = LinkUrl;
				if (embedText != null && EmbedUrl!=null) embedText.Text = EmbedUrl;
			}

			//Call the fullscreen support for if we're starting in fullscreen
			PerformResize();

			if (mainMediaElement != null) {
				mainMediaElement.AutoPlay = autoplaySetting;
				mainMediaElement.Stretch = stretchMode;

				StartAutoPlay();
			}

			UpdateDebugPanel();
		}

		#endregion

		#region Player Methods

		/// <summary>
		/// Moves to the given item in the playlist.  If the item does not exist it does nothing.
		/// </summary>
		/// <param name="playlistItemIndex">The item index to seek to</param>
		protected void SeekToPlaylistItem(int playlistItemIndex) {
			if (playlistItemIndex < 0 || playlistItemIndex >= Playlist.Count) {
				return;
			}

			//we have something to play.
			currentlyPlayingChapter = 0;
			currentlyPlayingItem = playlistItemIndex;

			if (chapterListBox != null) {
				chapterListBox.ItemsSource = Playlist[currentlyPlayingItem].Chapters;
			}

			if (Playlist[currentlyPlayingItem].DeliveryType == DeliveryTypes.Adaptive) {
				//hide the download progress bar
				DownloadProgressControlVisibility = Visibility.Collapsed;

				// Bring adaptive heuristics in..
				// NOTE: ADAPTIVE IS DISABLED IN THE PUBLIC BUILD, THE DLL MUST BE PROVIDED BY AKAMAI/MICROSOFT
				if (adaptiveConstructor == null) {
					if (!adaptiveInit) {
						adaptiveInit = true;
						LoadAdaptiveAssembly();
						return; //get out of here so WC can work..
					}
				} else {
					altMediaStreamSource = adaptiveConstructor.Invoke(new object[] {mainMediaElement, new Uri(Playlist[currentlyPlayingItem].Url)}) as MediaStreamSource;
				}

				if (altMediaStreamSource == null) {
					//TODO - do we crash, or just skip this content?  How do we inform user?
					throw new Exception("Unable to load adaptive DLL");
				}

				mainMediaElement.SetSource(altMediaStreamSource);

			} else {
				//Assign the source directly from the playlist url
				mainMediaElement.Source = new Uri(Playlist[currentlyPlayingItem].Url);

				// Set altMediaStreamSource to null, it is not used for non-adaptive streams
				altMediaStreamSource = null;

				//show the download progress bar if type is download
				//TODO: WE DON'T PROPERLY DETECT STREAMING VIA A METAFILE (ASX) YET
				DownloadProgressControlVisibility = (Playlist[currentlyPlayingItem].DeliveryType == DeliveryTypes.Progressive) ? Visibility.Visible : Visibility.Collapsed;
			}

			SetChapterButtonVisibility();

			if (isPlaying || AutoPlay) {
				Play();
			}
		}

		private void LoadAdaptiveAssembly() {
			WebClient wc = new WebClient();
			wc.OpenReadCompleted += OnAssemblyDownloaded;
			wc.OpenReadAsync(new Uri(HtmlPage.Document.DocumentUri, "ClientBin/AdaptiveStreaming.dll"));
		}

		void OnAssemblyDownloaded(object sender, OpenReadCompletedEventArgs e) {
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

			AssemblyPart assemblyPart = new AssemblyPart();
			Assembly asm = assemblyPart.Load(e.Result);
			if (asm == null) {
				throw new Exception("Invalid adaptive dll");
			}

			Type adapType = asm.GetType("Microsoft.Expression.Encoder.AdaptiveStreaming.AdaptiveStreamingSource");
			if (adapType == null || !adapType.IsSubclassOf(typeof(MediaStreamSource))) {
				throw new Exception("Invalid adaptive type");
			}

			adaptiveConstructor = adapType.GetConstructor(new[] { typeof(MediaElement), typeof(Uri) });
			if (adaptiveConstructor == null) {
				throw new Exception("Invalid adaptive constructor");
			}

			//go back where we left off..
			SeekToPlaylistItem(currentlyPlayingItem);
		}

		/// <summary>
		/// Sets the chapter button to a style appropriate for the presence/absence of
		/// chapters in the current playlist.
		/// </summary>
		private void SetChapterButtonVisibility() {
			chaptersButton.Opacity = Playlist[currentlyPlayingItem].Chapters.Count <= 0 ? .5 : 1;
		}

		/// <summary>
		/// Returns the relative chapter index given a postition on the timeline.
		/// </summary>
		/// <param name="position">The position on the timeline to reference</param>
		/// <returns>the relative chapter position</returns>
		protected int ChapterIndexFromPosition(TimeSpan position) {
			double seconds = position.TotalSeconds;

			int indexChapter = 0;
			while (indexChapter < Playlist[currentlyPlayingItem].Chapters.Count && Playlist[currentlyPlayingItem].Chapters[indexChapter].Position < seconds) {
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
			if (chapterIndex >= 0 && chapterIndex < Playlist[currentlyPlayingItem].Chapters.Count) {
				currentlyPlayingChapter = chapterIndex;
				chapterListBox.SelectedIndex = currentlyPlayingChapter;
				return true;
			}
			return false;
		}

		public object GetTemplateChildItem(String name) {
			return GetTemplateChild(name);
		}

		/// <summary>
		/// Called for fullscreen support to stretch the player out
		/// </summary>
		private void PerformResize() {
			if ((Application.Current != null)) {
				if (Application.Current.Host.Content.IsFullScreen) {
					HorizontalAlignment = HorizontalAlignment.Stretch;
					VerticalAlignment = VerticalAlignment.Stretch;

					//start timer to close controls
					ShowControls = false;
				}else {
					ShowControls = true;
				}
			}
		}

		private void SetCustomToolTip(Point pt, string text) {
			CustomToolTipText = text;

			customToolTip.SetValue(Canvas.LeftProperty, pt.X + 4);
			customToolTip.SetValue(Canvas.TopProperty, pt.Y - 30);

			if (customToolTip.Opacity < 1) customToolTip.Opacity = 1;
		}

		private void UpdateDebugPanel() {

			//throttle this to once a second - keep overhead down
			if (DateTime.Now - lastDebugUpdate > TimeSpan.FromSeconds(1)) {
				lastDebugUpdate = DateTime.Now;

				//check adaptive rate, right now just put in our rendered fps
				StringBuilder sb = new StringBuilder();
				string state = (mainMediaElement == null) ? "" : " (" + mainMediaElement.CurrentState + ")";
				sb.AppendFormat("OpenVideoPlayer v{0}{1}", version, state);

				if (mainMediaElement != null) {
					if (isPlaying) sb.AppendFormat("\n{0}/{1} FPS (Render/Drop), Resolution: {2}x{3}", mainMediaElement.RenderedFramesPerSecond, mainMediaElement.DroppedFramesPerSecond, mainMediaElement.NaturalVideoWidth, mainMediaElement.NaturalVideoHeight);

					if (mainMediaElement.DownloadProgress > 0 && mainMediaElement.DownloadProgress < 1) {
						sb.AppendFormat("\nDownload progress: {0}%", (int) (100*mainMediaElement.DownloadProgress));
					}
				}

				//HACK - we shouldn't use reflection if avoidable
				//Is there a better way to get these through?  No common referenced assemblies other than SL/.NET framework
				if (altMediaStreamSource != null) {
					Type source = altMediaStreamSource.GetType();

					if (methodBitrates == null) methodBitrates = source.GetMethod("Bitrates");
					if (methodBitrates != null) {
						ulong[] brs = (ulong[])methodBitrates.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null);
						string brst = "";
						foreach (ulong br in brs) brst += br / 1024 + ",";
						sb.AppendFormat("\nBitrates: {0} kbps", brst.Substring(0, brst.Length - 1));
					}

					if (propCurrentBitrate == null) propCurrentBitrate = source.GetProperty("CurrentBitrate");
					if (propCurrentBitrate != null) {
						Double bps = (Double)propCurrentBitrate.GetValue(altMediaStreamSource, BindingFlags.Default, null, null, null);
						sb.AppendFormat("\nCurrent Bitrate: {0} kbps", (int)(bps / 1024));
					}

					if (propCurrentBandwidth == null) propCurrentBandwidth = source.GetProperty("CurrentBandwidth");
					if (propCurrentBandwidth != null) {
						Double bps = (Double)propCurrentBandwidth.GetValue(altMediaStreamSource, BindingFlags.Default, null, null, null);
						sb.AppendFormat("\nAvailable bandwidth:  {0} kbps", (int)(bps / 1024));
					}

					if (methodBufferSize == null) methodBufferSize = source.GetMethod("BufferSize");
					if (methodBufferSize != null) {
						ulong bs = (ulong)methodBufferSize.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null);
						if (methodBufferTime == null) methodBufferTime = source.GetMethod("BufferTime");
						if (methodBufferTime != null) {
							TimeSpan ts = (TimeSpan)methodBufferTime.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null);
							sb.AppendFormat("\nCurrent Buffer:  {0} KB, {1} sec", bs / 1024 / 8, Math.Round(ts.TotalSeconds, 1));
						}
					}
				}

				BitRateText = sb.ToString();
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

			startupArgs = e;

			//OpenVideoPlayer Ready
		}

		void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			Debug.WriteLine("Exception: " + e.ExceptionObject);
		}

		public void OnButtonClickPlaylistItems(object sender, RoutedEventArgs e) {
			//PlayListOverlay = false;

			if (itemsContainer != null) {
				ShowPlaylist = !ShowPlaylist;
				//itemsContainer.Visibility = (itemsContainer.Visibility == Visibility.Collapsed) ?
				//    Visibility.Visible : Visibility.Collapsed;

				AdjustForPlaylist();
			}

			CollapseMenus();
			UnSelectMenus();
		}

		private void AdjustForPlaylist() {
			if (PlayListOverlay == false) {
				mainBorder.Margin = new Thickness(0, 0, ((itemsContainer.Visibility == Visibility.Collapsed) ? 0 : itemListBox.ActualWidth), 0);
				itemsContainer.Margin = new Thickness(0, 5, 3, 5);
			} else {
				itemsContainer.Margin = new Thickness(0, 3, 3, 24);
			}
		}

		public void OnButtonClickChapter(object sender, RoutedEventArgs e) {
			if (chaptersContainer != null && currentlyPlayingItem > 0 && Playlist.Count > currentlyPlayingItem && Playlist[currentlyPlayingItem].Chapters.Count > 0) {
				//chaptersContainer.Visibility = (chaptersContainer.Visibility == Visibility.Collapsed) ?
				//    Visibility.Visible : Visibility.Collapsed;
				ShowChapters = !ShowChapters;
			}

			CollapseMenus();
			UnSelectMenus();
		}

		public void OnTimerTick(object sender, EventArgs e) {
			try {
				if (mainMediaElement == null) {
					return;
				}
				timerIsUpdating = true;

				if (PlayListOverlay == false && itemsContainer.Visibility == Visibility.Visible && mainBorder.Margin.Right == 0) {
					mainBorder.Margin = new Thickness(0, 0, itemListBox.ActualWidth, 0);
				}

				itemsButton.Opacity = (Playlist.Count < 2) ? .5 : 1;

				TimeSpan pos = mainMediaElement.Position;
				scrubberBar.IsEnabled = mainMediaElement.NaturalDuration.TimeSpan != TimeSpan.Zero;
				PlaybackPosition = pos.TotalSeconds;

				//hack - sometimes gets into a wierd state.  need to l0ok more at this
				if(PlaybackPosition > scrubberBar.Maximum) {
					if(mainMediaElement.RenderedFramesPerSecond == 0) {
						if (currentlyPlayingItem < Playlist.Count - 1) {
							SeekToNextItem();
						} else {
							mainMediaElement.Position = TimeSpan.Zero;
							Pause();
						}
					}
				}
				PlaybackPositionText = string.Format("{0}:{1}:{2}", pos.Hours.ToString("00"), pos.Minutes.ToString("00"), pos.Seconds.ToString("00"));

				if (chapterListBox != null && currentlyPlayingChapter >= 0 && currentlyPlayingChapter < chapterListBox.Items.Count && chapterListBox.SelectedIndex != currentlyPlayingChapter) {
					// set the currently playing chapter on the list box without triggering our events
					chapterListBox.SelectionChanged -= OnChapterListSelectionChanged;
					chapterListBox.SelectedIndex = currentlyPlayingChapter;
					chapterListBox.SelectionChanged += OnChapterListSelectionChanged;

					// move that into view
					chapterListBox.ScrollIntoView(chapterListBox.Items[currentlyPlayingChapter]);
				}

				UpdateDebugPanel();

				if (updateDownloading) {
					updateDownloading = false;

					DownloadPercent = mainMediaElement.DownloadProgress*100;
					DownloadOffsetPercent = mainMediaElement.DownloadProgressOffset*100;
				}

				if (updateBuffering) {
					updateBuffering = false;

					BufferingControlVisibility = (mainMediaElement.BufferingProgress < 1) ? Visibility.Visible : Visibility.Collapsed;
					BufferingPercent = mainMediaElement.BufferingProgress*100;
				}

				//Catch single click for play pause
				if (waitOnClick && DateTime.Now - lastClick >= TimeSpan.FromMilliseconds(250)) {
					waitOnClick = false;
					TogglePlayPause();
				}

				timerIsUpdating = false;

			}catch(Exception ex) {
				Debug.WriteLine("Error in timer: " + ex);
			}
		}

		void OnClosePlaylist_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			itemsContainer.Visibility = Visibility.Collapsed;
			AdjustForPlaylist();
		}

		void OnCloseLinkEmbed_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			linkEmbedBox.Visibility = Visibility.Collapsed;
		}

		void OnCloseChapters_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			chaptersContainer.Visibility = Visibility.Collapsed;
		}

		public void OnMediaElementCurrentStateChanged(object sender, RoutedEventArgs e) {
			// If we're playing make the play element invisible and the pause element visible
			// otherwise invert.
			if (mainMediaElement.CurrentState == MediaElementState.Playing) {
				playToggle.Visibility = Visibility.Collapsed;
				pauseToggle.Visibility = Visibility.Visible;

			//	isPlaying = true;
				ToolTipService.SetToolTip(playPauseButton, "Pause");
			}else if (mainMediaElement.CurrentState == MediaElementState.Opening) {
			} else if (mainMediaElement.CurrentState == MediaElementState.Closed) {
			} else {
				playToggle.Visibility = Visibility.Visible;
				pauseToggle.Visibility = Visibility.Collapsed;

			//	isPlaying = false;
				ToolTipService.SetToolTip(playPauseButton, "Play");
			}

			Debug.WriteLine("State: " + mainMediaElement.CurrentState);
		}

		public void OnMediaElementMediaOpened(object sender, RoutedEventArgs e) {
			PerformResize();
			
			MediaElement mediaElement;
			if ((mediaElement = sender as MediaElement) == null) {
				return;
			}

			//Detect that we opened a streaming link (perhaps through an asx) and
			//hide the download progress bar
			DownloadProgressControlVisibility = (mediaElement.DownloadProgress == 1) ? Visibility.Collapsed : Visibility.Visible;

			if (mainMediaElement.NaturalDuration.HasTimeSpan && mainMediaElement.NaturalDuration.TimeSpan > TimeSpan.Zero) {
				TimeSpan dur = mediaElement.NaturalDuration.TimeSpan;
				PlaybackDuration = dur.TotalSeconds;
				PlaybackDurationText = string.Format("{0}:{1}:{2}", dur.Hours.ToString("00"), dur.Minutes.ToString("00"), dur.Seconds.ToString("00"));
			} else {
				PlaybackDurationText = "(Live)";
			}

			if (isPlaying) {
				Play();
			}

			itemListBox.SelectedIndex = currentlyPlayingItem;
			messageBox.Visibility = Visibility.Collapsed;

			Debug.WriteLine("Opened: " + currentlyPlayingItem + ", " + CurrentSource);
		}

		public void OnMediaElementMediaEnded(object sender, RoutedEventArgs e) {
			SeekToNextItem();
		}

		public void OnMediaElementMediaFailed(object sender, RoutedEventArgs e) {
			Debug.WriteLine("Content Failed! ");

			string error = (e as ExceptionRoutedEventArgs !=null) ? "Message: " + ((ExceptionRoutedEventArgs)e).ErrorException.Message : null;
			MessageText = string.Format("Error opening {0}\n{1}{2}", CurrentSource, (error ?? "(Unknown Error)"), (Playlist.Count>currentlyPlayingItem+1)?"\n\nTrying next playlist item...":"");
			playSymbol.Visibility = Visibility.Collapsed;
			messageBox.Visibility = Visibility.Visible;
			messageBoxText.FontSize = 12;
			messageBox.Opacity = .8;
			messageBoxText.Foreground = new SolidColorBrush(Colors.Red);
			messageBox.Height = messageBoxText.ActualHeight + 30;
			SeekToNextItem();
		}

		public void OnMediaElementBufferingProgressChanged(object sender, RoutedEventArgs e) {
			updateBuffering = true;
		}

		public void OnMediaElementDownloadProgressChanged(object sender, RoutedEventArgs e) {
			updateDownloading = true;
		}

		void On_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			if ((Application.Current != null) && Application.Current.Host.Content.IsFullScreen) {
				//Debug.WriteLine("Move: " + e.GetPosition(mainBorder) + ", " + mainBorder.ActualHeight);
				if (!ShowControls) {
					if (e.GetPosition(mainBorder).Y > mainBorder.ActualHeight - 25) {
						ShowControls = true;
					}
				} else {
					if (e.GetPosition(mainBorder).Y < mainBorder.ActualHeight - 100) {
						ShowControls = false;
					}
				}
			}
		}

		public void OnButtonClickStart(object sender, RoutedEventArgs e) {
			SeekToPlaylistItem(0);
			Play();
			CollapseMenus();
		}

		public void OnButtonClickPause(object sender, RoutedEventArgs e) {
			Pause();
			CollapseMenus();
		}

		public void OnButtonClickPlay(object sender, RoutedEventArgs e) {
			Play();
			CollapseMenus();
		}

		public void OnButtonClickPlayPause(object sender, RoutedEventArgs e) {
			TogglePlayPause();
			CollapseMenus();
		}

		public void OnButtonClickStop(object sender, RoutedEventArgs e) {
			Stop();
		}

		/// <summary>
		/// Event callback, wraps the Scriptable SeekToPreviousChapter() method
		/// </summary>
		/// <param name="sender">The object calling the event</param>
		/// <param name="e">Args</param>
		public void OnButtonClickPrevious(object sender, RoutedEventArgs e) {
			SeekToPreviousChapter();
		}

		/// <summary>
		/// Event callback, supports a Next button by seeking to the next chapter or the
		/// next Item if no further chapters are available.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnButtonClickNext(object sender, RoutedEventArgs e) {
			if (Playlist[currentlyPlayingItem].Chapters.Count > 1) {
				SeekToNextChapter();
			} else {
				SeekToNextItem();
			}
		}

		/// <summary>
		/// Event callback to update the mainMediaElement with the volume's current value
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnSliderVolumeChanged(object sender, RoutedEventArgs e) {
			mainMediaElement.Volume = volumeSlider.Value;
			isMuted = volumeSlider.Value < LOWER_VOLUME_THRESHOLD;
			optionsMenu.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Event callback, supports a Mute button by storing off the last used volume
		/// and setting the volume to 0 or vice-versa.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnButtonClickMute(object sender, RoutedEventArgs e) {
			Muted = !Muted;
			CollapseMenus();
		}

		public void OnButtonClickMenu(object sender, RoutedEventArgs e) {
			optionsMenu.Visibility = (optionsMenu.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			subMenuDebugBox.Visibility = subMenuScalingBox.Visibility = Visibility.Collapsed;
			UnSelectMenus();
		}

		/// <summary>
		/// Event callback, supports fullscreen toggling by wrapping the PerformResize method.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnFullScreenChanged(object sender, EventArgs e) {
			PerformResize();
		}

		/// <summary>
		/// Event callback, causes the player to go fullscreen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnButtonClickFullScreen(object sender, RoutedEventArgs e) {
			ToggleFullscreen();
		}

		/// <summary>
		/// Event callback, fires when the user changes the playlist item
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnItemListSelectionChanged(object sender, RoutedEventArgs e) {
			SeekToPlaylistItem(itemListBox.SelectedIndex);
			if (PlayListOverlay) {
				//itemsContainer.Visibility = Visibility.Collapsed; // hides the playlist
				if (isPlaying) itemsContainer.Visibility = Visibility.Collapsed; // hides the playlist
			
				AdjustForPlaylist();
			}
			//chaptersContainer.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Event callback, fires when the user changes the current chapter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnChapterListSelectionChanged(object sender, RoutedEventArgs e) {
			currentlyPlayingChapter = chapterListBox.SelectedIndex;
			if (currentlyPlayingChapter >= 0) {
				mainMediaElement.Position = TimeSpan.FromSeconds(Playlist[currentlyPlayingItem].Chapters[currentlyPlayingChapter].Position);
			}
		}

		/// <summary>
		/// Event callback, fires when the mainMediaElement hits a marker.  Markers may or may not be
		/// defined for each item in the playlist.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Param</param>
		public void OnMediaElementMarkerReached(object sender, TimelineMarkerRoutedEventArgs e) {
			// Marker types could trigger add points, captions, interactions or chapters
			switch (MarkerTypeConv.StringToMarkerType(e.Marker.Type)) {
				case MarkerTypes.Chapter:
					if (chapterListBox != null) {
						currentlyPlayingChapter = ChapterIndexFromPosition(e.Marker.Time);
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

		void OnMediaElement_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			//Debug.WriteLine("Click received from : " + sender.GetType());

			if (optionsMenu.Visibility == Visibility.Visible) {
				optionsMenu.Visibility = Visibility.Collapsed;
				return;
			}

			if (DateTime.Now - lastClick < TimeSpan.FromMilliseconds(10)) {
				//duplicate
				return;
			} else if (DateTime.Now - lastClick < TimeSpan.FromMilliseconds(250)) {
				//this means a double click..
				lastClick = DateTime.MinValue;
				waitOnClick = false;
				OnButtonClickFullScreen(this, new RoutedEventArgs());
			} else {
				//if there isnt another click, this will get picked up by our dispatcher timer to make a single click
				waitOnClick = true;
				lastClick = DateTime.Now;
			}
			
		}

		public void OnScrubberChanged(object sender, RoutedEventArgs e) {
			if (!timerIsUpdating) {
				mainMediaElement.Position = TimeSpan.FromSeconds(scrubberBar.Value);
			}
		}

		public void OnScrubberChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			if (!timerIsUpdating) {
				mainMediaElement.Position = TimeSpan.FromSeconds(e.Value);
			}
		}

		void OnVolumeChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			volumeSlider.Value = e.Value;
		}

		void OnVolumeMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			Point pt = e.MouseArgs.GetPosition(this);
			Double val = (e.Value > 1) ? 1 : e.Value;

			string text = string.Format("Volume: {0}%", (int)(val * 100));

			SetCustomToolTip(pt, text); 
			
			if (e.MousePressed) {
				volumeSlider.Value = val;
			}
		}

		void OnVolumeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			if (customToolTip.Opacity > 0) customToolTip.Opacity = 0;
		}

		void OnScrubberMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			if (customToolTip.Opacity > 0) customToolTip.Opacity = 0;
		}

		void OnScrubberMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			Point pt = e.MouseArgs.GetPosition(this);
			TimeSpan ts = TimeSpan.FromSeconds(e.Value);
			string text = "Seek to: " + string.Format("{0}:{1}:{2}", ts.Hours.ToString("00"), ts.Minutes.ToString("00"), ts.Seconds.ToString("00")); 

			SetCustomToolTip(pt, text);

			if (e.MousePressed && e.Value < scrubberBar.Maximum && e.Value>=0) {
			 	scrubberBar.Value = e.Value;
			}
		}

		void OnEmbedText_GotFocus(object sender, RoutedEventArgs e) {
			embedText.SelectAll();
		}

		void OnLinkText_GotFocus(object sender, RoutedEventArgs e) {
			linkText.SelectAll();
		}

		void OnButtonLinkEmbed_Click(object sender, RoutedEventArgs e) {
			linkEmbedBox.Visibility = (linkEmbedBox.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
		}

		#endregion

		#region Menu stuff - to be custom control in future
		void OnMenuScaling_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			subMenuScalingBox.Visibility = Visibility.Visible;
			subMenuDebugBox.Visibility = Visibility.Collapsed;

			UnSelectMenus();
		}

		void OnMenuDebug_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			subMenuDebugBox.Visibility = Visibility.Visible;
			subMenuScalingBox.Visibility = Visibility.Collapsed;
			UnSelectMenus();
		}

		void OnMenuStats_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			ToggleDebugPanel();

			UnSelectMenus();
			CollapseMenus();
			CheckMenuHighlights();
		}

		void OnMenuLogs_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			//ToggleLogPanel();

			UnSelectMenus();
			CollapseMenus();
			CheckMenuHighlights();
		}

		void OnMenuStretch_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			StretchMode = Stretch.Fill; 
			
			CheckMenuHighlights();
			UnSelectMenus();
			CollapseMenus();
		}

		void OnMenuNative_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			StretchMode = Stretch.None; 
			
			CheckMenuHighlights();
			UnSelectMenus();
			CollapseMenus();
		}

		void OnMenuFit_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			StretchMode = Stretch.Uniform;

			UnSelectMenus();
			CheckMenuHighlights();
			CollapseMenus();
		}

		SolidColorBrush sel = new SolidColorBrush(Color.FromArgb(0xFF, 0x37, 0x80, 0x94));//377994
		SolidColorBrush unsel = new SolidColorBrush(Colors.Transparent);
		
		private void CollapseMenus() {
			subMenuDebugBox.Visibility = optionsMenu.Visibility = subMenuScalingBox.Visibility = Visibility.Collapsed;
		}

		private void UnSelectMenus() {
			if (optionsMenu.SelectedItem is ListBoxItem) ((ListBoxItem)optionsMenu.SelectedItem).IsSelected = false;
			if (subMenuDebug.SelectedItem is ListBoxItem) ((ListBoxItem)subMenuDebug.SelectedItem).IsSelected = false;
			if (subMenuScaling.SelectedItem is ListBoxItem) ((ListBoxItem)subMenuScaling.SelectedItem).IsSelected = false;
		}

		private void CheckMenuHighlights() {
			if (menuNative != null) menuNative.Background = (StretchMode == Stretch.None) ? sel : unsel;
			if (menuFit != null) menuFit.Background = (StretchMode == Stretch.Uniform) ? sel : unsel;
			if (menuStretch != null) menuStretch.Background = (StretchMode == Stretch.Fill) ? sel : unsel;
			if (menuStats != null) menuStats.Background = (DebugVisibility == Visibility.Collapsed) ? unsel : sel;
			//menuNativeSmaller.Background = (menuNativeSmaller.Background == sel) ? unsel : sel;
		}

		#endregion

		#region IMediaControl (Scriptable) Members

		/// <summary>
		/// Causes the mainMediaElement to begin playing
		/// </summary>
		[ScriptableMember]
		public void Play() {
			isPlaying = true;

			if (mainMediaElement != null) {
				if(Playlist.Count > 0 && currentlyPlayingItem == -1) {
					SeekToPlaylistItem(0);
					return;
				}
				if (mainMediaElement.CurrentState == MediaElementState.Stopped) {
					mainMediaElement.Position = new TimeSpan(0);
				}
				mainMediaElement.Play();
				Debug.WriteLine("Play");
			}
			ToolTipService.SetToolTip(playPauseButton, "Pause");
			messageBox.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Pause's the mainMediaElement
		/// </summary>
		[ScriptableMember]
		public void Pause() {
			if (mainMediaElement != null) {
				if (mainMediaElement.CanPause) {
					mainMediaElement.Pause();
					Debug.WriteLine("Pause");
				} else {
					mainMediaElement.Stop();
					Debug.WriteLine("Stop");
				}
			}
			isPlaying = false;
			ToolTipService.SetToolTip(playPauseButton, "Play");
			SetPausedMessageBox();
		}

		private void SetPausedMessageBox() {
			messageBox.Visibility = Visibility.Visible;
			messageBox.Opacity = .5;
			MessageText = "";
			playSymbol.Visibility = Visibility.Visible;
			messageBoxText.FontSize = 18;
			messageBox.Height = messageBoxText.ActualHeight + 30;
			messageBoxText.Foreground = new SolidColorBrush(Colors.White);
		}

		public void StartAutoPlay() {
			if (Playlist.Count > 0) {
				if (AutoPlay) {
					SeekToPlaylistItem(0);
				}
				else {
					SetPausedMessageBox();
				}
			}
		}


		/// <summary>
		/// Toggles the Play or Pause state
		/// </summary>
		[ScriptableMember]
		public void TogglePlayPause() {
			if (mainMediaElement != null && mainMediaElement.CurrentState != MediaElementState.Playing) {
				Play();
			}else {
				Pause();	
			}
		}

		//TODO - dep property
		[ScriptableMember]
		public bool PlayListOverlay { get; set; }

		/// <summary>
		/// Stops the mainMediaElement
		/// </summary>
		[ScriptableMember]
		public void Stop() {
			isPlaying = false;
			if (mainMediaElement != null) {
				mainMediaElement.Stop();
				mainMediaElement.AutoPlay = false;
				mainMediaElement.Source = null;
			}
		}

		/// <summary>
		/// Attempts to seek to the next chapter or the first chapter of the next
		/// item in the playlist.
		/// </summary>
		[ScriptableMember]
		public void SeekToNextChapter() {
			if (!SeekToChapterPoint(currentlyPlayingChapter + 1)) {
				SeekToPlaylistItem(currentlyPlayingItem + 1);
			}
		}

		/// <summary>
		/// Attempts to seek to the previous chapter or the last chapter of the
		/// previous playlist item.
		/// </summary>
		[ScriptableMember]
		public void SeekToPreviousChapter() {
			if (!SeekToChapterPoint(currentlyPlayingChapter - 1)) {
				SeekToPlaylistItem(currentlyPlayingItem - 1);
				SeekToChapterPoint(Playlist[currentlyPlayingItem].Chapters.Count - 1);
			}
		}

		/// <summary>
		/// Attempts to seek to the next item in the playlist
		/// </summary>
		[ScriptableMember]
		public void SeekToNextItem() {
			SeekToPlaylistItem(currentlyPlayingItem + 1);
		}

		/// <summary>
		/// Attempts to seek to the previous item in the playlist
		/// </summary>
		[ScriptableMember]
		public void SeekToPreviousItem() {
			SeekToPlaylistItem(currentlyPlayingItem - 1);
		}

		/// <summary>
		/// Can change the volume up or down given a positive or negative number.
		/// </summary>
		/// <param name="incrementValue">Value to increment the volume. A negative number
		/// here causes a decrement</param>
		[ScriptableMember]
		public void VolumeIncrement(double incrementValue) {
			double currentVolume = mainMediaElement.Volume;
			currentVolume = Math.Min(1.0, Math.Max(0.0, currentVolume + incrementValue));
			mainMediaElement.Volume = currentVolume;
			volumeSlider.Value = currentVolume;
			lastUsedVolume = currentVolume;
			isMuted = volumeSlider.Value < LOWER_VOLUME_THRESHOLD;
		}

		///<summary>
		/// Shows or hides the debug panel
		/// </summary>
		[ScriptableMember]
		public void ToggleDebugPanel() {
			DebugVisibility = (DebugVisibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			CheckMenuHighlights();
		}

		[ScriptableMember]
		public void ToggleControls() { ShowControls = !ShowControls; }

		[ScriptableMember]
		public bool ShowControls { 
			get { return controlBox.Visibility == Visibility.Visible; }
			set {
				controlBox.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
				mainGrid.RowDefinitions[2].Height = (value) ? new GridLength(32) : new GridLength(0); ;
			}
		}

		[ScriptableMember]
		public string CurrentSource {
			get {
				string plsource = (Playlist != null && Playlist.Count > currentlyPlayingItem && Playlist[currentlyPlayingItem] != null) ? Playlist[currentlyPlayingItem].Url : null;
				string source = (mainMediaElement.Source == null) ? plsource : mainMediaElement.Source.ToString(); //TODO - add support for non-playlisted adapative sources?
				return source;
			}
		}

		[ScriptableMember]
		public string LinkUrl { get; set;}

		[ScriptableMember]
		public string EmbedUrl { get; set; }

		[ScriptableMember]
		public void ToggleFullscreen() {
			bool full = Application.Current.Host.Content.IsFullScreen;
			Application.Current.Host.Content.IsFullScreen = !full;
			if (fullscreenButton != null) {
				if (!full) {
					ToolTipService.SetToolTip(fullscreenButton, "Restore Screen");
				}
				else {
					ToolTipService.SetToolTip(fullscreenButton, "FullScreen");
				}
			}
			if(optionsMenu!=null) optionsMenu.Visibility = Visibility.Collapsed;
		}

		[ScriptableMember]
		public bool ShowChapters{
			get { return chaptersContainer.Visibility == Visibility.Visible; }
			set { chaptersContainer.Visibility = (value && chapterListBox.Items.Count > 0) ? Visibility.Visible : Visibility.Collapsed; }
		}

		[ScriptableMember]
		public bool ShowPlaylist {
			get { return itemsContainer.Visibility == Visibility.Visible; }
			set { itemsContainer.Visibility = (value && (Playlist.Count > 1)) ? Visibility.Visible : Visibility.Collapsed; }
		}
		#endregion
	}
}
