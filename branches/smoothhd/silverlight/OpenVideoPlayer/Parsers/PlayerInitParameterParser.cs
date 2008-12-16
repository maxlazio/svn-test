using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Media;
using org.OpenVideoPlayer.Connections;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Controls;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// A special parser class used to parse any optional initialization values into the
	/// OpenVideoPlayer Control.
	/// </summary>
	public class PlayerInitParameterParser {
		//protected ParserManager parserMgr;

		protected IConnection connection;
		protected OpenVideoPlayerControl _player;
		protected OutputLog log = new OutputLog("InitParser");

		public PlayerInitParameterParser() { }

		public void ImportInitParams(StartupEventArgs e, OpenVideoPlayerControl player) {
			//String initValue;
			//_player = player;
			//player.Playlist.Clear();

			foreach (string param in e.InitParams.Keys) {
				ParseParameter(param.ToLower(), e.InitParams[param], player);
			}
		}

		public void ParseParameter(string key, string initValue, OpenVideoPlayerControl player) {
			switch(key) {
				case "theme":
					if (!initValue.Contains(";component/")) {
						initValue = "OpenVideoPlayer;component/themes/" + initValue + (initValue.Contains(".xaml") ? "" : ".xaml");
					}
					try {
						Uri uri = new Uri(initValue, UriKind.Relative); //@"SLLib;component/themes/default.xaml"
						ControlHelper.ApplyTheme(player.LayoutRoot, uri, true);
						ControlHelper.ApplyTheme(player, uri, false);
						ControlHelper.ApplyTheme(player.Parent as FrameworkElement, uri, false);
					} catch (Exception ex) {
						log.Output(OutputType.Error, "Can't apply template: ", ex.Message);
					}
					break;
				case "autoplay":
					player.AutoPlay = (initValue == "1" || initValue.ToUpper() == "TRUE");
					break;
				case "muted":
					player.Muted = ParseBool(initValue);
					break;
				case "stretchmode":
					player.StretchMode = ParseStretchMode(initValue);
					break;
				case "showstats":
					player.StatVisibility = ParseBool(initValue) ? Visibility.Visible : Visibility.Collapsed;
					break;
				case "showlogs":
					player.LogVisibility = ParseBool(initValue) ? Visibility.Visible : Visibility.Collapsed;
					break;
				case "playlistoverlay":
					player.PlayListOverlay = ParseBool(initValue);
					break;
				case "linkurl":
					player.LinkUrl = initValue;
					break;
				case "embedurl":
					player.EmbedUrl = initValue;
					break;
				case "playlist":
				case "mediasource":
				case "playlistsource":
				case "feedsource":
				case "refsource":
					ParseSource(player.Playlist, key, initValue);
					break;
				default:
					log.Output(OutputType.Debug, "Unknown Parameter: " + key);
					break;
			}
		}

		private bool ParseBool(string initValue) {
			return (initValue == "1" || initValue.ToUpper() == "TRUE");
		}

		//parse the source/input - can be inseveral different params
		//	ParseSource(player, e);

		protected Stretch ParseStretchMode(string args) {
			args = args.ToLower();  // force it to-lower for matching
			if (args == "1" || args == "uniform" || args == "fit") {
				return Stretch.Uniform;
			}
			if (args == "2" || args == "uniformtofill" || args == "fill") {
				return Stretch.UniformToFill;
			}
			if (args == "3" || args == "stretch") {
				return Stretch.Fill;
			}

			//nativs
			return Stretch.None;
		}

		protected Color ParseBGColor(string args) {
			try {
				return Conversion.ColorFromString(args);
			} catch (System.FormatException) {
				return Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
			}
		}

		#region Source/Playlist specific

		private PlaylistCollection _playlist;
		public void ParseSource(PlaylistCollection playlist, string key, string initValue) {
			//string initValue;
			// ** Playlist handing ***
			ParserManager parserMgr = null;
			Stream sourceStream = null;
			Uri sourceUri = null;

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
				playlist.Add(new VideoItem() { Url = initValue });

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

		void OnConnection_Loaded(object sender, EventArgs e) {
			if (_playlist == null) {
				log.Output(OutputType.Critical, "Error: received source input, but no player to use");
				return;
			}

			//bool ap = false;

			if (connection.Playlist != null) {
				log.Output(OutputType.Info, "Received playlist with " + connection.Playlist.Count + " items");

				for (int i = 0; i < connection.Playlist.Count; i++) {
					//HACK
					if (connection.Playlist[i].Url.Contains(".xml") && !tryLoad.Contains(connection.Playlist[i].Url)) {
					} else {
						log.Output(OutputType.Info, "Added item " + connection.Playlist[i].Url);
						_playlist.Add(connection.Playlist[i]);
						connection.Playlist.RemoveAt(i);
					}
				}

				for (int i = 0; i < connection.Playlist.Count; i++) {
					//HACK
					if (connection.Playlist[i].Url.Contains(".xml") && !tryLoad.Contains(connection.Playlist[i].Url)) {
						if (temp == null) {
							temp = new DefaultConnection(getParserManager());
							temp.Loaded += temp_Loaded;
							temp.Error += temp_Error;
						}
						log.Output(OutputType.Info, "Resolving xml item " + connection.Playlist[i].Url);
						tryLoad.Add(connection.Playlist[i].Url);
						temp.Connect(connection.Playlist[i].Url);
						return;
					}
				}

				//log.Output(OutputType.Critical, "Could not access playlist - no items.");

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


		void temp_Loaded(object sender, EventArgs e) {
			DefaultConnection conn = (DefaultConnection)sender;
			
			for (int i = 0; i < connection.Playlist.Count; i++) {
			    if (connection.Playlist[i].Url == conn.Uri.ToString()) {
					log.Output(OutputType.Info, "Converted uri " + conn.Playlist[0].Url);
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
			parserFactories.Add(new RssFactory());
			parserFactories.Add(new BossFactory());

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
