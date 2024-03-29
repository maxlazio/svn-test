﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Connections {
	/// <summary>
	/// The DefaultConnection class is a primary IConnection object which is used
	/// to connect to playlists and media.
	/// </summary>
	public class DefaultConnection : IConnection {
		#region Properties
		protected Uri _currentURI;
		protected ParserManager pManager;
		protected PlaylistCollection playlist = new PlaylistCollection();
		protected string error;
		protected bool isConnected;

		/// <summary>
		/// The last error message stored on the connection.  This is cleared on every
		/// operation.
		/// </summary>
		//public string ErrorMsg {
		//    get { return error; }
		//}

		/// <summary>
		/// The uri that was most recently parsed
		/// </summary>
		public Uri Uri {
			get { return _currentURI; }
		}

		/// <summary>
		/// The playlist generated by the parsers
		/// </summary>
		public PlaylistCollection //List<IMediaItem> 
			Playlist {
			get { return playlist; }
		}

		/// <summary>
		/// returns the state of the connection
		/// </summary>
		public bool IsConnected {
			get { return isConnected; }
		}
		#endregion

		#region Event Delegates
		public event ConnectionEventHandler Loaded;
		public event EventHandler<UnhandledExceptionEventArgs> Error;

		protected virtual void OnLoaded(EventArgs e) {
			if (Loaded != null) {
				Loaded(this, e);
			}
		}

		protected virtual void OnError(UnhandledExceptionEventArgs e) {
			if (Error != null) {
				Error(this, e);
			}
		}
		#endregion

		/// <summary>
		/// Initiates a new instance of the IConnection class.
		/// </summary>
		/// <param name="pManager">The parser manager to use on connect attempts</param>
		public DefaultConnection(ParserManager pManager) {
			this.error = null;
			if (pManager == null) {
				throw new NullReferenceException("Parser manaager must not be null");
			}
			this.pManager = pManager;
		}

		/// <summary>
		/// connects to the named uri passed in as a string.
		/// </summary>
		/// <param name="uri">string uri to connect to</param>
		public void Connect(string uri) {
			this.Connect(new Uri(uri));
		}

		/// <summary>
		/// connect to the named uri.  Calling this method initiates the connection to
		/// the uri specified and assigns the results to be parsed by the parser factory
		/// which was given to this object on construction.
		/// </summary>
		/// <param name="uri">The Uri to connect to</param>
		public void Connect(Uri uri) {
			this.error = null;
			//System.Diagnostics.Debug.WriteLine("Loading Url");

			//store out the current uri for use
			_currentURI = uri;

			try {
				WebClient client = new WebClient();
				client.DownloadStringCompleted += connect_DownloadStringCompleted;
				client.DownloadStringAsync(uri);

				//client.Headers[HttpRequestHeader.Host] = "www.test.com";

				isConnected = true;
			} catch (Exception ex) {
				isConnected = false;
				OnError(new UnhandledExceptionEventArgs(new Exception("Error encountered connecting to resource " + uri + ". The message was: " + ex.Message),false ));
			}
		}

		/// <summary>
		/// Directly calls the parsermanager and parsers for a given URI and stream. This is
		/// used both internally and is available externally for callback and direct
		/// parsing support.
		/// </summary>
		/// <param name="uri">The uri that sourced this stream</param>
		/// <param name="streamToParse">The Stream to parse</param>
		public void ParseStream(Uri uri, Stream streamToParse) {
			_currentURI = uri;
			parseStreamNow(streamToParse);
		}

		/// <summary>
		/// This is the internal part of the direct stream parsing
		/// </summary>
		/// <param name="streamToParse">The Stream to parse</param>
		protected void parseStreamNow(Stream streamToParse) {
			IPlaylistParser p = pManager.getPlaylistParser(streamToParse, _currentURI);
			try {
				if (p != null) {
					//reset the stream position to 0 in case the Playlist parser consumed any of it
					streamToParse.Position = 0;
					p.load(streamToParse);

					foreach (IMediaItem im in p.getMediaItemList()) playlist.Add(im);

					playlist.Author = p.Author;
					playlist.ImageURL = p.ImageURL;
					playlist.SourceURI = p.SourceURI;
					playlist.Title = p.Title;

					OnLoaded(EventArgs.Empty);
				} else {
					OnError(new UnhandledExceptionEventArgs(new Exception("No Valid Parsers found for " + _currentURI ?? "(null)"), false));
				}
			}catch(Exception ex) {
				OnError(new UnhandledExceptionEventArgs(new Exception("Error parsing " + _currentURI ?? "(null)", ex), false));
			}
		}

		internal void connect_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) {
			if (e.Cancelled) {
				//log.Output(OutputType.Critical, "Playlist Load cancelled");
				OnError(new UnhandledExceptionEventArgs(new Exception("Playlist loading was cancelled"), false));
				return;
			}
			if (e.Error != null) {
				//throw e.Error;
				OnError(new UnhandledExceptionEventArgs(new Exception("Error connecting to " + _currentURI??"(null)" + " : " + e.Error ), false));
				return;
			}
			if (e.Result == null) {
				OnError(new UnhandledExceptionEventArgs(new Exception("Invalid result from playlist request"), false));
				return;
			}
			
			try {
				OutputLog.StaticOutput("Conn",OutputType.Debug, "Load complete. parsing : " + _currentURI ?? "(null)");
				Stream reader = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
				parseStreamNow(reader);

			} catch (Exception ex) {
				OnError(new UnhandledExceptionEventArgs(new Exception("Error loading remote playlist" + ex.Message), false));
			}
		}

		/// <summary>
		/// Clears the current connection
		/// </summary>
		public void Clear() {
			isConnected = false;
			playlist = new PlaylistCollection();
			_currentURI = null;
		}
	}
}
