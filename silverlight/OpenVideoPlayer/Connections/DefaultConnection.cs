﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Parsers;

namespace org.OpenVideoPlayer.Connections
{
    /// <summary>
    /// The DefaultConnection class is a primary IConnection object which is used
    /// to connect to playlists and media.
    /// </summary>
    public class DefaultConnection : IConnection
    {
        #region Properties
        protected Uri _currentURI;
        protected ParserManager pManager;
        protected List<IMediaItem> playlist;
        protected string error;
        protected bool isConnected;

        /// <summary>
        /// The last error message stored on the connection.  This is cleared on every
        /// operation.
        /// </summary>
        public string ErrorMsg
        {
            get { return error; }
        }

        /// <summary>
        /// The uri that was most recently parsed
        /// </summary>
        public Uri Uri
        {
            get { return _currentURI; }
        }

        /// <summary>
        /// The playlist generated by the parsers
        /// </summary>
        public List<IMediaItem> Playlist
        {
            get { return playlist; }
        }

        /// <summary>
        /// returns the state of the connection
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected; }
        }
        #endregion

        #region Event Delegates
        public event ConnectionEvents.ConnectionEventHandler Ready;
        public event ConnectionEvents.ConnectionEventHandler Error;

        protected virtual void OnLoaded(EventArgs e)
        {
            if (Ready != null) {
                Ready(this, e);
            }
        }

        protected virtual void OnError(EventArgs e)
        {
            if (Error != null) {
                Error(this, e);
            }
        }
        #endregion

        /// <summary>
        /// Initiates a new instance of the IConnection class.
        /// </summary>
        /// <param name="pManager">The parser manager to use on connect attempts</param>
        public DefaultConnection(ParserManager pManager)
        {
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
        public void connect(string uri)
        {
            this.connect(new Uri(uri));
        }

        /// <summary>
        /// connect to the named uri.  Calling this method initiates the connection to
        /// the uri specified and assigns the results to be parsed by the parser factory
        /// which was given to this object on construction.
        /// </summary>
        /// <param name="uri">The Uri to connect to</param>
        public void connect(Uri uri)
        {
            this.error = null;
            System.Diagnostics.Debug.WriteLine("Loading Url");
            
            //store out the current uri for use
            _currentURI = uri;

            try
            {
                WebClient client = new WebClient();
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(connect_DownloadStringCompleted);
                client.DownloadStringAsync(uri);
                isConnected = true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                error = "Error encountered connecting to resource " + uri.ToString() +
                    ". The message was: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("URL Error: " + ex.Message);
                OnError(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Directly calls the parsermanager and parsers for a given URI and stream. This is
        /// used both internally and is available externally for callback and direct
        /// parsing support.
        /// </summary>
        /// <param name="uri">The uri that sourced this stream</param>
        /// <param name="streamToParse">The Stream to parse</param>
        public void parseStream(Uri uri, Stream streamToParse)
        {
            _currentURI = uri;
            parseStreamNow(streamToParse);
        }

        /// <summary>
        /// This is the internal part of the direct stream parsing
        /// </summary>
        /// <param name="streamToParse">The Stream to parse</param>
        protected void parseStreamNow(Stream streamToParse)
        {
            IPlaylistParser p = pManager.getPlaylistParser(streamToParse, _currentURI);
            if (p != null) {
                //reset the stream position to 0 in case the Playlist parser consumed any of it
                streamToParse.Position = 0;
                p.load(streamToParse);
                playlist = p.getMediaItemList();
                OnLoaded(EventArgs.Empty);
            } else {
                error = "Error: No valid parsers to handle this request.";
                System.Diagnostics.Debug.WriteLine("No Valid Parsers found");
                OnError(EventArgs.Empty);
            }
        }

        internal void connect_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try {
                System.Diagnostics.Debug.WriteLine("Load complete. parsing");
                Stream reader = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
                parseStreamNow(reader);
                /*
                IPlaylistParser p = pManager.getPlaylistParser(reader, this._currentURI);
                if (p != null) {
                    //reset the stream position to 0 in case the Playlist parser consumed any of it
                    reader.Position = 0;
                    p.load(reader);
                    this.playlist = p.getMediaItemList();
                    OnLoaded(EventArgs.Empty);
                } else {
                    error = "Error: No valid parsers to handle this request.";
                    System.Diagnostics.Debug.WriteLine("No Valid Parsers found");
                    OnError(EventArgs.Empty);
                }*/
            } catch (Exception ex) {
                error = "Error encountered Parsing the response. The message was: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Error loading remote playlist");
                OnError(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Clears the current connection
        /// </summary>
        public void clear()
        {
            isConnected = false;
            playlist = null;
            _currentURI = null;
        }
    }
}