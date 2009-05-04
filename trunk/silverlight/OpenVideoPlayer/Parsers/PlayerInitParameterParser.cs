using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Media;
using org.OpenVideoPlayer;
using org.OpenVideoPlayer.Connections;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Controls;
using org.OpenVideoPlayer.Util;
using System.Net;
using System.Windows.Markup;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// A special parser class used to parse any optional initialization values into the
	/// OpenVideoPlayer Control.
	/// </summary>
	public class PlayerInitParameterParser {

		protected IConnection connection;
		protected OpenVideoPlayerControl _player;
		protected OutputLog log = new OutputLog("InitParser");

		public PlayerInitParameterParser() { }

		/// <summary>
		/// Use to parse the initparams and set appropriate values
		/// </summary>
		/// <param name="e">Start up arguments</param>
		/// <param name="player">the player to set values to</param>
		public void ImportInitParams(StartupEventArgs e, OpenVideoPlayerControl player) {
			StringBuilder initstring =new StringBuilder();
			foreach (string param in e.InitParams.Keys) {
				initstring.AppendFormat("{0}={1}\n", param, e.InitParams[param]);
			}

			log.Output(OutputType.Debug, "Loading Initial Parameters", initstring.ToString());

			foreach (string param in e.InitParams.Keys) {
				string key = param.ToLower();
				ParseParameter(key, e.InitParams[param], player);
			}
		}

		public void RetryUnmatched(OpenVideoPlayerControl player) {
			List<string> found = new List<string>();
			foreach (string param in Unmatched.Keys) {
				string key = param.ToLower();
				if (ParseParameter(key, Unmatched[param], player)) {
					found.Combine(key);
				}
			}
			foreach (string key in found) Unmatched.Remove(key);
		}

		/// <summary>
		/// Use to parse an individual value
		/// </summary>
		/// <param name="key">the key of the param</param>
		/// <param name="initValue">the value of the param</param>
		/// <param name="player">a player, to set values to</param>
		public bool ParseParameter(string key, string initValue, OpenVideoPlayerControl player) {
			try {
				switch (key) {
					case "theme":
						_player = player;
						if (!initValue.Contains(";component/")) {
							WebClient wwc = new WebClient();
							Uri exuri = (Uri.IsWellFormedUriString(initValue, UriKind.Absolute)) ? new Uri(initValue) : new Uri(HtmlPage.Document.DocumentUri, initValue);
							wwc.OpenReadCompleted += new OpenReadCompletedEventHandler(Theme_DownloadCompleted);
							wwc.OpenReadAsync(exuri, exuri);
							player.Visibility = Visibility.Collapsed;
							return true;
						}
						Uri uri = new Uri(initValue, UriKind.Relative); //@"SLLib;component/themes/default.xaml"
						try {
							//player.ApplyTheme(uri, true);
							ControlBase.ApplyThemeToElement(player.LayoutRoot, uri, true);
							ControlBase.ApplyThemeToElement(player.Parent as FrameworkElement, uri, true);
						} catch (Exception ex) {
							log.Output(OutputType.Error, "Can't apply template: ", ex.Message);
						}
						break;

						//TODO - make proper properties for these
					//case "statsopacity":
					//	((FrameworkElement)player.stats.Parent).Opacity = Convert.ToDouble(initValue);
					//	break;

					case "logsopacity":
						((FrameworkElement)player.LogViewer.Parent).Opacity = Convert.ToDouble(initValue);
						break;

					case "source":
					case "playlist":
					case "mediasource":
					case "playlistsource":
					case "feedsource":
					case "refsource":
						ParseSource(player.Playlist, key, initValue);
						break;

					case "plugins":
						PluginManager.LoadPlugins(initValue);
						break;

					case "type":
						//this is handled by the application.  
						break;

					default:
						//look for property on player
						if (ReflectionHelper.SetValue(player, key, initValue)) break;

						//look for property in plugins
						bool match = false;
						foreach (IPlugin ip in player.Plugins) {
							if (match = (key.StartsWith(ip.PlugInName.ToLower()) || key.StartsWith(ip.GetType().Name.ToLower()) || key.StartsWith(ip.GetType().FullName.ToLower()))) {
								if(!ReflectionHelper.SetValue(ip.GetType(), key.Substring(key.IndexOf('.') + 1), initValue)) {
									ReflectionHelper.SetValue(ip, key.Substring(key.IndexOf('.') + 1), initValue);
								}
								break;
							}
						}
						if (match) break;

						//log 'misses'
						if (!key.Contains(":")) {
							log.Output(OutputType.Debug, "Unknown Parameter: " + key);
							if (!Unmatched.ContainsKey(key)) Unmatched.Add(key, initValue);
							return false;
						}

						break;
				}
				return true;

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error parsing parameter: " + key, ex);
				return false;
			}
		}

		public Dictionary<string, string> Unmatched = new Dictionary<string, string>();

		void Theme_DownloadCompleted(object sender, OpenReadCompletedEventArgs e) {
			try {
				Stream ss = e.Result;
				//we need to check for extra chars at beginning - seems to happen sometimes with WebClient and xaml
				byte b = 0;
				while ((b = (byte)ss.ReadByte()) != (byte)'<') ;
				if (ss.Position >= ss.Length - 4) throw new Exception("invalid xaml");
				byte[] ba = new byte[ss.Length - ss.Position + 1];
				ba[0] = b;
				ss.Read(ba, 1, ba.Length);
				MemoryStream s = new MemoryStream(ba);
				//string test = System.Text.Encoding.UTF8.GetString(b, 0, b.Length);

				log.Output(OutputType.Info, "Downloaded theme: " + e.UserState);
				

				ControlBase.ApplyThemeToElement(_player.LayoutRoot, s, true);
				ControlBase.ApplyThemeToElement(_player.Parent as FrameworkElement, s, true);

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Can't apply template: ", ex.Message);
			} finally {
				_player.Visibility = Visibility.Visible;
			}
		}


		#region Source/Playlist specific

		private PlaylistCollection _playlist;

		/// <summary>
		/// specifically parse a parameter pertaining to a media source
		/// </summary>
		/// <param name="playlist">The playlist to poulate</param>
		/// <param name="key">param key</param>
		/// <param name="initValue">the param value</param>
		public void ParseSource(PlaylistCollection playlist, string key, string initValue) {
			//string initValue;
			// ** Playlist handing ***
			ParserManager parserMgr = null;
			Stream sourceStream = null;
			Uri sourceUri = null;

			if (key == "source") {
				if (initValue.Contains(".ism") ||
				    initValue.Contains(".wmv") ||
				    initValue.Contains(".wma") ||
				    initValue.Contains("mms") ||
				    initValue.Contains(".mp3") //todo - make sure we've coverred everything
					) {
					key = "mediasource";
				}else if (initValue.Trim().StartsWith("<")) {
					key = "playlist";
				}else {
					key = "playlistsource";
				}
			}
		

			if (key=="playlist") {
				//load our local parser manager
				parserMgr = getParserManager();
				// If we find a non-source (direct playlist) parameter then we can
				//parse the xml directly through the connection object.  The playlist
				//can be a lot of things, including a ms-playlist, rss feed, asx file, etc...

				//For now put in stream for later..
				sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(initValue));

			} else if (key=="mediasource") {
				//direct-link create a new media item here
				VideoItem vi = new VideoItem();
				if (initValue.Contains(".ism")) {
					vi.DeliveryType = DeliveryTypes.Adaptive;
					if (!initValue.ToLower().Contains("/manifest") && !initValue.ToLower().Contains(".ismc")) {
						initValue += "/manifest";
					}
				}
				vi.Url = initValue;
				playlist.Add(vi);
				playlist.LoadCompleted();

			} else {
				//otherise - create the right parser manager for our connection..
				if (key=="playlistsource") {
					parserMgr = getParserManager();

				} else if (key=="feedsource") {
					parserMgr = getFeedParserManager();

					// If we find a refsource parameter then we need to setup a factory with only reference file parsers
				} else if (key == "refsource") {
					parserMgr = getRefParserManager();
				}

				initValue = HttpUtility.UrlDecode(initValue);
				//create a uri to use below.
				sourceUri = (Uri.IsWellFormedUriString(initValue, UriKind.Absolute))
								? new Uri(initValue)
								: new Uri(HtmlPage.Document.DocumentUri, initValue);
			}

			//if we ended up with a parser manager then give it to our connection class
			if (parserMgr != null) {
				_playlist = playlist;

				connection = new DefaultConnection(parserMgr);
				connection.Loaded += OnConnection_Loaded;
				connection.Error += OnConnection_Error;

				if (sourceStream != null) {
					//if the stream is there, parse directly
					connection.ParseStream(HtmlPage.Document.DocumentUri, sourceStream);
				} else {
					//otherwise connect to the uri for the source/playlist
					connection.Connect(sourceUri);
				}
			}
			// ** End Playlist handing ***
		}

		void OnConnection_Error(object sender, UnhandledExceptionEventArgs e) {
			log.Output(OutputType.Error, "Connection error: ", (Exception)e.ExceptionObject);
		}

		DefaultConnection temp;
		private List<string> tryLoad = new List<string>();

		/// <summary>
		/// fires when we receive the content of our playlist/source
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnConnection_Loaded(object sender, EventArgs e) {
			if (_playlist == null) {
				log.Output(OutputType.Critical, "Error: received source input, but no player to use");
				return;
			}

			if (connection.Playlist != null) {
				//log.Output(OutputType.Debug, "Received playlist with " + connection.Playlist.Count + " items");

				for (int i = 0; i < connection.Playlist.Count; i++) {
					//load the non xml items first
					if (!connection.Playlist[i].Url.Contains(".xml") || tryLoad.Contains(connection.Playlist[i].Url)) {
						_playlist.Add(connection.Playlist[i]);
						connection.Playlist.RemoveAt(i);
						i--;
					}
				}

				for (int i = 0; i < connection.Playlist.Count; i++) {
					//HACK - if it is xml, we assume it needs to be further resolved
					if (connection.Playlist[i].Url.Contains(".xml") && !tryLoad.Contains(connection.Playlist[i].Url)) {
						if (temp == null) {
							temp = new DefaultConnection(getParserManager());
							temp.Loaded += temp_Loaded;
							temp.Error += temp_Error;
						}
						//add to a list so we dont try to load it multiple times
						tryLoad.Add(connection.Playlist[i].Url);
						temp.Connect(connection.Playlist[i].Url);
						return;
					}
				}

			}
			_playlist.Author = connection.Playlist.Author;
			_playlist.ImageURL = connection.Playlist.ImageURL;
			_playlist.SourceURI = connection.Playlist.SourceURI;
			_playlist.Title = connection.Playlist.Title;

			//_player.StartAutoPlay();
			_playlist.LoadCompleted();
			//if(!ap) _player.StartAutoPlay();
		}

		void temp_Error(object sender, UnhandledExceptionEventArgs e) {
			log.Output(OutputType.Debug, "Error receiving item: " + e.ExceptionObject);
			OnConnection_Loaded(connection, e);
		}

		/// <summary>
		/// retrieved from our temp connection, used to resolve items that are indirected from the playlist
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void temp_Loaded(object sender, EventArgs e) {
			DefaultConnection conn = (DefaultConnection)sender;
			
			for (int i = 0; i < connection.Playlist.Count; i++) {
			    if (connection.Playlist[i].Url == conn.Uri.ToString()) {
					//log.Output(OutputType.Info, "Loaded referenced playlist: " + conn.Playlist[0].Title);
					connection.Playlist[i].Url = conn.Playlist[0].Url;
					connection.Playlist[i].DeliveryType = conn.Playlist[0].DeliveryType;
			    	conn.Clear();
			    	OnConnection_Loaded(connection, e);
			        return;
			    }
			}
			log.Output(OutputType.Debug, "No Match");
        }

		/// <summary>
		/// Used to encapsulate the creation of the ParserManager.  The ParserManager
		/// is responsible for calling parser factory classes in order, each deciding
		/// if they are capable of handling the response from the connection class.
		/// This method could be overloaded or edited for simple customization of what
		/// types of playlists you want to parse for this player.
		/// </summary>
		/// <returns>the ParerManager containing the classes to use for parsing the response.</returns>
		protected ParserManager getParserManager() {
			ParserManager pm = new ParserManager();
			List<IPlaylistParserFactory> parserFactories = new List<IPlaylistParserFactory>();

			//Add expected parser factories here
			parserFactories.Add(new MSPlaylistFactory());
			parserFactories.Add(new BossFactory());
			parserFactories.Add(new RssFactory());

			//load the parsers we've selected into the parser manager and return it
			pm.LoadParsers(parserFactories.ToArray());
			return pm;
		}

		protected ParserManager getFeedParserManager() {
			ParserManager pm = new ParserManager();
			List<IPlaylistParserFactory> parserFactories = new List<IPlaylistParserFactory>();

			//Add expected parser factories here
			parserFactories.Add(new RssFactory());

			//load the parsers we've selected into the parser manager and return it
			pm.LoadParsers(parserFactories.ToArray());
			return pm;
		}

		protected ParserManager getRefParserManager() {
			ParserManager pm = new ParserManager();
			List<IPlaylistParserFactory> parserFactories = new List<IPlaylistParserFactory>();

			//Add expected parser factories here
			parserFactories.Add(new BossFactory());

			//load the parsers we've selected into the parser manager and return it
			pm.LoadParsers(parserFactories.ToArray());
			return pm;
		}

		#endregion

	}
}
