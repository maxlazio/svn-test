using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Media;
using org.OpenVideoPlayer.Connections;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Player;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// A special parser class used to parse any optional initialization values into the
	/// OpenVideoPlayer Control.
	/// </summary>
	public class PlayerInitParameterParser {
		protected ParserManager parserMgr;
		protected IConnection connection;
		protected Uri link;

		public PlayerInitParameterParser() {

		}

		public void ImportInitParams(StartupEventArgs e, OpenVideoPlayerControl player) {
			String initValue;
			player.Playlist.Clear();
			if (e.InitParams.TryGetValue("playlist", out initValue)) {
				//load our local parser manager
				parserMgr = getParserManager();
				connection = new DefaultConnection(parserMgr);

				// If we find a non-source (direct playlist) parameter then we can
				//parse the xml directly through the connection object.  The playlist
				//can be a lot of things, including a ms-playlist, rss feed, asx file, etc...
				try {
					Stream inStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(initValue));
					connection.ParseStream(HtmlPage.Document.DocumentUri, inStream);
					for (int i = 0; i < connection.Playlist.Count; i++) {
						player.Playlist.Add(connection.Playlist[i]);
					}
				} catch (System.Xml.XmlException xe) {
					Debug.WriteLine("Playlist Parsing Error:" + xe);
				} catch (NullReferenceException) {
				}

			} else if (e.InitParams.TryGetValue("mediasource", out initValue)) {
				//direct-link create a new media item here
				player.Playlist.Add(new VideoItem() { Url = initValue });
			} else {
				if (e.InitParams.TryGetValue("playlistsource", out initValue)) {
					parserMgr = getParserManager();
				} else if (e.InitParams.TryGetValue("feedsource", out initValue)) {
					parserMgr = getFeedParserManager();
				} else if (e.InitParams.TryGetValue("refsource", out initValue)) {
					parserMgr = getRefParserManager();
				}
				if (parserMgr != null) {
					connection = new DefaultConnection(parserMgr);
					// If we find a refsource parameter then we need to setup a factory with only reference file parsers
					try {
						//connect to remote uri turn the data to a memory stream and pass the stream in here
						WebClient wc = new WebClient();
						wc.DownloadStringCompleted += wc_DownloadStringCompleted;

						link = (Uri.IsWellFormedUriString(initValue, UriKind.Absolute))
							? new Uri(initValue)
							: new Uri(HtmlPage.Document.DocumentUri, initValue);

						wc.DownloadStringAsync(link, player);

					} catch (System.Xml.XmlException xe) {
						Debug.WriteLine("XML Parsing Error:" + xe);
					} catch (NullReferenceException) {
					}
				}
			}

			if (e.InitParams.TryGetValue("autoplay", out initValue)) {
				player.AutoPlay = (initValue == "1" || initValue.ToUpper() == "TRUE");
			}

			if (e.InitParams.TryGetValue("muted", out initValue)) {
				player.Muted = (initValue == "1" || initValue.ToUpper() == "TRUE");
			}

			if (e.InitParams.TryGetValue("stretchmode", out initValue)) {
				player.StretchMode = ParseStretchMode(initValue);
			}

			if (e.InitParams.TryGetValue("background", out initValue)) {
				player.BackgroundColor = ParseBGColor(initValue);
			}

			if (e.InitParams.TryGetValue("showstats", out initValue)) {
				player.DebugVisibility = (initValue == "1" || initValue.ToUpper() == "TRUE") ? Visibility.Visible : Visibility.Collapsed;
			}

			if (e.InitParams.TryGetValue("playlistoverlay", out initValue)) {
				player.PlayListOverlay = (initValue == "1" || initValue.ToUpper() == "TRUE");
			}

			if (e.InitParams.TryGetValue("showplaylist", out initValue)) {
				player.ShowPlaylist = (initValue == "1" || initValue.ToUpper() == "TRUE");
			}

			if (e.InitParams.TryGetValue("showchapters", out initValue)) {
				player.ShowChapters = (initValue == "1" || initValue.ToUpper() == "TRUE");
			}

			if (e.InitParams.TryGetValue("linkurl", out initValue)) {
				player.LinkUrl = initValue;
			}

			if (e.InitParams.TryGetValue("embedurl", out initValue)) {
				player.EmbedUrl = initValue;
			}
		}

		//TODO - integrate with conn. class
		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			OpenVideoPlayerControl player = e.UserState as OpenVideoPlayerControl;
			if (e.Cancelled) {
				throw new Exception("Playlist load cancelled");
			}
			if (e.Error != null) {
				throw e.Error;
			}
			if (e.Result == null) {
				throw new Exception("Invalid result from playlist request");
			}
			if (player == null) {
				throw new Exception("Null player on download result");
			}

			Stream inStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result));
			connection.ParseStream(link, inStream);

			if (connection.Playlist != null) {
				for (int i = 0; i < connection.Playlist.Count; i++) {
					player.Playlist.Add(connection.Playlist[i]);
				}
			} else {
				//TODO - get error message into player
			}

			player.StartAutoPlay();//if(player.Playlist.Count>0  && player.AutoPlay) player.SeekToNextItem();
		}

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
			//parserFactories.Add(new MSPlaylistFactory());
			parserFactories.Add(new RssFactory());
			//parserFactories.Add(new BossFactory());

			//load the parsers we've selected into the parser manager and return it
			pm.LoadParsers(parserFactories.ToArray());
			return pm;
		}

		protected ParserManager getRefParserManager() {
			ParserManager pm = new ParserManager();
			List<IPlaylistParserFactory> parserFactories = new List<IPlaylistParserFactory>();

			//Add expected parser factories here
			//parserFactories.Add(new MSPlaylistFactory());
			//parserFactories.Add(new RssFactory());
			parserFactories.Add(new BossFactory());

			//load the parsers we've selected into the parser manager and return it
			pm.LoadParsers(parserFactories.ToArray());
			return pm;
		}
	}
}
