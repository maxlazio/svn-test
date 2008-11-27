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
using Microsoft.Windows.Controls.Theming;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Player.Visuals;
using org.OpenVideoPlayer.Util;

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
			PlaybackDurationText = PlaybackPositionText = "00:00";

			BufferingControlVisibility = Visibility.Collapsed;
			LogVisibility= StatVisibility = Visibility.Collapsed;

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

		#region Protected UI Element fields - bound to XAML Template
		protected Border mainBorder;
		protected Grid mainGrid;

		protected ListBox optionsMenu;

		protected FrameworkElement playToggle;
		protected FrameworkElement pauseToggle;
		protected MediaElement mediaElement;

		protected Button buttonPlayPause;
		protected Button buttonPrevious;
		protected Button buttonNext;
		protected Button buttonStop;
		protected Button buttonLinkEmbed;

		protected Grid buttonPlaylist;
		protected Grid buttonChapters;
		protected Grid menuScaling;
		protected Grid menuDebug;

		protected Grid menuLogs;
		protected Grid menuStats;
		protected Grid menuStretch;
		protected Grid menuFit;
		protected Grid menuFill;
		protected Grid menuNative;
		protected Grid menuNativeSmaller;

		protected Border subMenuDebugBox;
		protected Border subMenuScalingBox;
		protected ListBox subMenuDebug;
		protected ListBox subMenuScaling;

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

		protected Grid controlBox;
		protected Border statBox;
		protected Border customToolTip;
		protected Border messageBox;
		protected TextBlock messageBoxText;

		protected Path playSymbol;

		protected TextBox embedText;
		protected TextBox linkText;

		protected Border linkEmbedBox;
		protected LogViewer logViewer;
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

		private LogCollection logList = new LogCollection();
		private OutputLog log = new OutputLog("Player");

		SolidColorBrush sel = new SolidColorBrush(Color.FromArgb(0xFF, 0x37, 0x80, 0x94));//377994
		SolidColorBrush unsel = new SolidColorBrush(Colors.Transparent);

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
				if(mediaElement!=null) mediaElement.Stretch = StretchMode;
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

		public Visibility StatVisibility {
			get { return (Visibility)GetValue(StatVisibilityProperty); }
			set {
				SetValue(StatVisibilityProperty, value);
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
		//public static readonly DependencyProperty DebugLogTextProperty =
		//	DependencyProperty.Register("DebugLogText", typeof(String), typeof(OpenVideoPlayerControl), new PropertyMetadata(null));

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
		/// Binds all the protected properties of the object into the template
		/// </summary>
		protected void BindTemplate() {
			//use reflection to eliminate all that biolerplate binding code.
			//NOTE - field names must match element names in the xaml for binding to work!
			FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach(FieldInfo fi in fields) {
				if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) && fi.GetValue(this) == null) {
					object o = GetTemplateChild(fi.Name);
					if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType))) {
						fi.SetValue(this, o);
					} else {
						Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
					}
				}
			}
		}

		/// <summary>
		/// Overrides the controls OnApplyTemplate Method to capture and wire things up
		/// </summary>
		public override void OnApplyTemplate() {
			try {
				base.OnApplyTemplate();
				UnhookHandlers();
				BindTemplate();
				HookHandlers();

				ApplyConfiguration();
				CheckMenuHighlights();

				//    FrameworkElement layoutRoot = (FrameworkElement)GetTemplateChild("mainBorder");
				//    Uri uri = new Uri(@"OpenVideoPlayer;component/themes/default.xaml", UriKind.Relative);
				//    ImplicitStyleManager.SetResourceDictionaryUri(layoutRoot, uri);
				//    ImplicitStyleManager.SetApplyMode(layoutRoot, ImplicitStylesApplyMode.Auto);
				//    ImplicitStyleManager.Apply(layoutRoot);
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

			if (menuDebug != null) menuDebug.MouseLeftButtonDown += OnMenuDebug_MouseLeftButtonDown;
			if (menuScaling != null) menuScaling.MouseLeftButtonDown += OnMenuScaling_MouseLeftButtonDown;

			if (menuLogs != null) menuLogs.MouseLeftButtonDown += OnMenuLogs_MouseLeftButtonDown;
			if (menuStats != null) menuStats.MouseLeftButtonDown += OnMenuStats_MouseLeftButtonDown;

			if (menuFit != null) menuFit.MouseLeftButtonDown += OnMenuFit_MouseLeftButtonDown;
			if (menuNative != null) menuNative.MouseLeftButtonDown += OnMenuNative_MouseLeftButtonDown;
			if (menuFill != null) menuFill.MouseLeftButtonDown += OnMenuFill_MouseLeftButtonDown;
			//if (menuNativeSmaller != null) menuNativeSmaller.MouseLeftButtonDown += OnMenuNativeSmaller_MouseLeftButtonDown;
			if (menuStretch != null) menuStretch.MouseLeftButtonDown += OnMenuStretch_MouseLeftButtonDown;
			
			if (buttonMenu != null) {
				buttonMenu.Click += OnButtonClickMenu;
				buttonMenu.MouseEnter += OnMenuButton_MouseEnter;
				buttonMenu.MouseLeave += OnMenuButton_MouseLeave;
			}

			if (buttonFullScreen != null) {
				buttonFullScreen.Click += OnButtonClickFullScreen;
			}

			if (buttonPlaylist != null) {
				buttonPlaylist.MouseLeftButtonUp += OnButtonClickPlaylistItems;
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

			if (buttonChapters != null) {
				buttonChapters.MouseLeftButtonUp += OnButtonClickChapter;
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
				}
				else {
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

			if (mediaElement != null) {
				mediaElement.MediaFailed -= OnMediaElementMediaFailed;
				mediaElement.MediaOpened -= OnMediaElementMediaOpened;
				mediaElement.MediaEnded -= OnMediaElementMediaEnded;
				mediaElement.CurrentStateChanged -= OnMediaElementCurrentStateChanged;
				mediaElement.MarkerReached -= OnMediaElementMarkerReached;
				mediaElement.BufferingProgressChanged -= OnMediaElementBufferingProgressChanged;
				mediaElement.DownloadProgressChanged -= OnMediaElementDownloadProgressChanged;
			}

			if (buttonPlayPause != null) {
				buttonPlayPause.Click -= OnButtonClickPlayPause;
			}

			if (buttonStop != null) {
				buttonStop.Click -= OnButtonClickStop;
			}

			if (buttonPrevious != null) {
				buttonPrevious.Click -= OnButtonClickPrevious;
			}

			if (buttonNext != null) {
				buttonNext.Click -= OnButtonClickNext;
			}

			if (buttonMute != null) {
				buttonMute.Click -= OnButtonClickMute;
			}

			if (buttonMenu != null) {
				buttonMenu.Click -= OnButtonClickMenu;
			}

			if (closeChapters != null) {
				closeChapters.MouseLeftButtonUp -= OnCloseChapters_Click;
			}

			if (buttonFullScreen != null) {
				buttonFullScreen.Click -= OnButtonClickFullScreen;
			}

			if (menuDebug != null) menuDebug.MouseLeftButtonDown -= OnMenuDebug_MouseLeftButtonDown;
			if (menuScaling != null) menuScaling.MouseLeftButtonDown -= OnMenuScaling_MouseLeftButtonDown;

			if (menuLogs != null) menuLogs.MouseLeftButtonDown -= OnMenuLogs_MouseLeftButtonDown;
			if (menuStats != null) menuStats.MouseLeftButtonDown -= OnMenuStats_MouseLeftButtonDown;

			if (menuFit != null) menuFit.MouseLeftButtonDown -= OnMenuFit_MouseLeftButtonDown;
			if (menuFill != null) menuFill.MouseLeftButtonDown -= OnMenuFill_MouseLeftButtonDown;
			if (menuNative != null) menuNative.MouseLeftButtonDown -= OnMenuNative_MouseLeftButtonDown;
		//	if (menuNativeSmaller != null) menuNativeSmaller.MouseLeftButtonDown -= OnMenuNativeSmaller_MouseLeftButtonDown;
			if (menuStretch != null) menuStretch.MouseLeftButtonDown -= OnMenuStretch_MouseLeftButtonDown;

			if(closePlaylist!=null) {
				closePlaylist.MouseLeftButtonUp -= OnClosePlaylist_Click;
			}

			if (buttonPlaylist != null) {
				buttonPlaylist.MouseLeftButtonUp -= OnButtonClickPlaylistItems;
			}

			if (listBoxPlaylist != null) {
				listBoxPlaylist.SelectionChanged -= OnItemListSelectionChanged;
			}

			if (listBoxChapters != null) {
				listBoxChapters.SelectionChanged -= OnChapterListSelectionChanged;
			}

			if (buttonChapters != null) {
				buttonChapters.MouseLeftButtonUp -= OnButtonClickChapter;
			}

			if (scrubberBar != null) {
				scrubberBar.ValueChanged -= OnScrubberChanged;
				scrubberBar.ValueChangeRequest -= OnScrubberChangeRequest;
				scrubberBar.MouseOver -= OnScrubberMouseOver;
				scrubberBar.MouseLeave -= OnScrubberMouseLeave;
			}

			if (sliderVolume != null) {
				sliderVolume.ValueChanged -= OnSliderVolumeChanged;
				sliderVolume.ValueChangeRequest -= OnVolumeChangeRequest;
				sliderVolume.MouseOver -= OnVolumeMouseOver;
				sliderVolume.MouseLeave -= OnVolumeMouseLeave;
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
			if(logViewer!=null) {
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

			if (string.IsNullOrEmpty(EmbedUrl) && string.IsNullOrEmpty(LinkUrl)) {//check embed 
				controlBox.ColumnDefinitions[7].Width = new GridLength(0);
			} else {
				if (linkText != null && LinkUrl!=null) linkText.Text = LinkUrl;
				if (embedText != null && EmbedUrl!=null) embedText.Text = EmbedUrl;
			}

			//Call the fullscreen support for if we're starting in fullscreen
			PerformResize();

			if (mediaElement != null) {
				mediaElement.AutoPlay = autoplaySetting;
				mediaElement.Stretch = stretchMode;

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
					altMediaStreamSource = adaptiveConstructor.Invoke(new object[] {mediaElement, new Uri(Playlist[currentlyPlayingItem].Url)}) as MediaStreamSource;
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

			SetChapterButtonVisibility();

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
		/// Sets the chapter button to a style appropriate for the presence/absence of
		/// chapters in the current playlist.
		/// </summary>
		private void SetChapterButtonVisibility() {
			buttonChapters.Opacity = Playlist[currentlyPlayingItem].Chapters.Count <= 0 ? .5 : 1;
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
				string state = (mediaElement == null) ? "" : " (" + mediaElement.CurrentState + ")";
				sb.AppendFormat("OpenVideoPlayer v{0}{1}", version, state);

				if (mediaElement != null) {
					if (isPlaying) sb.AppendFormat("\n{0}/{1} FPS (Render/Drop), Res: {2}x{3}", mediaElement.RenderedFramesPerSecond, mediaElement.DroppedFramesPerSecond, mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight);

					if (mediaElement.DownloadProgress > 0 && mediaElement.DownloadProgress < 1) {
						sb.AppendFormat("\nDownload progress: {0}%", (int) (100*mediaElement.DownloadProgress));
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
						sb.AppendFormat("\n{0} kbps", brst.Substring(0, brst.Length - 1));
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
							sb.AppendFormat("\nCurrent Buffer: {0} KB, {1} sec", bs / 1024 / 8, Math.Round(ts.TotalSeconds, 1));
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

			//OpenVideoPlayer Loaded
		}

		void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e) {
			//Debug.WriteLine("Exception: " + e.ExceptionObject);
			log.Output(OutputType.Error, "Error: ", e.ExceptionObject);			
			e.Handled = true;
		}

		public void OnButtonClickPlaylistItems(object sender, RoutedEventArgs e) {
			//PlayListOverlay = false;

			if (borderPlaylist != null) {
				ShowPlaylist = !ShowPlaylist;
				//borderPlaylist.Visibility = (borderPlaylist.Visibility == Visibility.Collapsed) ?
				//    Visibility.Visible : Visibility.Collapsed;

				AdjustForPlaylist();
			}

			CollapseMenus();
			UnSelectMenus();
		}

		private void AdjustForPlaylist() {
			if (PlayListOverlay == false) {
				mainBorder.Margin = new Thickness(0, 0, ((borderPlaylist.Visibility == Visibility.Collapsed) ? 0 : listBoxPlaylist.ActualWidth), 0);
				borderPlaylist.Margin = new Thickness(0, 5, 3, 5);
			} else {
				borderPlaylist.Margin = new Thickness(0, 3, 3, 24);
			}
		}

		public void OnButtonClickChapter(object sender, RoutedEventArgs e) {
			if (borderChapters != null && currentlyPlayingItem > 0 && Playlist.Count > currentlyPlayingItem && Playlist[currentlyPlayingItem].Chapters.Count > 0) {
				//borderChapters.Visibility = (borderChapters.Visibility == Visibility.Collapsed) ?
				//    Visibility.Visible : Visibility.Collapsed;
				ShowChapters = !ShowChapters;
			}

			CollapseMenus();
			UnSelectMenus();
		}

		public void OnTimerTick(object sender, EventArgs e) {
			try {
				if (mediaElement == null) {
					return;
				}
				timerIsUpdating = true;

				if (PlayListOverlay == false && borderPlaylist.Visibility == Visibility.Visible && mainBorder.Margin.Right == 0) {
					mainBorder.Margin = new Thickness(0, 0, listBoxPlaylist.ActualWidth, 0);
				}

				buttonPlaylist.Opacity = (Playlist.Count < 2) ? .5 : 1;

				TimeSpan pos = mediaElement.Position;
				scrubberBar.IsEnabled = mediaElement.NaturalDuration.TimeSpan != TimeSpan.Zero;
				PlaybackPosition = pos.TotalSeconds;

				//hack - sometimes gets into a wierd state.  need to l0ok more at this
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

				PlaybackPositionText = (dur.Hours >= 1) ? string.Format("{0}:{1}:{2}", pos.Hours.ToString("00"), pos.Minutes.ToString("00"), pos.Seconds.ToString("00"))
					: string.Format("{0}:{1}", pos.Minutes.ToString("00"), pos.Seconds.ToString("00"));

				if (listBoxChapters != null && currentlyPlayingChapter >= 0 && currentlyPlayingChapter < listBoxChapters.Items.Count && listBoxChapters.SelectedIndex != currentlyPlayingChapter) {
					// set the currently playing chapter on the list box without triggering our events
					listBoxChapters.SelectionChanged -= OnChapterListSelectionChanged;
					listBoxChapters.SelectedIndex = currentlyPlayingChapter;
					listBoxChapters.SelectionChanged += OnChapterListSelectionChanged;

					// move that into view
					listBoxChapters.ScrollIntoView(listBoxChapters.Items[currentlyPlayingChapter]);
				}

				UpdateDebugPanel();

				if (updateDownloading) {
					updateDownloading = false;

					DownloadPercent = mediaElement.DownloadProgress*100;
					DownloadOffsetPercent = mediaElement.DownloadProgressOffset*100;
				}

				if (updateBuffering) {
					updateBuffering = false;

					BufferingControlVisibility = (mediaElement.BufferingProgress < 1) ? Visibility.Visible : Visibility.Collapsed;
					BufferingPercent = mediaElement.BufferingProgress*100;
				}

				//Catch single click for play pause
				if (waitOnClick && DateTime.Now - lastClick >= TimeSpan.FromMilliseconds(250)) {
					waitOnClick = false;
					TogglePlayPause();
				}

				timerIsUpdating = false;

			}catch(Exception ex) {
				//Debug.WriteLine("Error in timer: " + ex);
				log.Output(OutputType.Error, "Timer Error: ", ex);
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
			}else if (mediaElement.CurrentState == MediaElementState.Opening) {
			} else if (mediaElement.CurrentState == MediaElementState.Closed) {
			} else {
				playToggle.Visibility = Visibility.Visible;
				pauseToggle.Visibility = Visibility.Collapsed;

			//	isPlaying = false;
				ToolTipService.SetToolTip(buttonPlayPause, "Play");
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

				controlBox.ColumnDefinitions[1].Width = controlBox.ColumnDefinitions[3].Width = new GridLength((dur.Hours >= 1) ? 56 : 36);
			} else {
				PlaybackDurationText = "(Live)";
			}

			if (isPlaying) {
				Play();
			}

			listBoxPlaylist.SelectedIndex = currentlyPlayingItem;
			messageBox.Visibility = Visibility.Collapsed;

			log.Output(OutputType.Info, "Opened: " + currentlyPlayingItem + ", " + CurrentSource);
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

			string error = (e as ExceptionRoutedEventArgs !=null) ? "Message: " + ((ExceptionRoutedEventArgs)e).ErrorException.Message : null;
			MessageText = string.Format("Error opening {0}\n{1}{2}", CurrentSource, (error ?? "(Unknown Error)"), (Playlist.Count>currentlyPlayingItem+1)?"\n\nTrying next playlist item...":"");
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
		/// Event callback to update the mediaElement with the volume's current value
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnSliderVolumeChanged(object sender, RoutedEventArgs e) {
			mediaElement.Volume = sliderVolume.Value;
			isMuted = sliderVolume.Value < LOWER_VOLUME_THRESHOLD;
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

			if (optionsMenu.Visibility == Visibility.Visible) {
				CollapseMenus();
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

		public void OnButtonClickMenu(object sender, RoutedEventArgs e) {
			optionsMenu.Visibility = (optionsMenu.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			subMenuDebugBox.Visibility = subMenuScalingBox.Visibility = Visibility.Collapsed;
			UnSelectMenus();
		}

		void OnMenuButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			//optionsMenu.Visibility = Visibility.Collapsed;
			//subMenuDebugBox.Visibility = subMenuScalingBox.Visibility = Visibility.Collapsed;
			//UnSelectMenus();
		}

		void OnMenuButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
			//optionsMenu.Visibility =  Visibility.Visible;
			//subMenuDebugBox.Visibility = subMenuScalingBox.Visibility = Visibility.Collapsed;
			//UnSelectMenus();
		}


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
			ToggleStatPanel();

			UnSelectMenus();
			CollapseMenus();
			CheckMenuHighlights();
		}

		void OnMenuLogs_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			ToggleLogPanel();

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

		void OnMenuFill_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			StretchMode = Stretch.UniformToFill;

			UnSelectMenus();
			CheckMenuHighlights();
			CollapseMenus();
		}

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
			if (menuFill!= null) menuFill.Background = (StretchMode == Stretch.UniformToFill) ? sel : unsel;
			if (menuStretch != null) menuStretch.Background = (StretchMode == Stretch.Fill) ? sel : unsel;
			if (menuStats != null) menuStats.Background = (StatVisibility == Visibility.Collapsed) ? unsel : sel;
			if (menuLogs != null) menuLogs.Background = (LogVisibility == Visibility.Collapsed) ? unsel : sel;
			//menuNativeSmaller.Background = (menuNativeSmaller.Background == sel) ? unsel : sel;
		}

		#endregion

		#region IMediaControl (Scriptable) Members

		protected MediaElementState lastMediaState = MediaElementState.Closed;
		protected string lastCommand;
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
				if(Playlist.Count > 0 && currentlyPlayingItem == -1) {
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
			if (mediaElement != null && mediaElement.CurrentState != MediaElementState.Playing) {
				Play();
			}else {
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
			CheckMenuHighlights();
		}

		[ScriptableMember]
		public void ToggleLogPanel() {
			LogVisibility = (LogVisibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
			CheckMenuHighlights();
		}


		[ScriptableMember]
		public void ToggleControls() { ShowControls = !ShowControls; }

		[ScriptableMember]
		public bool ShowControls { 
			get { return controlBox.Visibility == Visibility.Visible; }
			set {
				controlBox.Visibility = (value) ? Visibility.Visible : Visibility.Collapsed;
				mainGrid.RowDefinitions[2].Height = (value) ? new GridLength(32) : new GridLength(0); 
			}
		}

		[ScriptableMember]
		public string CurrentSource {
			get {
				string plsource = (Playlist != null && Playlist.Count > currentlyPlayingItem && Playlist[currentlyPlayingItem] != null) ? Playlist[currentlyPlayingItem].Url : null;
				string source = (mediaElement.Source == null) ? plsource : mediaElement.Source.ToString(); //TODO - add support for non-playlisted adapative sources?
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
			if (buttonFullScreen != null) {
				if (!full) {
					ToolTipService.SetToolTip(buttonFullScreen, "Restore Screen");
				}
				else {
					ToolTipService.SetToolTip(buttonFullScreen, "FullScreen");
				}
			}
			if(optionsMenu!=null) optionsMenu.Visibility = Visibility.Collapsed;
		}

		[ScriptableMember]
		public bool ShowChapters{
			get { return borderChapters.Visibility == Visibility.Visible; }
			set { borderChapters.Visibility = (value && listBoxChapters.Items.Count > 0) ? Visibility.Visible : Visibility.Collapsed; }
		}

		[ScriptableMember]
		public bool ShowPlaylist {
			get { return borderPlaylist.Visibility == Visibility.Visible; }
			set { borderPlaylist.Visibility = (value && (Playlist.Count > 1)) ? Visibility.Visible : Visibility.Collapsed; }
		}

		public Border MainBorder {
			get { return mainBorder; }
		}

		#endregion

		internal void SetTheme(Uri uri) {
			try {
				ImplicitStyleManager.SetResourceDictionaryUri(MainBorder, uri);
				ImplicitStyleManager.SetApplyMode(MainBorder, ImplicitStylesApplyMode.Auto);
				ImplicitStyleManager.Apply(MainBorder);
			}catch(Exception ex) {
				log.Output(OutputType.Error, "Couldnt set theme: ", ex);
			}
		}
	}
}
