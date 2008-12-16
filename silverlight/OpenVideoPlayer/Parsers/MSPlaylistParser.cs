using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using org.OpenVideoPlayer.Media;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Parsers
{
    public class MSPlaylistParser : IPlaylistParser
    {
        private string _title;
        private string _author;
        private string _sourceUri;
        private List<IMediaItem> items;
        private List<Thumbnail> thumbnails;
        private Dictionary<string, string[]> _metadata = new Dictionary<string, string[]>();

        #region Implementation of IPlaylistParser

        /// <summary>
        /// Property containing the playlist title
        /// </summary>
        public string Title
        {
            get { return _title; }
        }

        /// <summary>
        /// Property containing the Playlist author
        /// </summary>
        public string Author
        {
            get { return _author; }
        }

        /// <summary>
        /// Property containing the image url of the playlist
        /// </summary>
        public string ImageURL
        {
			get {
				//we only support a single channel url so return the last one added
				if (thumbnails != null && this.thumbnails.Count > 0 && thumbnails[this.thumbnails.Count - 1] != null) {
					return this.thumbnails[this.thumbnails.Count - 1].Url;
				} else {
					return null;
				}
			}
        }

        /// <summary>
        /// Number of items in the playlist
        /// </summary>
        public int Count
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Property optionally containing the root url of where the playlist was gathered from.
        /// </summary>
        public string SourceURI
        {
            get { return _sourceUri; }
        }

        /// <summary>
        /// constructs a new instance of the MSPlaylistParser class
        /// </summary>
        /// <param name="uri">The uri source on the stream</param>
        public MSPlaylistParser(string uri)
        {
            _sourceUri = uri;
            items = new List<IMediaItem>();
        }

        /// <summary>
        /// Load and parse the playlist
        /// </summary>
        /// <param name="sourceUri">The source uri where the stream was loaded</param>
        /// <param name="input">Input Stream to parse</param>
        public void load(string sourceUri, Stream input)
        {
            
        }

        /// <summary>
        /// Load and parse the playlist
        /// </summary>
        /// <param name="input">Input Stream to parse</param>
        public void load(Stream input)
        {
            string streamContents = "";
            using (StreamReader reader = new StreamReader(input))
            {
                streamContents = reader.ReadToEnd();
            }
            XDocument xmlPlaylist = XDocument.Parse(streamContents);
            for (IEnumerator<XElement> iTor = xmlPlaylist.Descendants("playListItem").GetEnumerator(); iTor.MoveNext(); ) {
                Extract(iTor);
            }
			for (IEnumerator<XElement> iTor = xmlPlaylist.Descendants("mediaItem").GetEnumerator(); iTor.MoveNext(); ) {
				Extract(iTor);
			}
        }

    	private void Extract(IEnumerator<XElement> iTor) {
    		try {
    			VideoItem toAdd = new VideoItem();
    			toAdd.Url = "";

    			if (iTor.Current.Attribute("title") != null) {
    				toAdd.Title = System.Uri.UnescapeDataString(iTor.Current.Attribute("title").Value);
    			}

    			if (iTor.Current.Attribute("thumbSource") != null && iTor.Current.Attribute("thumbSource").Value != "") {
    				toAdd.Thumbnails.Add(new Thumbnail(Conversion.GetPathFromUri(new Uri(_sourceUri),
    				                                                             System.Uri.UnescapeDataString(iTor.Current.Attribute("thumbSource").Value)).ToString()));
    			}

    			if (iTor.Current.Attribute("mediaSource") != null) {
    				toAdd.Url = Conversion.GetPathFromUri(new Uri(_sourceUri),
    				                                      System.Uri.UnescapeDataString(
    				                                      	iTor.Current.Attribute("mediaSource").Value)).ToString
    					();
    			}

    			if (iTor.Current.Attribute("description") != null) {
    				toAdd.Description = System.Uri.UnescapeDataString(iTor.Current.Attribute("description").Value);
    			}

    			if (iTor.Current.Attribute("adaptiveStreaming") != null) {
    				toAdd.DeliveryType = bool.Parse(iTor.Current.Attribute("adaptiveStreaming").Value)
    				                     	? DeliveryTypes.Adaptive
    				                     	: DeliveryTypes.Progressive;
    			}

    			for (IEnumerator<XElement> iTorChapters = iTor.Current.Descendants("chapter").GetEnumerator();
    			     iTorChapters.MoveNext();) {
    				string chapterTitle = "";
    				double chapterPosition = 0.0;
    				string chapterThumb = "";
    				if (iTorChapters.Current.Attribute("title") != null) {
    					chapterTitle = iTorChapters.Current.Attribute("title").Value;
    				}
    				if (iTorChapters.Current.Attribute("position") != null) {
    					chapterPosition = double.Parse(iTorChapters.Current.Attribute("position").Value,
    					                               CultureInfo.InvariantCulture);
    				}
    				if (iTorChapters.Current.Attribute("thumbnailSource") != null) {
    					chapterThumb = iTorChapters.Current.Attribute("thumbnailSource").Value;
    				}
    				toAdd.Chapters.Add(new ChapterItem(chapterTitle, chapterPosition, chapterThumb));
    			}

    			items.Add(toAdd);
    		} catch (Exception ex) {
    			Debug.WriteLine("Error during xml parsing phase "+ex.ToString());
    		}
    		//Add(toAdd);
    	}

    	/// <summary>
        /// Returns the playlist item at the specified index
        /// </summary>
        /// <param name="index">the item to return</param>
        /// <returns>The IMediaItem object at the specified index. Throws an IndexOutOfBounds
        /// exception if the index is invalid.</returns>
        public IMediaItem getItemAt(int index)
        {
            if (index < 0 || index > items.Count) {
                throw new IndexOutOfRangeException("No IMediaItem exists in this position");
            }
            return items[index];
        }

        /// <summary>
        /// Retrieves the list of IMediaItem's parsed by the parser
        /// </summary>
        /// <returns>The List of available media items</returns>
        public List<IMediaItem> getMediaItemList()
        {
            return items;
        }

        /// <summary>
        /// Returns the metadata stored at the given keyword
        /// </summary>
        /// <param name="keyword">the keyword to retrieve</param>
        /// <returns>Array of metadata</returns>
        public string[] getMeta(string keyword)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Adds metadata at this keyword position
        /// </summary>
        /// <param name="keyword">The keyword to add metadata to</param>
        /// <param name="value">The metadata to add</param>
        public void addMeta(string keyword, string value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Replaces metadata at this keyword position with the given value
        /// </summary>
        /// <param name="keyword">The keyword to replace</param>
        /// <param name="value">The string metadata to put in this position</param>
        public void replaceMeta(string keyword, string value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Replaces metadata at this keyword position with the given array
        /// </summary>
        /// <param name="keyword">The keyword to replace</param>
        /// <param name="value">The array of metadata to put in this position</param>
        public void replaceMeta(string keyword, List<string> value)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
