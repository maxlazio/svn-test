﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Browser;

namespace org.OpenVideoPlayer.Media
{
    /// <summary>
    /// A IMediaItem interface defines an object that is consumable by the player.
    /// IMediaItem's are generated by a parser-factory class.
    /// </summary>
    public interface IMediaItem
    {
		[ScriptableMember]
		
        /// <summary>
        /// Stores the IMediaItem's author field here
        /// </summary>
        string Author { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the IMediaItem's title field here
        /// </summary>
        string Title { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the IMediaItem's description field
        /// </summary>
        string Description { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the type of asset this IMediaItem is managing
        /// </summary>
        MediaTypes Type { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the way in which this IMediaItem is delivered
        /// </summary>
        DeliveryTypes DeliveryType { get; set; }

		[ScriptableMember]
		string Comments { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the url for this IMediaItem
        /// </summary>
        string Url { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the length in seconds, of this IMediaItem
        /// </summary>
        long Length { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the height of this IMediaItem
        /// </summary>
        int Height { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores the width of this IMediaItem
        /// </summary>
        int Width { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Flags this IMediaItem as skippable
        /// </summary>
        bool Skippable { get; set; }

		[ScriptableMember]
        /// <summary>
        /// Stores a collection of thumbnails for this IMediaItem
        /// </summary>
        List<Thumbnail> Thumbnails { get; set; }

		[ScriptableMember]
        string ThumbSource { get; }

		[ScriptableMember]
        /// <summary>
        /// Stores a collection of ContentObjects that comprise this IMediaItem
        /// </summary>
        List<ContentObject> ContentList { get; set; }

		[ScriptableMember]
        ObservableCollection<ChapterItem> Chapters { get; }

		[ScriptableMember]
        /// <summary>
        /// Required to enable declaritve collections where playlistitems are instantiated in XAML with default constructor.
        /// </summary>
        PlaylistCollection OwnerCollection { set; }

		[ScriptableMember]
        int MyIndex { get; }

        /// <summary>
        /// Returns the Metadata stored on this video item
        /// </summary>
        /// <returns>A dictionary of name/array-value pairs</returns>
        Dictionary<string, string[]> getMeta();

        /// <summary>
        /// returns a specific keyword from the metadata stored on this video item
        /// </summary>
        /// <param name="keyword">The keyword to return</param>
        /// <returns>the metadata values stored on this keyword</returns>
        string[] GetMetaItem(string keyword);

        /// <summary>
        /// Adds a value to the metadata stored at this keyword
        /// </summary>
        /// <param name="keyword">The keyword to store this value at</param>
        /// <param name="value">The value to store</param>
        void AddMeta(string keyword, string value);

        /// <summary>
        /// Replaces the metadata stored on this keyword with a single string value
        /// </summary>
        /// <param name="keyword">The keyword to replace the metadata for</param>
        /// <param name="value">The value to store</param>
        void ReplaceMeta(string keyword, string value);

        /// <summary>
        /// Replaces the metadata stored on this keyword with an array of metadata values
        /// </summary>
        /// <param name="keyword">The keyword to replace the metadata for</param>
        /// <param name="value">The value to store</param>
        void ReplaceMeta(string keyword, List<string> value);
    }
}