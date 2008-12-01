using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
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
		//protected ParserManager parserMgr;

		protected IConnection connection;
		protected OpenVideoPlayerControl _player;
		protected OutputLog log = new OutputLog("InitParser");

		public PlayerInitParameterParser() { }

		public void ImportInitParams(StartupEventArgs e, OpenVideoPlayerControl player) {
			String initValue;
			_player = player;
			player.Playlist.Clear();

			//parse the source/input - can be inseveral different params
			ParseSource(player, e);

			if (e.InitParams.TryGetValue("theme", out initValue)) {
				if (!initValue.Contains(";component/")) {
					initValue = "OpenVideoPlayer;component/themes/" + initValue + (initValue.Contains(".xaml") ? "" : ".xaml");
				}
				Uri uri = new Uri(initValue, UriKind.Relative); //@"SLLib;component/themes/default.xaml"
				player.SetTheme(uri);
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
				player.StatVisibility = (initValue == "1" || initValue.ToUpper() == "TRUE") ? Visibility.Visible : Visibility.Collapsed;
			}

			if (e.InitParams.TryGetValue("showlogs", out initValue)) {
				player.LogVisibility = (initValue == "1" || initValue.ToUpper() == "TRUE") ? Visibility.Visible : Visibility.Collapsed;
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
		private void ParseSource(OpenVideoPlayerControl player, StartupEventArgs e) {
			string initValue;
			// ** Playlist handing ***
			ParserManager parserMgr = null;
			Stream sourceStream = null;
			Uri sourceUri = null;

			if (e.InitParams.TryGetValue("playlist", out initValue)) {

				//load our local parser manager
				parserMgr = getParserManager();
				// If we find a non-source (direct playlist) parameter then we can
				//parse the xml directly through the connection object.  The playlist
				//can be a lot of things, including a ms-playlist, rss feed, asx file, etc...

				//For now put in stream for later..
				sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(initValue));

			} else if (e.InitParams.TryGetValue("mediasource", out initValue)) {
				//direct-link create a new media item here
				player.Playlist.Add(new VideoItem() { Url = initValue });

			} else {
				//otherise - create the right parser manager for our connection..
				if (e.InitParams.TryGetValue("playlistsource", out initValue)) {
					parserMgr = getParserManager();

				} else if (e.InitParams.TryGetValue("feedsource", out initValue)) {
					parserMgr = getFeedParserManager();

					// If we find a refsource parameter then we need to setup a factory with only reference file parsers
				} else if (e.InitParams.TryGetValue("refsource", out initValue)) {
					parserMgr = getRefParserManager();
				}
				//create a uri to use below.
				sourceUri = (Uri.IsWellFormedUriString(initValue, UriKind.Absolute))
								? new Uri(initValue)
								: new Uri(HtmlPage.Document.DocumentUri, initValue);
			}

			//if we ended up with a parser manager then give it to our connection class
			if (parserMgr != null) {
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

		void OnConnection_Loaded(object sender, EventArgs e) {
			if (_player == null) {
				log.Output(OutputType.Critical, "Error: received source input, but no player to use");
				return;
			}

			if (connection.Playlist != null) {
				for (int i = 0; i < connection.Playlist.Count; i++) {
					_player.Playlist.Add(connection.Playlist[i]);
				}
			} else {
				log.Output(OutputType.Critical, "Could not access playlist - no items.");
			}

			_player.StartAutoPlay();
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
