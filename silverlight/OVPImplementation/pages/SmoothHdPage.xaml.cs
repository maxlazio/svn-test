using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using org.OpenVideoPlayer.Controls;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Util;
using org.OpenVideoPlayer;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Shapes;
using org.OpenVideoPlayer.Controls.Visuals;

namespace OVPImplementation {
	[ScriptableType]
	public partial class SmoothHdPage : UserControl {
		#region Members and constructor
		private PlaylistCollection groups = new PlaylistCollection();
		private List<PlaylistCollection> playlists = new List<PlaylistCollection>();		
		Dictionary<string, Image> images = new Dictionary<string, Image>();
		Dictionary<string, BitmapImage> sources = new Dictionary<string, BitmapImage>();

		bool lastFSMode;
		bool needAutoPlay = true;
		Image lastImage = null;
		string lastSource = null;
		int element = 0;
		DateTime start = DateTime.Now;
		Box creditsBox = null;
		OutputLog log = new OutputLog("smoothhd");
		private Dictionary<string, object> ads = new Dictionary<string, object>();

		//from item templates //todo get dynamically
		private double pliWidth = 209;
		private double gliHeight = 39;

		List<int[]> randomList = new List<int[]>();
		int plLoadedCount;
		int currentRandomIndex = 0;
		//bool randomFlag = false;
		bool hasRogan = false;
		List<string> feedList;

		public SmoothHdPage(object sender, StartupEventArgs e) {
			string val;
			if (e.InitParams.TryGetValue("masterplaylist", out val)) {
				WebClient wc = new WebClient();
				wc.DownloadStringCompleted += wc_DownloadStringCompleted;
				Uri uri = (Uri.IsWellFormedUriString(val, UriKind.Absolute)) ? new Uri(val) : new Uri(HtmlPage.Document.DocumentUri, val);
				wc.DownloadStringAsync(uri);
			}

			InitializeComponent();
			HtmlPage.RegisterScriptableObject("page", this);

			//return;

			//auto scaling settings
			Player.AutoScaling =  OpenVideoPlayerControl.AutoScalingType.ScaleObjectTag; 
			Player.AutoScalingPlayerMargin = new Size(16, 200); //need extra at bottom for playlist
			Player.AutoScalingBrowserMargin = new Size(60, 120);
			Player.AutoScalingForcedAspect = new Size(16, 9);
			Player.AutoScalingMinimumSize = new Size(848, 480);

			//events
			Player.FullScreenChanged += new EventHandler(Player_FullScreenChanged);
			//Player.ItemChanged += new RoutedEventHandler(Player_ItemChanged);
			Player.PlaylistIndexChanging += new PlaylistIndexChangingEventHandler(Player_PlaylistIndexChanging);
			Player.SizeChanged += new SizeChangedEventHandler(Player_SizeChanged);
			Player.OnStartup(sender, e);

			PluginManager.PluginLoaded += new PluginManager.PluginEventHandler(PluginManager_PluginLoaded);

			ApplyTemplate();
			Player.ApplyTemplate();
			groupPlaylist.ItemsSource = groups;
			groupPlaylist.SelectionChanged += groupPlaylist_SelectionChanged;
			listBoxPlaylist.SelectionChanged += listBoxPlaylist_SelectionChanged;

			//Ad containers//
			Screen s = new Screen() { Name = "screen" };
			Player.LayoutRoot.Children.Add(s);
			Player.Containers.Combine("PlayerFill", s);//Player.LayoutRoot);
			Player.Containers.Combine("PlaylistBug", BugContainer);
			Player.Containers.Combine("LowerBanner", BannerContainer);

			//hide the playlist and chapters menuitems/buttons, and the border of the player
			foreach (MenuItem mi in Player.OptionsMenu.Items) {
				if (mi.Text == "Chapters" || mi.Text == "Playlist") {
					mi.Visibility = Visibility.Collapsed;
				}
			}
			foreach (FrameworkElement fe in Player.ControlBox.Children) {
				if (fe is Button && (fe.Name.Contains("Chapters") || fe.Name.Contains("Playlist") || fe.Name.Contains("Next") || fe.Name.Contains("Previous"))) {
					fe.Visibility = Visibility.Collapsed;
					fe.Margin = new Thickness(0);
					fe.Width = 0.0;
				}
			}
			foreach (FrameworkElement fe in Player.Children) {
				Rectangle h = fe as Rectangle;
				if (h!=null && h.Name == "highlightBorder") h.StrokeThickness = 0;
			}

		}

		private bool random = true;
		[ScriptableMember]
		public bool Random {
			get { return random; }
			set {
				if (random != value) {
					random = value;
					if (random) GoNext();
				}
			}
		}

		[ScriptableMember]
		public void Credits() {
			if (creditsBox == null) {
				Image credits = new Image() { Source = new BitmapImage(new Uri(HtmlPage.Document.DocumentUri, "content/uif/credits.png")) };
				ScrollViewer sv = new ScrollViewer() { Content = credits, Margin = new Thickness(0, 20, 0, 20) };
				creditsBox = new Box() { Content = sv, VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Center };
				LayoutRoot.Children.Add(creditsBox);
			} else {
				creditsBox.Visibility = (creditsBox.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
			}

			if (creditsBox.Visibility == Visibility.Visible) {
				Player.Pause();
			}
		}

		#endregion

		#region Player Events
		void Player_FullScreenChanged(object sender, EventArgs e) {
			if (lastFSMode == Application.Current.Host.Content.IsFullScreen) return;
			lastFSMode = Application.Current.Host.Content.IsFullScreen;

			if (Application.Current.Host.Content.IsFullScreen) {
				playlistStackPanel.Visibility = Visibility.Collapsed;
				Player.SetValue(Grid.RowSpanProperty, 2);
			} else {
				playlistStackPanel.Visibility = Visibility.Visible;
				Player.SetValue(Grid.RowSpanProperty, 1);
			}

			PositionAdCanvas();
		}

		void Player_PlaylistIndexChanging(object sender, PlaylistIndexChangingEventArgs args) {
			if (Random) {
				if (Player.Position > TimeSpan.Zero && Player.Position == Player.Duration && Player.CurrentItem!=null && listBoxPlaylist.SelectedItem != null && Player.CurrentItem.Url == ((VideoItem)listBoxPlaylist.SelectedItem).Url) {// && Player.Playlist != listBoxPlaylist.Items || ) {//!randomFlag) {
					args.Cancel = true;
					GoNext();
					return;
				}
			}

			if (args.NewIndex >= Player.Playlist.Count) {
				needAutoPlay = true;
				if (groupPlaylist.SelectedIndex < groupPlaylist.Items.Count - 1) {
					groupPlaylist.SelectedIndex++;
				} else {
					groupPlaylist.SelectedIndex = 0;
				}
			} else {//if (!Random){
				listBoxPlaylist.SelectedIndex = args.NewIndex;
			}
		}

		void Player_SizeChanged(object sender, SizeChangedEventArgs e) {

			AdjustPlaylistSize();
			AdjustPageElementSize();
			PositionAdCanvas();
		}

		private void AdjustPageElementSize() {
			if (Player.AutoScalingCalculatedPlayerSize.Sum() == 0) return;
			//set SL container DIV height
			HtmlPage.Plugin.Parent.SetStyleAttribute("height", Player.AutoScalingCalculatedPlayerSize.Height + "px");

			//and it's container div width (for proper centering)
			Size bSize = Player.AutoScalingCalculatedPlayerSize.Scale(Player.AutoScalingBrowserMargin);
			if (bSize.Width < 980) bSize.Width = 980;
			if (bSize.Height < 870) bSize.Height = 870;
			HtmlPage.Plugin.Parent.Parent.SetStyleAttribute("width", bSize.Width + "px");
			HtmlPage.Plugin.Parent.Parent.SetStyleAttribute("height", bSize.Height + "px");
		}
		#endregion

		#region Playlist handling and other supporting methods

		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if(e.Error!=null) {
				Debug.WriteLine("Error: " + e.Error);
				return;
			}

			string[] feeds = e.Result.Split('\n');
			feedList = new List<string>();
			foreach (string f in feeds) {
				if (!string.IsNullOrEmpty(f) && f.Trim().Length > 0 && !f.Trim().StartsWith("#")) {
					feedList.Add(f);
					if (f.Contains("15433") && !hasRogan) {
						hasRogan = true;
						random = false;
					}
				}
			}

			log.Output(OutputType.Info, string.Format("Parsing master playlist, {0} items, {1}ms", feedList.Count, (DateTime.Now - start).TotalMilliseconds));
			foreach (string feed in feedList) {
				PlaylistCollection pl = new PlaylistCollection();
				pl.LoadComplete += pl_LoadComplete;
				playlists.Add(pl);

				PlayerInitParameterParser p = new PlayerInitParameterParser();
				p.ParseSource(pl, "feedsource", feed);
				groups.Add(new VideoItem());
			}
		}

		void pl_LoadComplete(object sender, RoutedEventArgs e) {
			PlaylistCollection pl = (PlaylistCollection) sender;
			log.Output(OutputType.Info, string.Format("Completed Playlist: {0}, {1}, {2} items, {3} ms", playlists.IndexOf(pl), pl.Title, pl.Count, (DateTime.Now - start).TotalMilliseconds));

			int x; for (x = 0; x < playlists.Count; x++) if (pl == playlists[x]) break;
			
			groups[x] = new VideoItem { Author = pl.Author, Title = pl.Title, 
				Thumbnails = new List<org.OpenVideoPlayer.Media.Thumbnail> { 
					new org.OpenVideoPlayer.Media.Thumbnail(pl.ImageURL +((pl == playlists[0])?"_1.jpg" : "_3.jpg")) 
				},
				Url = pl.SourceURI 
			};

			plLoadedCount++;

			for (int i = 1; i <= 3; i++) {
				string url = pl.ImageURL + "_" + i + ".jpg";
				BitmapImage bi = new BitmapImage(new Uri(url));
				//Debug.WriteLine("Adding : " + url);
				sources.Add(url, bi);
			}

			lock (randomList) {
				for (int y = 0; y < pl.Count; y++) {
					randomList.Add(new int[] {x, y});
				}
				if (playlistStackPanel.Opacity > 0) {
					//this means we've loaded it, so we just added to the randomized list, we need to re-randomize
					randomList.Randomize(currentRandomIndex);
				}
			}

			if (!Random) {
				if (pl == playlists[0]) {
					groupPlaylist.SelectedIndex = 0;
					playlistStackPanel.Opacity = 1;
					if (feedList.Count < 2) {
						groupPanel.Visibility = Visibility.Collapsed;
						AdjustPlaylistSize();
					}
				}
			} else {
				if (playlistStackPanel.Opacity<1 && (plLoadedCount == playlists.Count || DateTime.Now - start > TimeSpan.FromSeconds(3))) {
					Debug.WriteLine(string.Format("Starting - pl count: {0}, {1}s", plLoadedCount, (DateTime.Now - start).TotalSeconds));
					playlistStackPanel.Opacity = 1;
					randomList.Randomize();
					GoNext();
				}
			}
		}

		private void GoNext() {
			currentRandomIndex++;
			if (currentRandomIndex >= randomList.Count) {
				currentRandomIndex = 0;
				randomList.Randomize();
			}

			needAutoPlay = true;
			groupPlaylist.SelectedIndex = randomList[currentRandomIndex][0];
		}

		void groupPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				VideoItem vi = groupPlaylist.SelectedItem as VideoItem;
				if (vi == null) return;

				PlaylistCollection pl = null;
				foreach (PlaylistCollection p in playlists) {
					if (p.Title == vi.Title) {
						pl = p;
						break;
					}
				}
				if (pl == null) return;

				listBoxPlaylist.ItemsSource = pl;
				Player.Playlist = pl;

				if (pl.ImageURL.Contains("rogan") && pl.Count == 3) {
					pl.Add(pl[0]); pl.Add(pl[1]); pl.Add(pl[2]);
				}

				AdjustPlaylistSize();

				if (needAutoPlay) {
					needAutoPlay = false;
					listBoxPlaylist.SelectedIndex = (Random) ? randomList[currentRandomIndex][1] : 0;

				} else {
					if (pl.Count > Player.CurrentIndex && Player.CurrentSource == pl[Player.CurrentIndex].Url) {
						listBoxPlaylist.SelectedIndex = Player.CurrentIndex;
					}
				}

				foreach (string u in images.Keys) {
				    if (u.StartsWith(vi.ThumbSource)) {
				        Image i = images[u];
						SwapImage(i, "1");
						SwapImage(lastImage, "3");
				        lastImage = i;
				        break;
				    }
				}

				if (hasRogan) {
					//handle the extra logos at bottom when rogan channel is displayed
					bool rogan = (vi.ThumbSource.ToLower().Contains("rogan"));
					HtmlElement ads1 = HtmlPage.Document.GetElementById("ads1");
					HtmlElement ads2 = HtmlPage.Document.GetElementById("ads2");
					if (ads1 != null) ads1.SetStyleAttribute("visibility", ((rogan) ? "visible" : "hidden"));
					if (ads2 != null) ads2.SetStyleAttribute("visibility", ((rogan) ? "visible" : "hidden"));
				}

				HideBugs();
				PositionAdCanvas();
				if(creditsBox!=null) creditsBox.Visibility = Visibility.Collapsed;

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error on change", ex);
			}
		}

		private void AdjustPlaylistSize() {
			double plWidth = Player.ActualWidth - ((groupPanel.Visibility== Visibility.Visible)? groupPlaylist.FullSize().Width : 0.0);
			scrollLeft.Visibility = scrollRight.Visibility = Visibility.Collapsed;
			if ((plWidth < listBoxPlaylist.Items.Count * pliWidth)) {
				scrollLeft.Visibility = scrollRight.Visibility = Visibility.Visible;
				scrollLeft.IsEnabled = false;
				scrollRight.IsEnabled = true;
				listBoxPlaylist.Width = plWidth - 56;
			} else {
				listBoxPlaylist.Width = Double.NaN;
			}
		}

		void listBoxPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (listBoxPlaylist.SelectedIndex >=0 && (listBoxPlaylist.SelectedIndex != Player.CurrentIndex
				|| ((VideoItem)listBoxPlaylist.SelectedItem).Url != Player.CurrentSource)) {

				Player.SeekToPlaylistItem(listBoxPlaylist.SelectedIndex);
			}

			PositionAdCanvas();
			if(creditsBox!=null)creditsBox.Visibility = Visibility.Collapsed;
		}

		private void scrollDown_Click(object sender, RoutedEventArgs e) {
			groupPlaylist.VerticalOffset += gliHeight;
			if (groupPlaylist.VerticalOffset >= groupPlaylist.MaxVerticalOffset - groupPlaylist.ActualHeight - gliHeight - 2) scrollDown.IsEnabled = false;
			scrollUp.IsEnabled = true;
		}

		private void scrollUp_Click(object sender, RoutedEventArgs e) {
			groupPlaylist.VerticalOffset -= gliHeight;
			if (groupPlaylist.VerticalOffset <= gliHeight + 2 || groupPlaylist.VerticalOffset < (gliHeight / 2)) {
				scrollUp.IsEnabled = false;
				groupPlaylist.VerticalOffset = 0;
			}
			scrollDown.IsEnabled = true;
		}

		private void scrollLeft_Click(object sender, RoutedEventArgs e) {
			listBoxPlaylist.HorizontalOffset -= pliWidth;
			if (listBoxPlaylist.HorizontalOffset <= pliWidth || listBoxPlaylist.HorizontalOffset < pliWidth + (pliWidth / 2)) {
				listBoxPlaylist.HorizontalOffset = 0;
				scrollLeft.IsEnabled = false;
			}
			scrollRight.IsEnabled = true;

			PositionAdCanvas();
			//HideBugs();
		}

		private void scrollRight_Click(object sender, RoutedEventArgs e) {
			listBoxPlaylist.HorizontalOffset += pliWidth;
			if (listBoxPlaylist.HorizontalOffset >= listBoxPlaylist.MaxHorizontalOffset - listBoxPlaylist.ActualWidth - pliWidth - 2 ||
				listBoxPlaylist.HorizontalOffset > ((pliWidth * (listBoxPlaylist.Items.Count - 1)) - pliWidth / 2) - listBoxPlaylist.ActualWidth) {
				listBoxPlaylist.HorizontalOffset = listBoxPlaylist.MaxHorizontalOffset;
				scrollRight.IsEnabled = false;
			}
			scrollLeft.IsEnabled = true;

			PositionAdCanvas();
			//HideBugs();
		}

		#endregion

		#region Image and details handling

		void pli_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			SwapImage((Image)sender,"3");
		}

		void pli_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			SwapImage((Image)sender,"2");
		}

		private void SwapImage(Image i, string tag) {
			if (i == null) return;
			PlaylistCollection pl = null;
			foreach (PlaylistCollection p in playlists) if (p.Title == i.Tag.ToString()) pl = p;
			if (pl == Player.Playlist && tag!="1") return;
			string s = pl.ImageURL + "_" + tag + ".jpg";
			if (sources.ContainsKey(s)) {
				i.Source = sources[s];
			} else {
				i.Source = new BitmapImage(new Uri(s));
			}
		}

		private void Image_Loaded(object sender, RoutedEventArgs e) {
			string url = ((BitmapImage)((Image)sender).Source).UriSource.ToString();
			if(string.IsNullOrEmpty(url))return;

			if (!images.ContainsKey(url)) {
				images.Add(url, sender as Image);
				if (url.Contains("_1.jpg")) {
					lastImage = sender as Image;
				}
			}
		}

		private void Image_Loaded_1(object sender, RoutedEventArgs e) {
			string title = ((FrameworkElement)sender).Tag as string;
			if (title!=null && !images.ContainsKey(title)) images.Add(title, sender as Image);
		}

		private void DetailsButtonPress(object sender, RoutedEventArgs e) {
			ToggleDetails(sender, true);
		}

		private void DetailsButtonClose(object sender, RoutedEventArgs e) {
			ToggleDetails(sender, false);
		}

		private void ToggleDetails(object sender, bool show) {
			Button b = sender as Button;
			Grid g = b.Parent as Grid;
			if (g == null) g = ((FrameworkElement)b.Parent).Parent as Grid;
			foreach (FrameworkElement fe in g.Children) {
				Canvas c = fe as Canvas;
				if (c != null) {
					c.Visibility = (show)?Visibility.Visible: Visibility.Collapsed;
				}
			}
		}

		private void OVPlink_Click(object sender, RoutedEventArgs e) {
			string tag = ((FrameworkElement)sender).Tag.ToString();
			HtmlPage.Window.Navigate(new Uri(tag), "OVP");
		}

		private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
			TogglePlayIcon(sender, true);
		}

		private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			TogglePlayIcon(sender, false);
		}

		private void TogglePlayIcon(object sender, bool show) {
			Grid g = sender as Grid;
			if (g == null) return;

			try {
				foreach (FrameworkElement fe in g.Children) {
					Image i = fe as Image;
					if (i != null && ((VideoItem) listBoxPlaylist.SelectedItem).Url == Player.CurrentSource
					    && ((BitmapImage) i.Source).UriSource.ToString().StartsWith(((VideoItem) listBoxPlaylist.SelectedItem).ThumbSource)) return;
				}
			} catch {}
			foreach (FrameworkElement fe in g.Children) {
				ContentControl c = fe as ContentControl;
				if (c != null && c.Name == "playIcon") {
					c.Visibility = (show) ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		#endregion

		#region Ad Handling
		void PluginManager_PluginLoaded(object sender, PluginEventArgs args) {
			if (args == null) return;
			try {
				if (args.PluginType.ToString().Contains("UIFAdConnector")) {
					//hack in to the ad loading event so we can position the bug
					ReflectionHelper.AttachEvent(args.Plugin, "AdLoading", this, "OnUifAdLoading");
					ReflectionHelper.AttachEvent(args.Plugin, "AdUnLoading", this, "OnUifAdUnLoading");
				}

				if (args.PluginType.ToString().Contains("AdaptiveEdge")) {
					ReflectionHelper.SetValue(args.Plugin, "SetInitialBitrate", new object[] { MediaStreamType.Video, 800*1024 });
				}

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Couldn't attach to adload event", ex);
			}
		}

		public void OnUifAdLoading(object sender, RoutedEventArgs args) {
			string url = ReflectionHelper.GetValue(args, "Ad.Args.Uri") as string;
			string type = ReflectionHelper.GetValue(args, "Ad.Tag.type") as string;
			if (type == null || url == null) return;
			if (!ads.ContainsKey(url)) ads.Add(url, args); else ads[url] = args;
			type = type.ToLower();

			//This is a dirty dirty hack, not sure how else to handle this with UIF
			if (type.Contains("bug")) {
				FrameworkElement fe = ReflectionHelper.GetValue(args, "Ad.Args.UIElementToReturn.VideoContainer") as FrameworkElement;
				if (fe != null) {
					try {
						if (fe.Parent is Panel) (fe.Parent as Panel).Children.Remove(fe);

						Player.LayoutRoot.Children.Add(fe);

						fe.VerticalAlignment = VerticalAlignment.Stretch;
						fe.HorizontalAlignment = HorizontalAlignment.Stretch;
						fe.Height = fe.Width = double.NaN;
						fe.Margin = new Thickness(0);
						
					} catch (Exception ex) {
						log.Output(OutputType.Debug, "error placing bug output", ex);
					}
				}
			}

			//force change of ad banners at bottom
			HtmlElement e = (type.Contains("olay"))
				? HtmlPage.Document.GetElementById("olay") : (type.Contains("tres"))
				? HtmlPage.Document.GetElementById("tres") : (type.Contains("cont"))
				? HtmlPage.Document.GetElementById("cont") : null;

			if (e != null) {
				e.SetStyleAttribute("margin-top", "9px");
				e.SetProperty("src", "content/uif/" + e.Id + ".png");
			}

			PositionAdCanvas();
		}

		public void OnUifAdUnLoading(object sender, RoutedEventArgs args) {
			string url = ReflectionHelper.GetValue(args, "Ad.Args.Uri") as string;
			string type = ReflectionHelper.GetValue(args, "Ad.Tag.type") as string;
			if (type == null || url == null) return;
			if (!ads.ContainsKey(url)) ads.Remove(url);
			type = type.ToLower();

			if (type.Contains("bug")) {
				FrameworkElement fe = ReflectionHelper.GetValue(args, "Ad.Args.UIElementToReturn.VideoContainer") as FrameworkElement;
				if (fe != null) if (fe.Parent is Panel) (fe.Parent as Panel).Children.Remove(fe);
			}

			//force change of ad banners at bottom
			HtmlElement e = (type.Contains("olay")) ? HtmlPage.Document.GetElementById("olay") : (type.Contains("tres")) ? HtmlPage.Document.GetElementById("tres") : (type.Contains("cont")) ? HtmlPage.Document.GetElementById("cont") : null;
			if (e != null) {
				e.SetStyleAttribute("margin-top", "13px");
				e.SetProperty("src", "content/uif/" + e.Id + "18.png");
			}
		}

		protected void PositionAdCanvas() {
		    Async.UI(internalPositionAdCanvas, this, true);
		}

		private void internalPositionAdCanvas() {
			try {
				if (Player.CurrentItem == null || !images.ContainsKey(Player.CurrentItem.Title)) return;
				double x = 0, y = 0, w = 0, h = 0;
				double iMargin = 9;
				w = pliWidth;
				y = Player.ActualHeight - 3;

				x = -(((MatrixTransform)Player.LayoutRoot.TransformToVisual(images[Player.CurrentItem.Title])).Matrix.OffsetX) - 6;
				if (x < w || x > Player.ActualWidth - w) {
					foreach (VideoItem vi in Player.Playlist) {
						if (!images.ContainsKey(vi.Title)) continue;
						x = -(((MatrixTransform)Player.LayoutRoot.TransformToVisual(images[vi.Title])).Matrix.OffsetX) - 6;
						if (x >= w) break;
					}
				}
				h = listBoxPlaylist.ActualHeight;

				if (BugContainer.Visibility == Visibility.Visible) {
					BugContainer.SetValue(Canvas.LeftProperty,x);
					BugContainer.SetValue(Canvas.TopProperty, y);
					BugContainer.Width = w;
					BugContainer.Height = h;
				}

				MatrixTransform m = (MatrixTransform)Player.LayoutRoot.TransformToVisual(Player.MediaElement);
				x = -m.Matrix.OffsetX;
				y = -m.Matrix.OffsetY;

				if (BannerContainer.Visibility == Visibility.Visible) {
					BannerContainer.Width = Player.MediaElement.ActualWidth;
					BannerContainer.Height = 74; //hack
					BannerContainer.SetValue(Canvas.LeftProperty, x);
					BannerContainer.SetValue(Canvas.TopProperty, y + Player.MediaElement.ActualHeight - BannerContainer.ActualHeight);
				}

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Couldn't set ad position", ex);
			}
		}

		private void HideBugs() {
			foreach (object o in ads.Values) {
				string type = ReflectionHelper.GetValue(o, "Ad.Tag.type") as string;
				if (type.ToLower().Contains("bug")) {
					FrameworkElement fe = ReflectionHelper.GetValue(o, "Ad.Args.UIElementToReturn") as FrameworkElement;
					fe.Visibility = Visibility.Collapsed;
				}
			}
		}
		#endregion
	}
}
