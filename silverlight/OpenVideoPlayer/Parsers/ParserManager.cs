using System;

namespace org.OpenVideoPlayer.Parsers
{
    /// <summary>
    /// The ParserManager class is intantiated and given an array of factory objects.
    /// It's job is to determine the best parser for a given playlist by asking the
    /// parsers it has loaded if they can handle the playlist.
    /// </summary>
    public sealed class ParserManager
    {
        private IPlaylistParserFactory[] _factories;

        /// <summary>
        /// Default constructor, initializes the ParserManager with no factories
        /// loaded.
        /// </summary>
        public ParserManager()
        {
        }

        /// <summary>
        /// Initializes the ParserManager with the given list of factories, readying the manager.
        /// </summary>
        /// <param name="parser_factories">Array of factories to load</param>
        public ParserManager(IPlaylistParserFactory[] parser_factories)
        {
            _factories = parser_factories;
        }

        /// <summary>
        /// Loads an array of parsers into the ParserManager, readying the manager.
        /// </summary>
        /// <param name="parser_factories">Array of factoires to load</param>
        public void LoadParsers(IPlaylistParserFactory[] parser_factories)
        {
            _factories = parser_factories;
        }

        /// <summary>
        /// Attempts to return a playlist parser by calling each factory we have loaded
        /// and giving it the input stream and uri.
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="uri">The uri where the input stream was loaded from</param>
        /// <returns>A playlist parser that can handle the input stream</returns>
        public IPlaylistParser getPlaylistParser(System.IO.Stream input, Uri uri)
        {
            if ( _factories != null) {
                foreach (IPlaylistParserFactory f in _factories) {
                    IPlaylistParser retVal = f.getParser(input, uri);
                    if (retVal != null) {
                        return retVal;
                    }
                }
            } else {
                throw new NullReferenceException("No parser factories were loaded into the manager.");
            }
            return null;
        }
    }
}
