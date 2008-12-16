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
using org.OpenVideoPlayer.Controls.Visuals;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Util;
using System.Collections.Generic;
using System.Threading;

namespace org.OpenVideoPlayer.Controls {
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
			PlaybackDurationText = PlaybackPositionText = "00:00";

			BufferingControlVisibility = Visibility.Collapsed;
			LogVisibility = StatVisibility = Visibility.Collapsed;

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
				version = new Version(fn.Substring(iv + 8, fn.IndexOf(" ", iv + 1) - iv - 9));
			}

		}

		public event RoutedEventHandler TemplateBound;
		public event RoutedEventHandler ItemChanged;
		public event EventHandler BrowserSizeChanged;
		public event EventHandler AdaptiveBitrateChanged;
		//TODO - we need to carry through many of the media events and properties
		#endregion

		private Brush highlight = new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
		public Brush Highlight {
			get { return highlight; }
			set { highlight = value; }
		}

		#region Protected UI Element fields - bound to XAML Template
		protected Border mainBorder;
		protected Panel layoutRoot;
		protected Grid mainGrid;

		protected FrameworkElement playToggle;
		protected FrameworkElement pauseToggle;
		protected MediaElement mediaElement;

		protected Button buttonPlayPause;
		protected Button buttonPrevious;
		protected Button buttonNext;
		protected Button buttonStop;
		protected Button buttonLinkEmbed;

		private Menu menuOptions;

		protected Button buttonFullScreen;
		protected Button buttonMute;
		protected Button buttonMenu;

		protected ScrubberBar sliderVolume;
		protected ScrubberBar scrubberBar;
		protected ListBox listBoxPlaylist;
		protected Border borderPlaylist;
		protected ListBox listBoxChapters;
		protected Border borderChapters;
		protected Border closePlaylist;
		protected Border closeChapters;
		protected Border closeLinkEmbed;

		protected Panel controlBox;
		protected Border statBox;
		protected Border customToolTip;
		protected Border messageBox;
		protected TextBlock messageBoxText;

		protected Path playSymbol;

		protected TextBox embedText;
		protected TextBox linkText;

		protected Border linkEmbedBox;
		protected LogViewer logViewer;

		//TODO - make these configurable, define in xaml
		SolidColorBrush bad = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
		SolidColorBrush warn = new SolidColorBrush(Color.FromArgb(255, 255, 255, 140));
		SolidColorBrush std = new SolidColorBrush(Colors.White);
		SolidColorBrush good = new SolidColorBrush(Color.FromArgb(255, 175, 255, 175));
		protected QualityGauge qualityGauge;
		#endregion

		#region private instance variables

		#region Constants
		private const Double START_VOLUME = 0.5;
		private const Double LOWER_VOLUME_THRESHOLD = 0.01;
		public const string PLAYER_CONTROL_NAME = "OpenVideoPlayerControl";
		#endregion

		/// <summary>
		/// Stores any alternate media source objects to send directly to the
		/// video screen.
		/// </summary>
		private MediaStreamSource altMediaStreamSource;

		protected Version version;

		protected bool autoplaySetting;
		protected bool timerIsUpdating;
		protected bool fullscreenSetting;

		protected Stretch stretchMode;
		protected Color backgroundColor;

		protected DispatcherTimer mainTimer;
		protected Timer threadTimer;

		private int currentlyPlayingItem;
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

		//cache these for better performance, although getting away from reflection would be nice
		private ConstructorInfo adaptiveConstructor; 
		private MethodInfo methodBitrates;
		private MethodInfo methodAttributes;
		private PropertyInfo propCurrentBitrate;
		private PropertyInfo propCurrentBandwidth;
		private MethodInfo methodBufferSize;
		private MethodInfo methodBufferTime;
		private MethodInfo methodSetBitrateRange;

		private StartupEventArgs startupArgs;

		private bool adaptiveInit;

		private LogCollection logList = new LogCollection();
		private OutputLog log = new OutputLog("Player");

		private DateTime lastDebugUpdate;
		DateTime lastMouseMove;
		string lastSource = null;
		HorizontalAlignment lastHAlighn;
		VerticalAlignment lastVAlighn;
		double lastMediaHeight = 0;
		string bitrateString = "";
		protected MediaElementState lastMediaState = MediaElementState.Closed;
		protected string lastCommand;

		//stats stuff - go to struct?
		public long AdaptiveAvailableBandwidth { get; protected set; }
		public long AdaptiveCurrentBitrate {  get; protected set; }//	= -1.0;
		public long AdaptivePeakBitrate {  get; protected set; }//	= -1.0;
		public int AdaptiveSegmentCapIndex { get; protected set; }
		//ulong[] AdaptiveAvailableBitrates { public get; protected set; }

		public AdaptiveSegment[] AdaptiveSegments {  get; protected set; }

		//IList<IDictionary<MediaStreamAttributeKeys, string>> attributes = null;

		public ulong AdaptiveBufferSize {  get; protected set; }
		public TimeSpan AdaptiveBufferLength {  get; protected set; }
		public Double FPSRendered {  get; protected set; }
		public Double FPSDropped {  get; protected set; }
		public Double FPSTotal {  get; protected set; }

		public Size VideoResolution {  get; protected set; }
		public Size MediaElementSize {  get; protected set; }
		public Size BrowserSize {  get; protected set; }

		#endregion

		#region properties

		public bool AutoPlay {
			get { return autoplaySetting; }
			set { autoplaySetting = value; }
		}

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

		public Stretch StretchMode {
			get { return stretchMode; }
			set {
				stretchMode = value;
				if (mediaElement != null) mediaElement.Stretch = StretchMode;
				if (menuOptions != null) menuOptions.SetCheckState(StretchMode, true);
			}
		}

		/// <summary>
		/// Get's the object's playlist
		/// </summary>
		[System.ComponentModel.Category("Items")]
		public PlaylistCollection Playlist {
			get { return (PlaylistCollection)GetValue(PlaylistProperty); }
			set {
				SetValue(PlaylistProperty, value);
				listBoxPlaylist.ItemsSource = value;
			}
		}

		public Double PlaybackPosition {
			get { return (Double)GetValue(PlaybackPositionProperty); }
			set {
				SetValue(PlaybackPositionProperty, value);
				if (scrubberBar != null) scrubberBar.Value = value;
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

		public Visibility StatVisibility {
			get { return (Visibility)GetValue(StatVisibilityProperty); }
			set {
				if (StatVisibility != value) SetValue(StatVisibilityProperty, value);
			}
		}

		public Visibility LogVisibility {
			get { return (Visibility)GetValue(LogVisibilityProperty); }
			set {
				SetValue(LogVisibilityProperty, value);
			}
		}

		#endregion

		#region Dependency Property fields
		/**
         * We register dependency property's as a storage for many of the control objects properties.
         */

		public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(Brush), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Dependency Property storage for the playlist
		/// </summary>
		public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register("Playlist", typeof(PlaylistCollection), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Dependency Property stores the current playback position
		/// </summary>
		public static readonly DependencyProperty PlaybackPositionProperty = DependencyProperty.Register("PlaybackPosition", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Dependency Property stores the current playback position text value
		/// </summary>
		public static readonly DependencyProperty PlaybackPositionTextProperty = DependencyProperty.Register("PlaybackPositionText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		/// <summary>
		/// Depencency Property stores the total media duration
		/// </summary>
		public static readonly DependencyProperty PlaybackDurationProperty = DependencyProperty.Register("PlaybackDuration", typeof(Double), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

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
		public static readonly DependencyProperty StatVisibilityProperty =
			DependencyProperty.Register("StatVisibility", typeof(Visibility), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		public static readonly DependencyProperty LogVisibilityProperty =
	DependencyProperty.Register("LogVisibility", typeof(Visibility), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

		#endregion

		#region Template Methods


		/// <summary>
		/// Overrides the controls OnApplyTemplate Method to capture and wire things up
		/// </summary>
		public override void OnApplyTemplate() {
			try {
				base.OnApplyTemplate();

				lastSize = new Size(Width, Height);
				lastMargin = Margin;
				lastHAlighn = HorizontalAlignment;
				lastVAlighn = VerticalAlignment;

				//UnhookHandlers();
				BindTemplate(this, GetTemplateChild);
				if (menuOptions != null) {
					menuOptions.ApplyTemplate();
				}
				if (TemplateBound != null) TemplateBound(this, new RoutedEventArgs());

				ApplyConfiguration();

				HookHandlers();

			} catch (Exception ex) {
				// Debug.WriteLine("Failed to load theme : " + ", " + ex);
				log.Output(OutputType.Error, "Error in Apply Template", ex);
			}
		}

		/// <summary>
		/// Binds all the protected properties of the object into the template
		/// </summary>
		public static void BindTemplate(Control sender, org.OpenVideoPlayer.Util.ControlHelper.GetChildDlg dlg) {
			//use reflection to eliminate all that biolerplate binding code.
			//NOTE - field names must match element names in the xaml for binding to work!
			FieldInfo[] fields = sender.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

			foreach (FieldInfo fi in fields) {
				if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) && fi.GetValue(sender) == null) {
					//object o = sender.GetTemplateChild(fi.Name);
					object o = dlg(fi.Name);
					if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType))) {
						fi.SetValue(sender, o);
					} else {
						Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
					}
				}
			}
		}

		/// <summary>
		/// Wires up all the event handlers to the controls
		/// </summary>
		protected void HookHandlers() {
			OutputLog.StaticOutputEvent += OutputLog_StaticOutputEvent;

			if (mainTimer == null) {
				mainTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, (6 * 1001 / 30)) };
				mainTimer.Tick += OnTimerTick;
			}

			if (threadTimer == null) {
				threadTimer = new Timer(OnThreadTimerTick, threadTimer, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
			}

			mainTimer.Start();

			if (Application.Current != null) {
				Application.Current.Host.Content.FullScreenChanged += OnFullScreenChanged;
				Application.Current.Host.Content.Resized += OnFullScreenChanged;
			}

			MouseMove += On_MouseMove;
			MouseLeftButtonDown += On_MouseLeftButtonDown;
			SizeChanged += OnPlayerSizeChanged;

			if (mediaElement != null) {
				mediaElement.MediaFailed += OnMediaElementMediaFailed;
				mediaElement.MediaOpened += OnMediaElementMediaOpened;
				mediaElement.MediaEnded += OnMediaElementMediaEnded;
				mediaElement.CurrentStateChanged += OnMediaElementCurrentStateChanged;
				mediaElement.MarkerReached += OnMediaElementMarkerReached;
				mediaElement.BufferingProgressChanged += OnMediaElementBufferingProgressChanged;
				mediaElement.DownloadProgressChanged += OnMediaElementDownloadProgressChanged;
				mediaElement.MouseLeftButtonDown += OnMediaElement_MouseLeftButtonDown;
			}

			if (controlBox != null) {
				controlBox.SizeChanged += OnControlBoxSizeChanged;
				controlBox.MouseMove += new System.Windows.Input.MouseEventHandler(controlBox_MouseMove);
			}
			if (menuOptions != null) {
				menuOptions.ItemCheckedChanged += (OnMenuItemCheckedChanged);
				menuOptions.ItemClick += OnMenuItemClick;
			}

			if (qualityGauge != null) {
				qualityGauge.MouseMove += qualityGauge_MouseMove;
				qualityGauge.MouseLeave += qualityGauge_MouseLeave;
			}

			if (buttonPlayPause != null) {
				buttonPlayPause.Click += OnButtonClickPlayPause;
			}

			if (buttonStop != null) {
				buttonStop.Click += OnButtonClickStop;
			}

			if (buttonPrevious != null) {
				buttonPrevious.Click += OnButtonClickPrevious;
			}

			if (buttonNext != null) {
				buttonNext.Click += OnButtonClickNext;
			}

			if (buttonMute != null) {
				buttonMute.Click += OnButtonClickMute;
			}

			if (buttonMenu != null) {
				buttonMenu.Click += OnButtonClickMenu;
			}

			if (buttonFullScreen != null) {
				buttonFullScreen.Click += OnButtonClickFullScreen;
			}

			if (listBoxPlaylist != null) {
				listBoxPlaylist.SelectionChanged += OnItemListSelectionChanged;
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

			if (listBoxChapters != null) {
				listBoxChapters.SelectionChanged += OnChapterListSelectionChanged;
			}

			if (scrubberBar != null) {
				scrubberBar.ValueChanged += OnScrubberChanged;
				scrubberBar.ValueChangeRequest += OnScrubberChangeRequest;
				scrubberBar.MouseOver += OnScrubberMouseOver;
				scrubberBar.MouseLeave += OnScrubberMouseLeave;
			}

			if (sliderVolume != null && mediaElement != null) {
				sliderVolume.Minimum = 0;
				sliderVolume.Maximum = 1;

				if (isMuted) {
					sliderVolume.Value = 0;
					mediaElement.Volume = 0;
				} else {
					sliderVolume.Value = START_VOLUME;
				}
				mediaElement.Volume = sliderVolume.Value;

				sliderVolume.ValueChanged += OnSliderVolumeChanged;
				sliderVolume.ValueChangeRequest += OnVolumeChangeRequest;
				sliderVolume.MouseOver += OnVolumeMouseOver;
				sliderVolume.MouseLeave += OnVolumeMouseLeave;
			}

			if (listBoxPlaylist != null) {
				listBoxPlaylist.ItemsSource = Playlist;
			}

			if (messageBox != null) {
				messageBox.MouseLeftButtonDown += OnMediaElement_MouseLeftButtonDown;
			}

			if (buttonLinkEmbed != null) {
				buttonLinkEmbed.Click += OnButtonLinkEmbed_Click;
			}

			if (Playlist != null) {
				Playlist.CollectionChanged += Playlist_CollectionChanged;
				Playlist.LoadComplete += Playlist_LoadComplete;
			}

			if (linkText != null) {
				//linkText.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(linkText_MouseLeftButtonDown);
				linkText.GotFocus += OnLinkText_GotFocus;
			}
			if (embedText != null) {
				embedText.GotFocus += OnEmbedText_GotFocus;
			}
		}


		void controlBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			lastMouseMove = DateTime.Now;
		}

		void OnPlayerSizeChanged(object sender, SizeChangedEventArgs e) {
			//OnControlBoxSizeChanged(sender, e);

		}


		void Playlist_LoadComplete(object sender, RoutedEventArgs e) {
			StartAutoPlay();
		}

		void Playlist_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
				//StartAutoPlay();
				//log.Output(OutputType.Debug, "Time to autoplay");
			}
		}

		void OnControlBoxSizeChanged(object sender, SizeChangedEventArgs e) {
			Double sum = 0;
			foreach (FrameworkElement fe in controlBox.Children) {
				if (fe != null && fe.Name != "scrubberBar" && fe.Visibility == Visibility.Visible) sum += fe.ActualWidth + fe.Margin.Left + fe.Margin.Right;
			}
			if (scrubberBar != null) scrubberBar.Width = ActualWidth - sum - scrubberBar.Margin.Right - scrubberBar.Margin.Right - 4;

		}


		void On_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			CollapseMenus();
		}

		void OnMenuItemCheckedChanged(object sender, RoutedEventArgs e) {
			MenuItem m = sender as MenuItem;
			if (m == null) return;
			MenuItem t = (m.Menu.Target != null) ? m.Menu.Target as MenuItem : null;
			Debug.WriteLine("Item changed: " + m.Text + ", " + m.Checked);

			if (m.Checked) {
				if (t != null && t.Text == "Scaling") {
					StretchMode = (Stretch)Enum.Parse(typeof(Stretch), m.Tag.ToString(), true);
				}
			}

			if (m.Text == "Statistics") StatVisibility = (m.Checked) ? Visibility.Visible : Visibility.Collapsed;
			if (m.Text == "Logs") LogVisibility = (m.Checked) ? Visibility.Visible : Visibility.Collapsed;
		}

		void OnMenuItemClick(object sender, RoutedEventArgs e) {
			MenuItem m = sender as MenuItem;
			if (m == null) return;

			if (m.Text == "Playlist") TogglePlaylist();
			if (m.Text == "Chapters") ToggleChapters();
		}

		/// <summary>
		/// Applies the configuration of the properties to the template
		/// </summary>
		protected void ApplyConfiguration() {
			if (logViewer != null) {
				logViewer.ItemsSource = logList;
			}

			if (startupArgs != null) {
				//Import our initialization values via the init parser
				PlayerInitParameterParser playerInitParser = new PlayerInitParameterParser();
				playerInitParser.ImportInitParams(startupArgs, this);
			}

			//TODO: apply Markers from playlist.
			//TODO: ApplyConfiguration the markers to the video item

			if (borderPlaylist != null) {
				borderPlaylist.Visibility = Visibility.Collapsed;
			}

			if (borderChapters != null) {
				borderChapters.Visibility = Visibility.Collapsed;
			}

			if (string.IsNullOrEmpty(EmbedUrl) && string.IsNullOrEmpty(LinkUrl)) {
				//check embed 
				//controlBox.ColumnDefinitions[7].Width = new GridLength(0);
				buttonLinkEmbed.Visibility = Visibility.Collapsed;
			} else {
				if (linkText != null && LinkUrl != null) linkText.Text = LinkUrl;
				if (embedText != null && EmbedUrl != null) embedText.Text = EmbedUrl;
				buttonLinkEmbed.Visibility = Visibility.Visible;
			}

			//Call the fullscreen support for if we're starting in fullscreen
			PerformResize();

			if (mediaElement != null) {
				mediaElement.AutoPlay = autoplaySetting;
				mediaElement.Stretch = stretchMode;

				StartAutoPlay();
			}

			if (menuOptions != null) {
				menuOptions.SetCheckState((object)StretchMode.ToString(), true);
				menuOptions.SetCheckState("Statistics", StatVisibility == Visibility.Visible);
				menuOptions.SetCheckState("Logs", LogVisibility == Visibility.Visible);
			}

			UpdateDebugPanel();
		}

		#endregion

		#region Player Methods

		/// <summary>
		/// Moves to the given item in the playlist.  If the item does not exist it does nothing.
		/// </summary>
		/// <param name="playlistItemIndex">The item index to seek to</param>
		public void SeekToPlaylistItem(int playlistItemIndex) {
			if (playlistItemIndex < 0) return;
			//if (playlistItemIndex < 0 || playlistItemIndex >= Playlist.Count) {
			//    if (ItemChanged != null) ItemChanged(this, new RoutedEventArgs());
			//    return;
			//}

			//we have something to play.
			currentlyPlayingChapter = 0;
			currentlyPlayingItem = playlistItemIndex;
			if (ItemChanged != null) ItemChanged(this, new RoutedEventArgs());

			if (playlistItemIndex < 0 || playlistItemIndex >= Playlist.Count) {
				return;
			}

			if (listBoxChapters != null) {
				listBoxChapters.ItemsSource = Playlist[currentlyPlayingItem].Chapters;
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
					altMediaStreamSource = adaptiveConstructor.Invoke(new object[] { mediaElement, new Uri(Playlist[currentlyPlayingItem].Url) }) as MediaStreamSource;
				}

				if (altMediaStreamSource == null) {
					//TODO - do we crash, or just skip this content?  How do we inform user?
					throw new Exception("Unable to load adaptive DLL");
				}

				mediaElement.SetSource(altMediaStreamSource);

			} else {
				//Assign the source directly from the playlist url
				mediaElement.Source = new Uri(Playlist[currentlyPlayingItem].Url);

				// Set altMediaStreamSource to null, it is not used for non-adaptive streams
				altMediaStreamSource = null;

				//show the download progress bar if type is download
				//TODO: WE DON'T PROPERLY DETECT STREAMING VIA A METAFILE (ASX) YET
				DownloadProgressControlVisibility = (Playlist[currentlyPlayingItem].DeliveryType == DeliveryTypes.Progressive) ? Visibility.Visible : Visibility.Collapsed;
			}
			lastSource = Playlist[currentlyPlayingItem].Url;

			//SetChapterButtonVisibility();
			if (menuOptions != null) menuOptions.SetEnabled("Chapters", Playlist[currentlyPlayingItem].Chapters.Count > 0);

			if (isPlaying || AutoPlay) {
				Play();
			}
		}

		private void LoadAdaptiveAssembly() {
			WebClient wc = new WebClient();
			wc.OpenReadCompleted += OnAssemblyDownloaded;
			wc.OpenReadAsync(new Uri(HtmlPage.Document.DocumentUri, "AdaptiveStreaming.dll"));
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
				listBoxChapters.SelectedIndex = currentlyPlayingChapter;
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
					mainGrid.RowDefinitions[1].Height = mainGrid.RowDefinitions[2].Height = new GridLength(0);
				} else {
					ShowControls = true;
					ToolTipService.SetToolTip(buttonFullScreen, "FullScreen");
					Width = lastSize.Width;
					Height = lastSize.Height;
					Margin = lastMargin;
					HorizontalAlignment = lastHAlighn;
					VerticalAlignment = lastVAlighn;
					mainGrid.RowDefinitions[1].Height = mainGrid.RowDefinitions[2].Height = new GridLength(32);
					//OnControlBoxSizeChanged(this, null);
				}
			}
		}

		private void SetCustomToolTip(Point pt, string text) {
			CustomToolTipText = text;
			customToolTip.Visibility = Visibility.Visible;

			customToolTip.SetValue(Canvas.LeftProperty, pt.X - (customToolTip.ActualWidth));//+ 4);
			customToolTip.SetValue(Canvas.TopProperty, pt.Y - customToolTip.ActualHeight - 4);

			if (customToolTip.Opacity < 1) customToolTip.Opacity = 1;
		}

		//TODO -seperate an adaptive adapter/interface?
		private ulong AdaptiveGetBufferSize() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return 0;

			if (methodBufferSize == null) methodBufferSize = source.GetMethod("BufferSize");
			if (methodBufferSize != null) {
				return (ulong)methodBufferSize.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null);
				
			}
			return 0;// AdaptiveBufferLength;
		}

		private TimeSpan AdaptiveGetBufferLength() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return TimeSpan.Zero;

			if (methodBufferTime == null) methodBufferTime = source.GetMethod("BufferTime");
			if (methodBufferTime != null) {
				return (TimeSpan) methodBufferTime.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] {MediaStreamType.Video}, null);
			}
			return TimeSpan.Zero;
		}

		private double AdaptiveGetAvailableBandwidth() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return -1;

			if (propCurrentBandwidth == null) propCurrentBandwidth = source.GetProperty("CurrentBandwidth");
			if (propCurrentBandwidth != null) {
				return (Double)propCurrentBandwidth.GetValue(altMediaStreamSource, BindingFlags.Default, null, null, null);
			}
			return -1.0;
		}

		private IList<IDictionary<MediaStreamAttributeKeys, string>> AdaptiveGetAttributes() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return null;

			if (methodAttributes == null) methodAttributes = source.GetMethod("StreamAttributes");
			if (methodAttributes != null) {
				return methodAttributes.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null) as IList<IDictionary<MediaStreamAttributeKeys, string>>;
			}
			return null;
		}

		private ulong[] AdaptiveGetAvailableBitrates() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return null;

			if (methodBitrates == null) methodBitrates = source.GetMethod("Bitrates");
			if (methodBitrates != null) {
				return (ulong[])methodBitrates.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video }, null);
			}
			return null;
		}

		private double AdaptiveGetCurrentBitrate() {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return -1.0;

			if (propCurrentBitrate == null) propCurrentBitrate = source.GetProperty("CurrentBitrate");
			if (propCurrentBitrate != null) {
				return (Double)propCurrentBitrate.GetValue(altMediaStreamSource, BindingFlags.Default, null, null, null);
			}
			return -1.0;
		}


		private void AdaptiveSetMaxBitrate(long max) {
			Type source = (altMediaStreamSource != null) ? altMediaStreamSource.GetType() : null;
			if (source == null) return;
			if (methodSetBitrateRange == null) methodSetBitrateRange = source.GetMethod("SetBitrateRange");
			if (methodSetBitrateRange != null) {
				methodSetBitrateRange.Invoke(altMediaStreamSource, BindingFlags.Default, null, new object[] { MediaStreamType.Video, 0, max }, null);
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

			//OpenVideoPlayer Loaded
		}

		void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			//Debug.WriteLine("Exception: " + e.ExceptionObject);
			log.Output(OutputType.Error, "Error: ", e.ExceptionObject);
			e.Handled = true;
		}

		private void AdjustForPlaylist() {
			if (PlayListOverlay == false) {
				mainBorder.Margin = new Thickness(0, 0, ((borderPlaylist.Visibility == Visibility.Collapsed) ? 0 : listBoxPlaylist.ActualWidth), 0);
				borderPlaylist.Margin = new Thickness(0, 5, 3, 5);
			} else {
				borderPlaylist.Margin = new Thickness(0, 3, 3, 24);
			}
		}

		private readonly object timerLock = new object();
		public void OnThreadTimerTick(object sender) {
			if (!Monitor.TryEnter(timerLock)) return;
			try {

				UpdateStatistics();

			} catch (Exception ex) {
				Debug.WriteLine("Error in timer: " + ex);

				//TODO - make this thread-safe!!
				//log.Output(OutputType.Error, "Timer Error: ", ex);
			} finally {
				Monitor.Exit(timerLock);
			}
		}

		private void UpdateStatistics() {
			//TODO - we shouldn't use reflection if avoidable - Measure performance!
			//Is there a better way to get these through?  No common referenced assemblies other than SL/.NET framework
			if (altMediaStreamSource != null) {

				long current = (long)AdaptiveGetCurrentBitrate();
				if (AdaptiveCurrentBitrate != current) {
					AdaptiveCurrentBitrate = current;
					AdaptiveBitrateChanged(this, new EventArgs());
				}

				if (AdaptiveCurrentBitrate > AdaptivePeakBitrate) AdaptivePeakBitrate = AdaptiveCurrentBitrate;

				for (int x = 0; x < AdaptiveSegments.Length; x++) {
					AdaptiveSegment seg = AdaptiveSegments[x];
					if (seg.Bitrate == AdaptiveCurrentBitrate) {
						VideoResolution = seg.Resolution;
						break;
					}
				}

				AdaptiveAvailableBandwidth = (long)AdaptiveGetAvailableBandwidth();

				AdaptiveBufferLength = AdaptiveGetBufferLength();
				AdaptiveBufferSize = AdaptiveGetBufferSize();

				if (lastMediaHeight != MediaElementSize.Height) {
					AdaptiveFigureMaxResolution();
				}
			}
		}

		private void UpdateUIStatistics() {
			FPSRendered = mediaElement.RenderedFramesPerSecond;
			FPSDropped = mediaElement.DroppedFramesPerSecond;
			FPSTotal = FPSDropped + FPSRendered;
			MediaElementSize = new Size(mediaElement.ActualWidth, mediaElement.ActualHeight);
		}

		private void UpdateDebugPanel() {
			if (DateTime.Now - lastDebugUpdate > TimeSpan.FromSeconds(3)) {
				lastDebugUpdate = DateTime.Now;
				try {

					//check adaptive rate, right now just put in our rendered fps
					StringBuilder sb = new StringBuilder();
					string state = (mediaElement == null) ? "" : " (" + mediaElement.CurrentState + ")";
					sb.AppendFormat("OpenVideoPlayer v{0}{1}", version, state);

					if (isPlaying) sb.AppendFormat("\n{0}/{1} FPS (Drop/Total), Res: {2}", FPSDropped, FPSTotal, StringTools.SizetoString(VideoResolution));

					if (mediaElement.DownloadProgress > 0 && mediaElement.DownloadProgress < 1) {
						sb.AppendFormat("\nDownload progress: {0}%", (int)(100 * mediaElement.DownloadProgress));
					}
					if (AdaptiveCurrentBitrate > -1) sb.AppendFormat("\nCurrent Bitrate: {0}", StringTools.FriendlyBitsPerSec((int)AdaptiveCurrentBitrate));
					if (!string.IsNullOrEmpty(bitrateString)) sb.AppendFormat("\n{0} kbps", bitrateString);
					if (AdaptiveAvailableBandwidth > -1) sb.AppendFormat("\nAvailable bandwidth:  {0}", StringTools.FriendlyBitsPerSec((int)AdaptiveAvailableBandwidth));
					if (AdaptiveBufferLength >= TimeSpan.Zero) sb.AppendFormat("\nCurrent Buffer: {0}, {1} sec", StringTools.FriendlyBytes((long)AdaptiveBufferSize), Math.Round(AdaptiveBufferLength.TotalSeconds, 1));

					if (qualityGauge != null && AdaptiveSegments != null) {
						double max = AdaptiveSegments[AdaptiveSegments.Length - 1].Bitrate; // -bitrates[0];
						double capbr = AdaptiveSegments[AdaptiveSegmentCapIndex].Bitrate;
						Size capRes = AdaptiveSegments[AdaptiveSegmentCapIndex].Resolution;
						if (max > 0) qualityGauge.Value = (double)AdaptiveCurrentBitrate / ((capbr > 0 && capbr < max) ? capbr : max);
						int dropPerc = (FPSTotal > 0) ? (int)Math.Round(100 * (FPSDropped / FPSTotal), 0) : 0;
						int qualPercent = (int)Math.Round(qualityGauge.Value * 100, 0);

						string maxStr = (capbr > 0 && capbr < max) ? string.Format("Capped at {0} ({1})\n", StringTools.SizetoString(capRes), StringTools.FriendlyBitsPerSec((int)capbr)) : "";

						string tag = string.Format("{0} :: {1}x{2}\n{8}Bitrate: {3} ({4}% of max)\nAvailable bandwidth: {9}\nFramedrop: {5}% ({6}/{7} FPS)",
												   ((VideoResolution.Height >= 480) ? "High-Definition".ToUpper() : "Standard-Definition".ToUpper()),
												   VideoResolution.Width, VideoResolution.Height,
												   StringTools.FriendlyBitsPerSec((int)AdaptiveCurrentBitrate),
												   qualPercent, dropPerc, FPSDropped, FPSTotal, maxStr, StringTools.FriendlyBitsPerSec((int)AdaptiveAvailableBandwidth));

						qualityGauge.Foreground = ((VideoResolution.Height < 300 && capRes.Height > 300) || dropPerc >= 25) ? bad : (dropPerc >= 14) ? warn : (VideoResolution.Height >= 480 || VideoResolution.Height >= capRes.Height) ? good : std;
						qualityGauge.Tag = tag;

						//update tooltip in action - hack, shouldn't have to look at content, maybe use tag?
						if (customToolTip.Visibility == Visibility.Visible && CustomToolTipText != null && CustomToolTipText.ToLower().Contains("definition")) {
							CustomToolTipText = tag;
						}
					}

					BitRateText = sb.ToString();
				} catch (Exception ex) {
					log.Output(OutputType.Debug, "Update error", ex);
				}
			}
		}

		public void OnTimerTick(object sender, EventArgs e) {
			try {
				if (mediaElement == null) return;
				timerIsUpdating = true;

				//check controlbox width (event isnt reliable)
				//HACK - shouldnt be hardcoded value
				if (controlBox.Visibility== Visibility.Visible && controlBox.ActualWidth != ActualWidth - 4) OnControlBoxSizeChanged(null, null);

				//adjust margin when playlist shown
				if (mainBorder.Margin.Right != listBoxPlaylist.ActualWidth && PlayListOverlay == false && borderPlaylist.Visibility == Visibility.Visible && mainBorder.Margin.Right == 0) {
					mainBorder.Margin = new Thickness(0, 0, listBoxPlaylist.ActualWidth, 0);
				}

				//set scrubber position
				TimeSpan pos = mediaElement.Position;
				scrubberBar.IsEnabled = mediaElement.NaturalDuration.TimeSpan != TimeSpan.Zero;
				PlaybackPosition = pos.TotalSeconds;

				//hack - sometimes gets into a wierd state.  need to look more at this
				if(PlaybackPosition > scrubberBar.Maximum) {
					if(mediaElement.RenderedFramesPerSecond == 0) {
						if (currentlyPlayingItem < Playlist.Count - 1) {
							SeekToNextItem();
						} else {
							mediaElement.Position = TimeSpan.Zero;
							Pause();
						}
					}
				}
				TimeSpan dur = mediaElement.NaturalDuration.TimeSpan;

				//set position text
				PlaybackPositionText = (dur.Hours >= 1) ? string.Format("{0}:{1}:{2}", pos.Hours.ToString("00"), pos.Minutes.ToString("00"), pos.Seconds.ToString("00"))
					: string.Format("{0}:{1}", pos.Minutes.ToString("00"), pos.Seconds.ToString("00"));

				//make sure playlist is synced
				if (listBoxChapters != null && currentlyPlayingChapter >= 0 && currentlyPlayingChapter < listBoxChapters.Items.Count && listBoxChapters.SelectedIndex != currentlyPlayingChapter) {
					// set the currently playing chapter on the list box without triggering our events
					listBoxChapters.SelectionChanged -= OnChapterListSelectionChanged;
					listBoxChapters.SelectedIndex = currentlyPlayingChapter;
					listBoxChapters.SelectionChanged += OnChapterListSelectionChanged;

					// move that into view
					listBoxChapters.ScrollIntoView(listBoxChapters.Items[currentlyPlayingChapter]);
				}

				UpdateUIStatistics();
				UpdateDebugPanel();

				//update download stuff
				if (updateDownloading) {
					updateDownloading = false;

					DownloadPercent = mediaElement.DownloadProgress*100;
					DownloadOffsetPercent = mediaElement.DownloadProgressOffset*100;
				}

				//update buffering
				if (updateBuffering) {
					updateBuffering = false;

					BufferingControlVisibility = (mediaElement.BufferingProgress < 1) ? Visibility.Visible : Visibility.Collapsed;
					BufferingPercent = mediaElement.BufferingProgress*100;
				}

				//Catch single clicks for play pause
				if (waitOnClick && DateTime.Now - lastClick >= TimeSpan.FromMilliseconds(250)) {
					waitOnClick = false;
					TogglePlayPause();
				}

				//hide controls if fullscreeen and mouse stops moving
				if (ShowControls && Application.Current.Host.Content.IsFullScreen && DateTime.Now - lastMouseMove > TimeSpan.FromSeconds(5)) {
					ShowControls = false;
				}

				//check browser size, in case our parent wants to resize us dynamically
				Size newBrowserSize = new Size(BrowserScreenInfo.ClientWidth, BrowserScreenInfo.ClientHeight);
				if (newBrowserSize != BrowserSize && BrowserSizeChanged!=null) {
					BrowserSize = newBrowserSize;
					BrowserSizeChanged(this, new EventArgs());
				}

				timerIsUpdating = false;

			}catch(Exception ex) {
				log.Output(OutputType.Error, "UI Timer Error: ", ex);
			}
		}

		void OnClosePlaylist_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			borderPlaylist.Visibility = Visibility.Collapsed;
			AdjustForPlaylist();
		}

		void OnCloseLinkEmbed_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			linkEmbedBox.Visibility = Visibility.Collapsed;
		}

		void OnCloseChapters_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			borderChapters.Visibility = Visibility.Collapsed;
		}

		public void OnMediaElementCurrentStateChanged(object sender, RoutedEventArgs e) {
			if (mediaElement.CurrentState == lastMediaState) return;
			lastMediaState = mediaElement.CurrentState;

			// If we're playing make the play element invisible and the pause element visible, otherwise invert.
			if (mediaElement.CurrentState == MediaElementState.Playing) {
				playToggle.Visibility = Visibility.Collapsed;
				pauseToggle.Visibility = Visibility.Visible;

				//	isPlaying = true;
				ToolTipService.SetToolTip(buttonPlayPause, "Pause");
			} else if (mediaElement.CurrentState == MediaElementState.Opening) {
			} else if (mediaElement.CurrentState == MediaElementState.Closed) {
			} else {
				playToggle.Visibility = Visibility.Visible;
				pauseToggle.Visibility = Visibility.Collapsed;

				//	isPlaying = false;
				ToolTipService.SetToolTip(buttonPlayPause, "Play");
			}

			//update button visibility
			if (buttonNext != null && buttonPrevious != null) {
				buttonNext.Visibility = buttonPrevious.Visibility = (listBoxPlaylist.Items.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
			}

			//Debug.WriteLine("State: " + mediaElement.CurrentState);
			log.Output(OutputType.Debug, "State: " + mediaElement.CurrentState);
		}

		public void OnMediaElementMediaOpened(object sender, RoutedEventArgs e) {
			PerformResize();

			//Detect that we opened a streaming link (perhaps through an asx) and hide the download progress bar
			DownloadProgressControlVisibility = (mediaElement.DownloadProgress == 1) ? Visibility.Collapsed : Visibility.Visible;

			if (mediaElement.NaturalDuration.HasTimeSpan && mediaElement.NaturalDuration.TimeSpan > TimeSpan.Zero) {
				TimeSpan dur = mediaElement.NaturalDuration.TimeSpan;
				PlaybackDuration = dur.TotalSeconds;

				PlaybackDurationText = (dur.Hours >= 1) ? string.Format("{0}:{1}:{2}", dur.Hours.ToString("00"), dur.Minutes.ToString("00"), dur.Seconds.ToString("00"))
					: string.Format("{0}:{1}", dur.Minutes.ToString("00"), dur.Seconds.ToString("00"));

				//controlBox.ColumnDefinitions[1].Width = controlBox.ColumnDefinitions[3].Width = new GridLength((dur.Hours >= 1) ? 60 : 40);
			} else {
				PlaybackDurationText = "(Live)";
			}

			if (isPlaying) {
				Play();
			}

			listBoxPlaylist.SelectedIndex = currentlyPlayingItem;
			messageBox.Visibility = Visibility.Collapsed;

			if (buttonNext != null) {
				if (listBoxPlaylist.SelectedIndex < listBoxPlaylist.Items.Count - 1) {
					buttonNext.IsEnabled = true;
					buttonNext.Opacity = 1.0;
				} else {
					buttonNext.IsEnabled = false;
					buttonNext.Opacity = .5;
				}
			}
			if (buttonPrevious != null) {
				if (listBoxPlaylist.SelectedIndex > 0) {
					buttonPrevious.IsEnabled = true;
					buttonPrevious.Opacity = 1.0;
				} else {
					buttonPrevious.IsEnabled = false;
					buttonPrevious.Opacity = .5;
				}
			}

			if (menuOptions != null) menuOptions.SetEnabled("Playlist", Playlist.Count > 0);

			log.Output(OutputType.Info, "Opened: " + currentlyPlayingItem + ", " + CurrentSource);

			VideoResolution = new Size(mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight);

			if (altMediaStreamSource != null) {
				string brs = "";

				ulong[] bitrates = AdaptiveGetAvailableBitrates();
				IList<IDictionary<MediaStreamAttributeKeys, string>> attributes = AdaptiveGetAttributes();
				List<AdaptiveSegment> segments = new List<AdaptiveSegment>();
				int brIndex = 0;
				if (bitrates != null) {
					for (int x = 0; x < bitrates.Length; x++) {
						long br = (long)bitrates[x];
						brs += br / 1024 + ",";
						if (br == AdaptiveCurrentBitrate) brIndex = x;
						IDictionary<MediaStreamAttributeKeys, string> attr = (attributes!=null && attributes.Count>x) ? attributes[x] : null;
						segments.Add(new AdaptiveSegment(){
							Bitrate = (long)br,
							Codec = (attr!=null) ? attr[MediaStreamAttributeKeys.VideoFourCC] : null,
							Resolution = (attr!=null) ? StringTools.SizefromString((attr[MediaStreamAttributeKeys.Width]??"0") + "x" + (attr[MediaStreamAttributeKeys.Height]??"0")):Size.Empty
						});
					}
					bitrateString = brs.Substring(0, brs.Length - 1);
					AdaptiveSegments = segments.ToArray();
				}

				AdaptiveFigureMaxResolution();
			}

		}

		private void AdaptiveFigureMaxResolution() {
			if (AdaptiveSegments != null) {
				Size max = Size.Empty;
				long bitrate = 0;
				int index = -1;
				double ch = MediaElementSize.Height-10;//slop

				for(int x = 0; x < AdaptiveSegments.Length; x++){
					AdaptiveSegment seg = AdaptiveSegments[x];
					int h = (int)seg.Resolution.Height;
					if ((h > ch && (max.Height < ch)) || (h < ch && h > max.Height)) {
						max = seg.Resolution;
						index = x;
						bitrate = seg.Bitrate;
					}
				}
				AdaptiveSegmentCapIndex = index;

				log.Output(OutputType.Info, string.Format("Max preferred content size is : {0}, at {1} - player is {2}",
					StringTools.SizetoString(max),
					StringTools.FriendlyBitsPerSec((int)bitrate),
					StringTools.SizetoString(MediaElementSize)));

				AdaptiveSetMaxBitrate(bitrate);
				lastMediaHeight = MediaElementSize.Height;
			}
		}

		void OutputLog_StaticOutputEvent(OutputEntry outputEntry) {
			logList.Add(outputEntry);
			while (logList.Count > 999) logList.RemoveAt(0);
		}

		public void OnMediaElementMediaEnded(object sender, RoutedEventArgs e) {
			SeekToNextItem();
		}

		public void OnMediaElementMediaFailed(object sender, RoutedEventArgs e) {
			//Debug.WriteLine("Content Failed! ");

			string error = (e as ExceptionRoutedEventArgs != null) ? "Message: " + ((ExceptionRoutedEventArgs)e).ErrorException.Message : null;
			MessageText = string.Format("Error opening {0}\n{1}{2}", CurrentSource, (error ?? "(Unknown Error)"), (Playlist.Count > currentlyPlayingItem + 1) ? "\n\nTrying next playlist item..." : "");
			log.Output(OutputType.Error, MessageText);

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
		/// Event callback to update the mediaElement with the volume's current value
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnSliderVolumeChanged(object sender, RoutedEventArgs e) {
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
		public void OnButtonClickMute(object sender, RoutedEventArgs e) {
			Muted = !Muted;
			CollapseMenus();
		}

		public event RoutedEventHandler FullScreenChanged;
		/// <summary>
		/// Event callback, supports fullscreen toggling by wrapping the PerformResize method.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnFullScreenChanged(object sender, EventArgs e) {
			PerformResize();
			if (FullScreenChanged != null) FullScreenChanged(this, new RoutedEventArgs());
		}

		/// <summary>
		/// Event callback, causes the player to go fullscreen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnButtonClickFullScreen(object sender, RoutedEventArgs e) {
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
		public void OnItemListSelectionChanged(object sender, RoutedEventArgs e) {
			SeekToPlaylistItem(listBoxPlaylist.SelectedIndex);
			if (PlayListOverlay) {
				//borderPlaylist.Visibility = Visibility.Collapsed; // hides the playlist
				if (isPlaying) borderPlaylist.Visibility = Visibility.Collapsed; // hides the playlist

				AdjustForPlaylist();
			}
			//borderChapters.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Event callback, fires when the user changes the current chapter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnChapterListSelectionChanged(object sender, RoutedEventArgs e) {
			currentlyPlayingChapter = listBoxChapters.SelectedIndex;
			if (currentlyPlayingChapter >= 0) {
				mediaElement.Position = TimeSpan.FromSeconds(Playlist[currentlyPlayingItem].Chapters[currentlyPlayingChapter].Position);
			}
		}

		/// <summary>
		/// Event callback, fires when the mediaElement hits a marker.  Markers may or may not be
		/// defined for each item in the playlist.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">Param</param>
		public void OnMediaElementMarkerReached(object sender, TimelineMarkerRoutedEventArgs e) {
			// Marker types could trigger add points, captions, interactions or chapters
			switch (MarkerTypeConv.StringToMarkerType(e.Marker.Type)) {
				case MarkerTypes.Chapter:
					if (listBoxChapters != null) {
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
				OnButtonClickFullScreen(this, new RoutedEventArgs());
			} else {
				//if there isnt another click, this will get picked up by our dispatcher timer to make a single click
				waitOnClick = true;
				lastClick = DateTime.Now;
			}

		}

		public void OnScrubberChanged(object sender, RoutedEventArgs e) {
			if (!timerIsUpdating) {
				mediaElement.Position = TimeSpan.FromSeconds(scrubberBar.Value);
			}
		}

		public void OnScrubberChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			if (!timerIsUpdating) {
				mediaElement.Position = TimeSpan.FromSeconds(e.Value);
			}
		}

		void OnVolumeChangeRequest(object sender, ScrubberBarValueChangeArgs e) {
			sliderVolume.Value = e.Value;
		}

		void OnVolumeMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			Point pt = e.MouseArgs.GetPosition(this);
			Double val = (e.Value > 1) ? 1 : e.Value;

			string text = string.Format("Volume: {0}%", (int)(val * 100));

			SetCustomToolTip(pt, text);

			if (e.MousePressed) {
				sliderVolume.Value = val;
			}
		}

		void OnVolumeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		private void HideTooltip() {
			if (customToolTip.Opacity > 0) customToolTip.Opacity = 0;
			customToolTip.Visibility = Visibility.Collapsed;
		}

		void OnScrubberMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		void qualityGauge_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			HideTooltip();
		}

		void qualityGauge_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			if (qualityGauge.Tag == null || qualityGauge.Tag.ToString() == "") qualityGauge.Tag = "Standard-Definition (loading)\n\n\n";
			SetCustomToolTip(e.GetPosition(this), qualityGauge.Tag.ToString());
		}

		void OnScrubberMouseOver(object sender, ScrubberBarValueChangeArgs e) {
			Point pt = e.MouseArgs.GetPosition(this);
			TimeSpan ts = TimeSpan.FromSeconds(e.Value);
			string text = "Seek to: " + string.Format("{0}:{1}:{2}", ts.Hours.ToString("00"), ts.Minutes.ToString("00"), ts.Seconds.ToString("00"));

			SetCustomToolTip(pt, text);

			if (e.MousePressed && e.Value < scrubberBar.Maximum && e.Value >= 0) {
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

		public void OnButtonClickMenu(object sender, RoutedEventArgs e) {
			if (menuOptions == null) return;

			menuOptions.Target = buttonMenu;
			menuOptions.Toggle();
		}

		#endregion

		#region IMediaControl (Scriptable) Members

		/// <summary>
		/// Causes the mediaElement to begin playing
		/// </summary>
		[ScriptableMember]
		public void Play() {
			string command = "Play";
			if (command == lastCommand) return;
			lastCommand = command;

			isPlaying = true;

			if (mediaElement != null) {
				if (Playlist.Count > 0 && currentlyPlayingItem == -1) {
					SeekToPlaylistItem(0);
					return;
				}
				if (mediaElement.CurrentState == MediaElementState.Stopped) {
					mediaElement.Position = new TimeSpan(0);
				}
				mediaElement.Play();
				//Debug.WriteLine("Play");
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
			if (!mediaElement.CanPause) {
				Stop();
				return;
			}

			string command = "Pause";
			if (command == lastCommand) return;
			lastCommand = command;

			mediaElement.Pause();

			log.Output(OutputType.Debug, "Command: " + command);
			isPlaying = false;
			ToolTipService.SetToolTip(buttonPlayPause, "Play");
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
			if (mediaElement != null && mediaElement.CurrentState != MediaElementState.Playing) {
				Play();
			} else {
				Pause();
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

			isPlaying = false;
			if (mediaElement != null) {
				mediaElement.Stop();
				mediaElement.AutoPlay = false;
				mediaElement.Source = null;
			}
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
			string command = "PrevChapter";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

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
			string command = "Next";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			SeekToPlaylistItem(currentlyPlayingItem + 1);
		}

		/// <summary>
		/// Attempts to seek to the previous item in the playlist
		/// </summary>
		[ScriptableMember]
		public void SeekToPreviousItem() {
			string command = "Prev";
			lastCommand = command;
			log.Output(OutputType.Debug, "Command: " + command);

			SeekToPlaylistItem(currentlyPlayingItem - 1);
		}

		/// <summary>
		/// Can change the volume up or down given a positive or negative number.
		/// </summary>
		/// <param name="incrementValue">Value to increment the volume. A negative number
		/// here causes a decrement</param>
		[ScriptableMember]
		public void VolumeIncrement(double incrementValue) {
			double currentVolume = mediaElement.Volume;
			currentVolume = Math.Min(1.0, Math.Max(0.0, currentVolume + incrementValue));
			mediaElement.Volume = currentVolume;
			sliderVolume.Value = currentVolume;
			lastUsedVolume = currentVolume;
			isMuted = sliderVolume.Value < LOWER_VOLUME_THRESHOLD;
		}

		///<summary>
		/// Shows or hides the debug panel
		/// </summary>
		[ScriptableMember]
		public void ToggleStatPanel() {
			StatVisibility = (StatVisibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			//CheckMenuHighlights();
			menuOptions.SetCheckState("Statistics", StatVisibility == Visibility.Visible);
		}

		[ScriptableMember]
		public void ToggleLogPanel() {
			LogVisibility = (LogVisibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			//CheckMenuHighlights();
			menuOptions.SetCheckState("Logs", LogVisibility == Visibility.Visible);
		}


		[ScriptableMember]
		public void ToggleControls() { ShowControls = !ShowControls; }

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

		[ScriptableMember]
		public string LinkUrl { get; set; }

		[ScriptableMember]
		public string EmbedUrl { get; set; }

		private Size lastSize;
		private Thickness lastMargin;

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

		[ScriptableMember]
		public void ToggleChapters() { ShowChapters = !ShowChapters; }

		[ScriptableMember]
		public bool ShowChapters {
			get { return borderChapters.Visibility == Visibility.Visible; }
			set { borderChapters.Visibility = (value && listBoxChapters.Items.Count > 0) ? Visibility.Visible : Visibility.Collapsed; }
		}

		[ScriptableMember]
		public void TogglePlaylist() { ShowPlaylist = !ShowPlaylist; }

		[ScriptableMember]
		public bool ShowPlaylist {
			get { return borderPlaylist.Visibility == Visibility.Visible; }
			set { borderPlaylist.Visibility = (value && (Playlist.Count > 1)) ? Visibility.Visible : Visibility.Collapsed; }
		}

		public Border MainBorder {
			get { return mainBorder; }
		}

		public Panel LayoutRoot {
			get { return layoutRoot; }
		}

		public Menu OptionsMenu {
			get { return menuOptions; }
			set { menuOptions = value; }
		}

		public int CurrentlyPlayingItem {
			get { return currentlyPlayingItem; }
			set { currentlyPlayingItem = value; }
		}


		#endregion
	}

	//for now we dont have much reason to care about audio, just get video info
	public struct AdaptiveSegment {
		public Size Resolution;
		public long Bitrate;
		public string Codec;
	}
}
