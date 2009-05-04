using System.IO;
using System.Collections.Generic;
using org.OpenVideoPlayer.Media;

namespace org.OpenVideoPlayer.Parsers
{
    /// <summary>
    /// Defines an interface for an implementing class to parse and describe a playlist
    /// </summary>
    public interface IPlaylistParser
    {
        /// <summary>
        /// Property containing the playlist title
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Property containing the Playlist author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Property containing the image url of the playlist
        /// </summary>
        string ImageURL { get; }

        /// <summary>
        /// Property optionally containing the root url of where the playlist was gathered from.
        /// </summary>
        string SourceURI { get; }

        /// <summary>
        /// Number of items in the playlist
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Load and parse the playlist
        /// </summary>
        /// <param name="input">Input Stream to parse</param>
        void load(Stream input);

        /// <summary>
        /// Load and parse the playlist
        /// </summary>
        /// <param name="sourceUri">The source url</param>
        /// <param name="input">Input Stream to parse</param>
        void load(string sourceUri, Stream input);

        /// <summary>
        /// Returns the playlist item at the specified index
        /// </summary>
        /// <param name="index">the item to return</param>
        /// <returns>The IMediaItem object at the specified index. Throws an IndexOutOfBounds
        /// exception if the index is invalid.</returns>
        IMediaItem getItemAt(int index);

        /// <summary>
        /// Retrieves the list of IMediaItem's parsed by the parser
        /// </summary>
        /// <returns>The List of available media items</returns>
        List<IMediaItem> getMediaItemList();

        /// <summary>
        /// Returns the metadata stored at the given keyword
        /// </summary>
        /// <param name="keyword">the keyword to retrieve</param>
        /// <returns>Array of metadata</returns>
        string[] getMeta(string keyword);

        /// <summary>
        /// Adds metadata at this keyword position
        /// </summary>
        /// <param name="keyword">The keyword to add metadata to</param>
        /// <param name="value">The metadata to add</param>
        void addMeta(string keyword, string value);

        /// <summary>
        /// Replaces metadata at this keyword position with the given value
        /// </summary>
        /// <param name="keyword">The keyword to replace</param>
        /// <param name="value">The string metadata to put in this position</param>
        void replaceMeta(string keyword, string value);

        /// <summary>
        /// Replaces metadata at this keyword position with the given array
        /// </summary>
        /// <param name="keyword">The keyword to replace</param>
        /// <param name="value">The array of metadata to put in this position</param>
        void replaceMeta(string keyword, List<string> value);
    }
}
