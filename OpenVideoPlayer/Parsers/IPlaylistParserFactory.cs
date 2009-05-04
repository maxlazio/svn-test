using System;

namespace org.OpenVideoPlayer.Parsers
{
    /// <summary>
    /// A IPlaylistParserFactory implementation responsible for determining if
    /// it knows how to handle the given input and if so return a IPlaylistParser
    /// class to do so.
    /// </summary>
    public interface IPlaylistParserFactory
    {
        /// <summary>
        /// Returns the playlist parser that can handle this data
        /// </summary>
        /// <param name="input">The input Stream to handle</param>
        /// <param name="uri">The uri where this input stream was loaded from</param>
        /// <returns>IPlaylistParser is returned if the parser factory knows
        /// how to handle this data.  Otherwise a null value is returned and the
        /// consuming parser manager should move on to another parser factory.</returns>
        IPlaylistParser getParser(System.IO.Stream input, Uri uri);
    }
}
