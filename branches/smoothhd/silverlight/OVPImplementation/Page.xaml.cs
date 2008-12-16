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

namespace OVPImplementation {
	public partial class Page : UserControl {

		private PlaylistCollection groups = new PlaylistCollection();
		private List<PlaylistCollection> playlists = new List<PlaylistCollection>();
		bool lastFSMode;
		bool needAutoPlay = true;
		Image lastImage = null;
		string lastSource = null;
		int element = 0;
		Dictionary<string, Image> images = new Dictionary<string, Image>();
		private Size currentPlayerSize = new Size(848 + 16, 480 + 200);
		Size plMargin = new Size(16, 200);
		Size bMargin = new Size(60, 80);

		Stretch lastStretchMode;

		public Page(object sender, StartupEventArgs e) {
			InitializeComponent();
			Player.FullScreenChanged += new RoutedEventHandler(Player_FullScreenChanged);
			Player.ItemChanged += new RoutedEventHandler(Player_ItemChanged);
			Player.BrowserSizeChanged += new EventHandler(Player_BrowserSizeChanged);
			Player.BindingValidationError += new EventHandler<ValidationErrorEventArgs>(Player_BindingValidationError);
			Player.AdaptiveBitrateChanged += new EventHandler(Player_AdaptiveBitrateChanged);
			Player.OnStartup(sender, e);

			Application.Current.Host.Content.Resized += new EventHandler(Content_Resized);

			WebClient wc = new WebClient();
			wc.DownloadStringCompleted += wc_DownloadStringCompleted;
			string val;

			//TODO - fold into param parser
			if(e.InitParams.TryGetValue("masterplaylist",out val)) {
				Uri uri = (Uri.IsWellFormedUriString(val, UriKind.Absolute))? new Uri(val): new Uri(HtmlPage.Document.DocumentUri, val);
				wc.DownloadStringAsync(uri);
			}

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

		void Player_BindingValidationError(object sender, ValidationErrorEventArgs e) {
			Debug.WriteLine("Error binding: " + e.Error.Exception);
		}

		void Player_BrowserSizeChanged(object sender, EventArgs e) {
			AdjustPlayerSize(ChangeType.Size);
		}

		void Player_AdaptiveBitrateChanged(object sender, EventArgs e) {
			AdjustPlayerSize(ChangeType.Bitrate);
		}

		enum ChangeType { Bitrate, Size };

		void AdjustPlayerSize(ChangeType type) {
			if (Player.AdaptiveCurrentBitrate <= 0 || Player.AdaptiveSegments == null) return;

			for (int x = Player.AdaptiveSegments.Length-1; x >= 0; x--) {
				AdaptiveSegment seg = Player.AdaptiveSegments[x];
				//get new size of player and browser for this stage
				Size plSize = new Size(seg.Resolution.Width + plMargin.Width, seg.Resolution.Height + plMargin.Height);
				Size bSize = new Size(plSize.Width + bMargin.Width, plSize.Height + bMargin.Height);
				
				//only consider HD resolutions, otherwise we force 848x480 for this site
				if (seg.Resolution.Height > 480) {
					//extra needed if sizing upward, to keep from awkward middle spots and flashing between sizes
					int extra = (plSize.Width > currentPlayerSize.Width) ? 20 : 0;
					//compare with browser + margin
					if (Player.BrowserSize.Width < bSize.Width + extra || Player.BrowserSize.Height < bSize.Height + extra) continue;
					//make sure we've reached the bitrate below this level
					if (x > 0 && Player.AdaptiveCurrentBitrate < Player.AdaptiveSegments[x - 1].Bitrate) continue;
					//if it drops through then we need to set our minimum
				}

				//make sure we aren't setting the same value again
				if (plSize == currentPlayerSize) return;
				//only size upward from bitrate changes, not down.
				if (type == ChangeType.Bitrate && plSize.Width < currentPlayerSize.Width) return;

				// :: we are officially going to change something ::
				Debug.WriteLine("Setting size to: " + plSize + " from " + currentPlayerSize + ", Browser is: " + Player.BrowserSize);
				currentPlayerSize = plSize;

				Dispatcher.BeginInvoke(delegate {
					//we're golden, set player size
					HtmlPage.Plugin.SetStyleAttribute("width", plSize.Width + "px");
					HtmlPage.Plugin.SetStyleAttribute("height", plSize.Height + "px");
					//set SL container DIV height
					HtmlPage.Plugin.Parent.SetStyleAttribute("height", plSize.Height + "px");
					//and it's container div width (for proper centering)
					HtmlPage.Plugin.Parent.Parent.SetStyleAttribute("width", bSize.Width + "px");
					HtmlPage.Plugin.Parent.Parent.SetStyleAttribute("height", bSize.Height + "px");

					//go native if we match our resolution
					if (Player.StretchMode != Stretch.None && plSize.Width > Player.AdaptiveSegments[Player.AdaptiveSegments.Length - 1].Resolution.Width) {
						lastStretchMode = Player.StretchMode;
						Player.StretchMode = Stretch.None;
						Debug.WriteLine("Switched mode to native");
					} else if(Player.StretchMode == Stretch.None){
						Player.StretchMode = lastStretchMode;
						Debug.WriteLine("Switched mode from native to " + Player.StretchMode);
					}
				});
				return;
			}
		}

		void Player_FullScreenChanged(object sender, RoutedEventArgs e) {
			if (lastFSMode == Application.Current.Host.Content.IsFullScreen) return;
			lastFSMode = Application.Current.Host.Content.IsFullScreen;
			if (Application.Current.Host.Content.IsFullScreen) {
				playlistStackPanel.Visibility = Visibility.Collapsed;
				Player.SetValue(Grid.RowSpanProperty, 2);
				if (Player.StretchMode == Stretch.None && Player.StretchMode != lastStretchMode) {
					Player.StretchMode = lastStretchMode;
					lastStretchMode = Stretch.None;
					Debug.WriteLine("Switched mode from native to " + Player.StretchMode + " for FS");
				}
			} else {
				playlistStackPanel.Visibility = Visibility.Visible;
				Player.SetValue(Grid.RowSpanProperty, 1);
				if (Player.StretchMode != Stretch.None && lastStretchMode == Stretch.None ) {
					lastStretchMode = Player.StretchMode;
					Player.StretchMode = Stretch.None;
					Debug.WriteLine("Switched mode back to native");
				}
			}
		}

		void Content_Resized(object sender, EventArgs e) {
			//if (Player.ActualHeight < 1) return;
			//double h = Player.ActualHeight + 148;
			//HtmlPage.Plugin.SetStyleAttribute("height", h.ToString()+"px");
		}

		void Player_ItemChanged(object sender, RoutedEventArgs e) {
			if (Player.CurrentlyPlayingItem >= Player.Playlist.Count) {
				needAutoPlay = true;
				if (groupPlaylist.SelectedIndex < groupPlaylist.Items.Count - 1) {
					groupPlaylist.SelectedIndex++;
				} else {
					groupPlaylist.SelectedIndex = 0;
				}
			} else {
				listBoxPlaylist.SelectedIndex = Player.CurrentlyPlayingItem;
			}
		}

		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if(e.Error!=null) {
				Debug.WriteLine("Error: " + e.Error);
				return;
			}

			string[] feeds = e.Result.Split('\n');
			foreach (string feed in feeds) {
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
			Debug.WriteLine(string.Format("Completed Playlist: {0}, {1}, {2} items", playlists.IndexOf(pl), pl.Title, pl.Count));

			int x; for (x = 0; x < playlists.Count; x++) if (pl == playlists[x]) break;
			
			groups[x] = new VideoItem { Author = pl.Author, Title = pl.Title, Thumbnails = new List<Thumbnail> { 
				new Thumbnail(pl.ImageURL +((pl == playlists[0])?"_1.jpg" : "_3.jpg")) 
			}, Url = pl.SourceURI };

			if(pl == playlists[0]) {
				groupPlaylist.SelectedIndex = 0;
				playlistStackPanel.Opacity = 1;
				//Player.StartAutoPlay();
			}
		}

		void pli_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			Image i = ((Image)sender);
			PlaylistCollection pl = null;
			foreach (PlaylistCollection p in playlists) if (p.Title == i.Tag.ToString()) pl = p;
			if (pl == Player.Playlist) return;
			i.Source = new BitmapImage(new Uri(pl.ImageURL + "_3.jpg"));
		}

		void pli_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
			Image i = ((Image)sender);
			PlaylistCollection pl = null;
			foreach (PlaylistCollection p in playlists) if (p.Title == i.Tag.ToString()) pl = p;
			if (pl == Player.Playlist) return;
			i.Source = new BitmapImage(new Uri(pl.ImageURL + "_2.jpg"));
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e) {
			string url = ((System.Windows.Media.Imaging.BitmapImage)((Image)sender).Source).UriSource.ToString();
			if (url.Contains("_1.jpg")) {
				lastSource = url.Substring(0, url.Length - 6);
				lastImage = sender as Image;
			}
		}

		void groupPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
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

			if (needAutoPlay) {
				needAutoPlay = false;
				listBoxPlaylist.SelectedIndex = 0;
			} else {
				if (pl.Count>Player.CurrentlyPlayingItem && Player.CurrentSource == pl[Player.CurrentlyPlayingItem].Url) {
					listBoxPlaylist.SelectedIndex = Player.CurrentlyPlayingItem;
				}
			}

			foreach (string u in images.Keys) {
				if (u.StartsWith(vi.ThumbSource)) {
					Image i = images[u];
					string s = vi.ThumbSource.Substring(0, vi.ThumbSource.Length - 6);
					i.Source = new BitmapImage(new Uri(s + "_1.jpg"));
					if (lastImage != null) lastImage.Source = new BitmapImage(new Uri(lastSource + "_3.jpg"));
					lastImage = i;
					lastSource = s;
					break;
				}
			}
		}

		void listBoxPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (listBoxPlaylist.SelectedIndex >=0 && (listBoxPlaylist.SelectedIndex != Player.CurrentlyPlayingItem
				|| ((VideoItem)listBoxPlaylist.SelectedItem).Url != Player.CurrentSource)) {
				Player.SeekToPlaylistItem(listBoxPlaylist.SelectedIndex);
			}
		}

		private void DetailsButtonPress(object sender, RoutedEventArgs e) {
			//object o = HtmlPage.Plugin.GetAttribute("width");
			//object o = HtmlPage.Document.Body.GetProperty("width");

			//HtmlPage.Plugin.SetStyleAttribute("width", "1060px");
			//HtmlPage.Plugin.SetStyleAttribute("height", "782px");
			//HtmlPage.Plugin.SetStyleAttribute("height", Application.Current.Host.Content.ActualWidth + 40 + "px");
			
			ToggleDetails(sender, true);

			//Note - using this event handler as a testing dumping ground

			//WebClient wc1 = new WebClient();
			//wc1.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc3_DownloadProgressChanged);
			//wc1.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc1_DownloadStringCompleted);
			//Uri uri1 = new Uri("http://test1:1285/3.wmv");
			//wc1.DownloadStringAsync(uri1, uri1);

			//WebClient wc2 = new WebClient();
			//wc2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc3_DownloadProgressChanged);
			//wc2.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc1_DownloadStringCompleted);
			////Uri uri2 = new Uri(HtmlPage.Document.DocumentUri, "2.wmv");
			//Uri uri2 = new Uri("http://test1:1285/2.wmv");
			//wc2.DownloadStringAsync(uri2, uri2);

			//WebClient wc3 = new WebClient();
			//wc3.DownloadProgressChanged += new DownloadProgressChangedEventHandler(wc3_DownloadProgressChanged);
			//wc3.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc1_DownloadStringCompleted);
			////Uri uri3 = new Uri(HtmlPage.Document.DocumentUri, "1.wmv");
			//Uri uri3 = new Uri("http://test1:1285/3.wmv");
			//wc3.DownloadStringAsync(uri3, uri3);
		}

		void wc3_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
			Debug.WriteLine("Progress:: " + sender.GetHashCode() + ", " + e.ProgressPercentage + " " + ((Uri)e.UserState).ToString());
			//System.Net.Sockets.Socket 
		}

		void wc1_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			Debug.WriteLine("Complete:: " + sender.GetHashCode() );
			//	throw new NotImplementedException();
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

		private void scrollDown_Click(object sender, RoutedEventArgs e) {
			element += 3;
			if (element > groupPlaylist.Items.Count-1){
				element = groupPlaylist.Items.Count - 1;
				scrollDown.IsEnabled = false;
			} else {
				scrollUp.IsEnabled = true;
			}
			groupPlaylist.ScrollIntoView(groups[element]);
		}

		private void scrollUp_Click(object sender, RoutedEventArgs e) {
			element -= 3;
			if (element < 0) {
				element = 0;
				scrollUp.IsEnabled = false;
			} else {
				scrollDown.IsEnabled = true;
			}
			groupPlaylist.ScrollIntoView(groups[element]);
		}

		private void Image_Loaded(object sender, RoutedEventArgs e) {
			string url = ((System.Windows.Media.Imaging.BitmapImage)((Image)sender).Source).UriSource.ToString();
			if(!images.ContainsKey(url))images.Add(url, sender as Image);
		}
	}
}
