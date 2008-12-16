using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using org.OpenVideoPlayer.Media;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// A Playlist parser class for transforming rss feeds into multi-item
	/// playlists.  This is meant to be consumed by the rss factory.
	/// </summary>
	/// <remarks>
	/// There are a few ways this will soon be enhanced:
	/// TODO: parse multiple ContentObject objects in to the IMediaItem when a group tag is used.
	/// </remarks>
	public class MediaRssParser : IPlaylistParser {
		private string _title;
		private string _author;
		private string _sourceUri;
		private List<IMediaItem> items;
		private List<Thumbnail> thumbnails = new List<Thumbnail>();
		private Dictionary<string, string[]> _metadata = new Dictionary<string, string[]>();

		/// <summary>
		/// The title returned from the RSS Feed
		/// </summary>
		public string Title {
			get { return _title; }
		}

		/// <summary>
		/// The author returned from the RSS Feed
		/// </summary>
		public string Author {
			get { return _author; }
		}

		/// <summary>
		/// The image url for the playlist
		/// </summary>
		public string ImageURL {
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
		/// Number of items in the RSS Feed
		/// </summary>
		public int Count {
			get { return items.Count; }
		}

		/// <summary>
		/// Property optionally containing the root url of where the playlist was gathered from.
		/// </summary>
		public string SourceURI {
			get { return _sourceUri; }
		}

		#region Channel Metadata
		/// <summary>
		/// Retrieves an array of channel level metadata values stored on the playlist
		/// </summary>
		/// <param name="keyword">the keyword to retrieve</param>
		/// <returns>the array of applied metadata values</returns>
		public string[] getMeta(string keyword) {
			if (_metadata.ContainsKey(keyword)) {
				return _metadata[keyword];
			} else {
				return null;
			}
		}
		/// <summary>
		/// Adds a metadata value to the channel-level metadata of this playlist
		/// </summary>
		/// <param name="keyword">the keword position to assign the value to</param>
		/// <param name="value">the value to assign</param>
		public void addMeta(string keyword, string value) {
			if (_metadata.ContainsKey(keyword)) {
				List<string> myList = new List<string>(_metadata[keyword]);
				myList.Add(value);
				_metadata[keyword] = myList.ToArray();
			} else {
				_metadata[keyword] = new string[] { value };
			}
		}
		/// <summary>
		/// Adds a metadata value to the channel-level metadata of this playlist, replacing
		/// all previously assigned metadata at this keyword position.
		/// </summary>
		/// <param name="keyword">the keyword position to assign the value to</param>
		/// <param name="value">the value to assign</param>
		public void replaceMeta(string keyword, string value) {
			_metadata[keyword] = new string[] { value };
		}
		/// <summary>
		/// Adds a list ov metadata values to the channel-level metadata of this playlist,
		/// replacing all previously assigned metadata at this keyword position.
		/// </summary>
		/// <param name="keyword">the keyword position to assign the value to</param>
		/// <param name="value">the vlaue to assign</param>
		public void replaceMeta(string keyword, List<string> value) {
			_metadata[keyword] = value.ToArray();
		}
		#endregion

		/// <summary>
		/// Creates a new instance of the MediaRssParser class.
		/// </summary>
		public MediaRssParser() {
			items = new List<IMediaItem>();
			_metadata = new Dictionary<string, string[]>();
			thumbnails = new List<Thumbnail>();
		}

		/// <summary>
		/// Loads the RSS Feed provided in the input stream
		/// </summary>
		/// <param name="sourceUri">The uri source of the input stream</param>
		/// <param name="input">The input stream to parse</param>
		public void load(string sourceUri, Stream input) {
			_sourceUri = sourceUri;
			load(input);
		}

		/// <summary>
		/// Loads the RSS Feed provided in the input stream
		/// </summary>
		/// <param name="input">The input stream to parse</param>
		/// <remarks>NOTE: There is a lot of low-hanging fruit in here</remarks>
		public void load(Stream input) {
			System.Diagnostics.Debug.WriteLine("Load complete. parsing");
			XmlReader reader = XmlReader.Create(input);

			//loading data to items List
			Stack<string> elements = new Stack<string>();
			bool readingItem = false;
			bool popImmediately = false;    // will be used with empty elements in the stack
			IMediaItem vi = new VideoItem();

			while (reader.Read()) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						elements.Push(reader.Name.ToLower());
						popImmediately = reader.IsEmptyElement;
						string s = elements.Peek();
						switch (s) {
							case "image":
								reader.ReadToFollowing("url");
								this.thumbnails.Add(new Thumbnail(reader.ReadElementContentAsString()));
								break;
							case "author":  // TODO: Non-standard assignment of author
								this._author = reader.Value;
								break;
							case "item":
								readingItem = true;
								break;
							case "enclosure":
								reader.MoveToNextAttribute();
								do {
									switch (reader.Name) {
										case "url":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												vi.Url = reader.Value;
											}
											reader.MoveToNextAttribute();
											break;
										case "length":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												vi.Length = (long)reader.ReadContentAsLong();
											}
											reader.MoveToNextAttribute();
											break;
										case "type":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												if (reader.Value.ToLower() == "audio") {
													vi.Type = MediaTypes.Audio;
												} else {
													vi.Type = MediaTypes.Video;
												}
											}
											reader.MoveToNextAttribute();
											break;
										default:
											reader.MoveToNextAttribute();
											break;
									}
								} while (reader.Name != "");
								break;


							case "media:thumbnail":
								reader.MoveToNextAttribute();
								Thumbnail thumbnail = new Thumbnail();
								do {
									switch (reader.Name) {
										case "url":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												thumbnail.Url = reader.Value;
											}
											reader.MoveToNextAttribute();

											break;
										case "width":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												thumbnail.Width = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();

											break;
										case "height":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												thumbnail.Height = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();

											break;
										default:
											reader.MoveToNextAttribute();
											break;
									}
								} while (reader.Name != "");
								vi.Thumbnails.Add(thumbnail);
								break;


							case "media:content":
								reader.MoveToNextAttribute();
								ContentObject content = new ContentObject();
								do {

									switch (reader.Name) {
										case "url":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Url = reader.Value;
											}
											reader.MoveToNextAttribute();
											break;
										case "width":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Width = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();
											break;
										case "height":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Height = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();
											break;
										case "lang":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Lang = reader.Value;
											}
											reader.MoveToNextAttribute();
											break;
										case "duration":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Duration = reader.Value;
											}
											reader.MoveToNextAttribute();
											break;
										case "framerate":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Framerate = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();
											break;
										case "bitrate":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Bitrate = (int)reader.ReadContentAsInt();
											}
											reader.MoveToNextAttribute();
											break;
										case "type":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.Type = reader.Value;
											}
											reader.MoveToNextAttribute();
											break;
										case "fileSize":
											reader.ReadAttributeValue();
											if (reader.Value != "") {
												content.FileSize = (long)reader.ReadContentAsLong();
											}
											reader.MoveToNextAttribute();
											break;
										default:
											reader.MoveToNextAttribute();
											break;

									}
								} while (reader.Name != "");
								vi.ContentList.Add(content);
								break;

						}
						if (popImmediately) {
							//pop it immediately if its empty because we won't hit an
							//end element type
							elements.Pop();
						}
						break;

					case XmlNodeType.Text:
						s = elements.Peek();
						switch (s) {
							case "title":
								if (readingItem) { vi.Title = reader.Value; } else { this._title = reader.Value; }
								break;
							case "description":
								if (readingItem) {
									vi.Description = reader.Value;
								} else {
									this.addMeta("description", reader.Value);
								}
								break;
							case "author":
								vi.Author = reader.Value;
								break;
							case "category":
								vi.AddMeta("category", reader.Value);
								break;
							case "pubDate":
								vi.AddMeta("pubDate", reader.Value);
								break;
							case "media:title":
								vi.AddMeta("media:title", reader.Value);
								break;
							case "media:description":
								vi.AddMeta("media:description", reader.Value);
								break;
							case "media:keywords":
								vi.AddMeta("media:keywords", reader.Value);
								break;
							case "media:copyright":
								vi.AddMeta("media:copyright", reader.Value);
								break;
						}
						break;

					case XmlNodeType.EndElement:
						string e = elements.Pop();
						if (e == "item") {
							items.Add(vi);
							vi = new VideoItem();
							readingItem = false;
						}
						break;
				}
			}
		}

		/// <summary>
		/// Retrieves a IMediaItem at the specified index.
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		/// <returns>The specified media item.</returns>
		public IMediaItem getItemAt(int index) {
			if (index < 0 || index > items.Count) {
				throw new IndexOutOfRangeException("No IMediaItem exists in this position");
			}
			return items[index];
		}

		/// <summary>
		/// Retrieves the entire parsed playlist.
		/// </summary>
		/// <returns>a list of MediaItems that make up this playlist</returns>
		public List<IMediaItem> getMediaItemList() {
			return items;
		}

	}
}
