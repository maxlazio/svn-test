using System;
using System.Collections.Generic;
using System.IO;
using org.OpenVideoPlayer.EventHandlers;
using org.OpenVideoPlayer.Media;

namespace org.OpenVideoPlayer.Connections {
	/// <summary>
	/// Defines an external connection to an http/https resource
	/// </summary>
	public interface IConnection {
		/// <summary>
		/// Resets the state of the connection to a disconnected, not-ready state
		/// </summary>
		void Clear();

		/// <summary>
		/// Connect to the specified url
		/// </summary>
		/// <param name="uri">The URI to connect to as a string</param>
		void Connect(string uri);
		/// <summary>
		/// Connect to the specified url
		/// </summary>
		/// <param name="uri">The uri object to use as a destination</param>
		void Connect(Uri uri);

		/// <summary>
		/// Directly calls the parsermanager and parsers for a given stream.
		/// </summary>
		/// <param name="uri">The uri that sourced this stream</param>
		/// <param name="streamToParse">The Stream to parse</param>
		void ParseStream(Uri uri, Stream streamToParse);

		/// <summary>
		/// Property containing the uri this instance is currently connected to
		/// </summary>
		Uri Uri { get; }

		/// <summary>
		/// Property showing the connection state
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Property containing the data parsed on the connection
		/// </summary>
		PlaylistCollection Playlist { get; }

		/// <summary>
		/// Register a Loaded event handler
		/// </summary>
		event ConnectionEvents.ConnectionEventHandler Loaded;

		/// <summary>
		/// Register an Error event handler
		/// </summary>
		event EventHandler<UnhandledExceptionEventArgs> Error;
	}
}
