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

namespace OVPImplementation {
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
		OutputLog log = new OutputLog("smoothhd");
		private Dictionary<string, object> ads = new Dictionary<string, object>();

		//from item templates //todo get dynamically
		private double pliWidth = 209;
		private double gliHeight = 39;

		public SmoothHdPage(object sender, StartupEventArgs e) {
			string val;
			if (e.InitParams.TryGetValue("masterplaylist", out val)) {
				WebClient wc = new WebClient();
				wc.DownloadStringCompleted += wc_DownloadStringCompleted;
				Uri uri = (Uri.IsWellFormedUriString(val, UriKind.Absolute)) ? new Uri(val) : new Uri(HtmlPage.Document.DocumentUri, val);
				wc.DownloadStringAsync(uri);
			}

			InitializeComponent();

			Player.FullScreenChanged += new RoutedEventHandler(Player_FullScreenChanged);
			Player.ItemChanged += new RoutedEventHandler(Player_ItemChanged);
			Player.SizeChanged += new SizeChangedEventHandler(Player_SizeChanged);
			Player.OnStartup(sender, e);

			PluginManager.PluginLoaded += new PluginManager.PluginEventHandler(PluginManager_PluginLoaded);

			ApplyTemplate();
			Player.ApplyTemplate();
			groupPlaylist.ItemsSource = groups;
			groupPlaylist.SelectionChanged += groupPlaylist_SelectionChanged;
			listBoxPlaylist.SelectionChanged += listBoxPlaylist_SelectionChanged;

			//hide the playlist and chapters menuitems
			foreach (MenuItem mi in Player.OptionsMenu.Items) {
				if (mi.Text == "Chapters" || mi.Text == "Playlist") {
					mi.Visibility = Visibility.Collapsed;
				}
			}

		}
		#endregion

		#region Player Events
		void Player_FullScreenChanged(object sender, RoutedEventArgs e) {
			if (lastFSMode == Application.Current.Host.Content.IsFullScreen) return;
			lastFSMode = Application.Current.Host.Content.IsFullScreen;

			if (Application.Current.Host.Content.IsFullScreen) {
				playlistStackPanel.Visibility = Visibility.Collapsed;
				Player.SetValue(Grid.RowSpanProperty, 2);
			} else {
				playlistStackPanel.Visibility = Visibility.Visible;
				Player.SetValue(Grid.RowSpanProperty, 1);
			}
		}

		void Player_ItemChanged(object sender, RoutedEventArgs e) {
			if (Player.CurrentItem >= Player.Playlist.Count) {
				needAutoPlay = true;
				if (groupPlaylist.SelectedIndex < groupPlaylist.Items.Count - 1) {
					groupPlaylist.SelectedIndex++;
				} else {
					groupPlaylist.SelectedIndex = 0;
				}
			} else {
				listBoxPlaylist.SelectedIndex = Player.CurrentItem;
			}
		}

		void Player_SizeChanged(object sender, SizeChangedEventArgs e) {
			foreach (object o in ads.Values) {
				PositionAd(o);
			}
			AdjustPlaylistSize();
		}
		#endregion

		#region Playlist handling and other supporting methods
		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if(e.Error!=null) {
				Debug.WriteLine("Error: " + e.Error);
				return;
			}

			string[] feeds = e.Result.Split('\n');
			List<string> feedList = new List<string>();
			foreach (string f in feeds) {
				if (!string.IsNullOrEmpty(f) && f.Trim().Length > 0 && !f.Trim().StartsWith("#")) {
					feedList.Add(f);
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
				Thumbnails = new List<Thumbnail> { 
					new Thumbnail(pl.ImageURL +((pl == playlists[0])?"_1.jpg" : "_3.jpg")) 
				},
			Url = pl.SourceURI };

			for (int i = 1; i <= 3; i++) {
				string url = pl.ImageURL + "_" + i + ".jpg";
				BitmapImage bi = new BitmapImage(new Uri(url));
				sources.Add(url, bi);
			}

			if(pl == playlists[0]) {
				groupPlaylist.SelectedIndex = 0;
				playlistStackPanel.Opacity = 1;
			}
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
					listBoxPlaylist.SelectedIndex = 0;
				} else {
					if (pl.Count > Player.CurrentItem && Player.CurrentSource == pl[Player.CurrentItem].Url) {
						listBoxPlaylist.SelectedIndex = Player.CurrentItem;
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

				bool rogan = (vi.ThumbSource.ToLower().Contains("rogan"));
				HtmlElement ads1 = HtmlPage.Document.GetElementById("ads1");
				HtmlElement ads2 = HtmlPage.Document.GetElementById("ads2");
				if (ads1 != null) ads1.SetStyleAttribute("visibility", ((rogan) ? "visible" : "hidden"));
				if (ads2 != null) ads2.SetStyleAttribute("visibility", ((rogan) ? "visible" : "hidden"));
			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error on change", ex);
			}
		}

		private void AdjustPlaylistSize() {
			double plWidth = Player.ActualWidth - FullControlSize(groupPlaylist).Width;
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

		private Size FullControlSize(Control c) {
			return new Size(c.ActualWidth + c.Margin.Left + c.Margin.Right, c.ActualHeight + c.Margin.Top + c.Margin.Bottom);
		}

		void listBoxPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (listBoxPlaylist.SelectedIndex >=0 && (listBoxPlaylist.SelectedIndex != Player.CurrentItem
				|| ((VideoItem)listBoxPlaylist.SelectedItem).Url != Player.CurrentSource)) {
				Player.SeekToPlaylistItem(listBoxPlaylist.SelectedIndex);
			}
		}

		private void scrollDown_Click(object sender, RoutedEventArgs e) {
			groupPlaylist.VerticalOffset += gliHeight;
			if (groupPlaylist.VerticalOffset >= groupPlaylist.MaxVerticalOffset - groupPlaylist.ActualHeight - gliHeight - 2) scrollDown.IsEnabled = false;
			scrollUp.IsEnabled = true;
		}

		private void scrollUp_Click(object sender, RoutedEventArgs e) {
			groupPlaylist.VerticalOffset -= gliHeight;
			if (groupPlaylist.VerticalOffset <= gliHeight +2) scrollUp.IsEnabled = false;
			scrollDown.IsEnabled = true;
		}

		private void scrollLeft_Click(object sender, RoutedEventArgs e) {
			listBoxPlaylist.HorizontalOffset -= pliWidth;
			if (listBoxPlaylist.HorizontalOffset <= pliWidth) scrollLeft.IsEnabled = false;
			scrollRight.IsEnabled = true;
		}

		private void scrollRight_Click(object sender, RoutedEventArgs e) {
			listBoxPlaylist.HorizontalOffset += pliWidth;
			if (listBoxPlaylist.HorizontalOffset >= listBoxPlaylist.MaxHorizontalOffset - listBoxPlaylist.ActualWidth - pliWidth - 2) scrollRight.IsEnabled = false;
			scrollLeft.IsEnabled = true;
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
			try {
				if (args.PluginType.ToString().Contains("UIFAdManager")) {
					//hack in to the ad loading event so we can position the bug
					ReflectionHelper.AttachEvent(args.Plugin, "AdLoading", this, "OnUifAdLoading");
					ReflectionHelper.AttachEvent(args.Plugin, "AdUnLoading", this, "OnUifAdUnLoading");
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

			PositionAd(args);

			if (type.Contains("postroll")) {
				groupPlaylist.IsEnabled = false;
				listBoxPlaylist.IsEnabled = false;
			}

			HtmlElement e = (type.Contains("olay"))
				? HtmlPage.Document.GetElementById("olay") : (type.Contains("tres"))
				? HtmlPage.Document.GetElementById("tres") : (type.Contains("cont"))
				? HtmlPage.Document.GetElementById("cont") : null;

			if (e != null) {
				e.SetStyleAttribute("margin-top", "9px");
				e.SetProperty("src", "content/uif/" + e.Id + ".png");
			}
		}

		public void OnUifAdUnLoading(object sender, RoutedEventArgs args) {
			string url = ReflectionHelper.GetValue(args, "Ad.Args.Uri") as string;
			string type = ReflectionHelper.GetValue(args, "Ad.Tag.type") as string;
			if (type == null || url == null) return;
			if (!ads.ContainsKey(url)) ads.Remove(url);
			type = type.ToLower();

			if (type.Contains("postroll")) {
				groupPlaylist.IsEnabled = true;
				listBoxPlaylist.IsEnabled = true;
			}

			HtmlElement e = (type.Contains("olay")) ? HtmlPage.Document.GetElementById("olay") : (type.Contains("tres")) ? HtmlPage.Document.GetElementById("tres") : (type.Contains("cont")) ? HtmlPage.Document.GetElementById("cont") : null;
			if (e != null) {
				e.SetStyleAttribute("margin-top", "13px");
				e.SetProperty("src", "content/uif/" + e.Id + "18.png");
			}
		}

		private void PositionAd(object args) {
			try {
				string type = ReflectionHelper.GetValue(args, "Ad.Tag.type") as string;

				if (type == null) return;
				type = type.ToLower();
				if (type.Contains("bug") || type.Contains("ticker")) {
					Player.SetValue(Canvas.ZIndexProperty, 99);
					double x = 0, y = 0, w = 0, h = 0;

					if (type.ToLower().Contains("bug")) {
						double iMargin = 9;
						w = pliWidth;
						y = Player.ActualHeight - 3;
						x = Math.Abs(((MatrixTransform)Player.LayoutRoot.TransformToVisual(images[Player.Playlist[Player.CurrentItem].Title])).Matrix.OffsetX) -6;
						h = listBoxPlaylist.ActualHeight;

					} else if (type.ToLower().Contains("ticker")) {
						//position along bottom of player.  
						x = 2;
						h = (Double)ReflectionHelper.GetValue(args, "Ad.Args.Height");
						y = Player.ActualHeight - h - 64;
						w = Player.ActualWidth - 16;
					}

					//set arguments
					ReflectionHelper.SetValue(args, "Ad.Args.PositionX", x);
					ReflectionHelper.SetValue(args, "Ad.Args.PositionY", y);
					ReflectionHelper.SetValue(args, "Ad.Args.Width", w);
					ReflectionHelper.SetValue(args, "Ad.Args.Height", h);

					// adjust position when player changes.  Set actual canvas, in case it is already there.
					FrameworkElement fe = ReflectionHelper.GetValue(args, "Ad.Args.UIElementToReturn") as FrameworkElement;
					if (fe != null) {
						fe.SetValue(Canvas.LeftProperty, x);
						fe.SetValue(Canvas.TopProperty, y);
						fe.Width = w;
						fe.Height = h;
					}
				}

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Couldn't set ad position", ex);
			}
		}
		#endregion
	}
}
