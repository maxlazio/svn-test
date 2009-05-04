using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using org.OpenVideoPlayer.Media;
using System.Diagnostics;
using System.Text;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// A Playlist parser class for transforming ASX/WVX/WAX files directly into single-item
	/// playlists.  This is meant to be consumed by the boss parser factory.  It pulls the
	/// author tags out of the ASX file itself but since the asx file is a reference to a remote
	/// asset it simply uses the asx url as the media-item url.
	/// </summary>
	public class WMetafileParser : IPlaylistParser {
		private string _title;
		private string _author;
		private string _uri;
		private List<IMediaItem> items;
		private Dictionary<string, string[]> _metadata;

		/// <summary>
		/// The title returned from the ASX file
		/// </summary>
		public string Title {
			get { return _title; }
		}

		/// <summary>
		/// The author returned from the ASX file
		/// </summary>
		public string Author {
			get { return _author; }
		}

		/// <summary>
		/// The image url for the playlist
		/// </summary>
		/// <remarks>Not supported in this parser because the ASX format doesn't
		/// really have a applicable element.</remarks>
		public string ImageURL {
			get {
				// Images aren't really supported in ASX level xml so no playlist
				// level thumbnail is supported here.
				return null;
			}
		}

		/// <summary>
		/// The number of items in the playlist.  Even though it points to items.Count
		/// it will almost always be 0 or 1.
		/// </summary>
		public int Count {
			get { return items.Count; }
		}

		/// <summary>
		/// Property optionally containing the root url of where the playlist was gathered from.
		/// </summary>
		public string SourceURI {
			get { return _uri; }
		}

		#region Metadata Access
		/// <summary>
		/// Retrieves an array of channel level metadata values stored on the playlist
		/// </summary>
		/// <param name="keyword">the keyword to retrieve</param>
		/// <returns>the array of applied metadata values</returns>
		public string[] getMeta(string keyword) {
			if (_metadata.ContainsKey(keyword)) {
				return _metadata[keyword];
			}
			return null;
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
		/// Creates a new instance of the WMetafileParser class.
		/// </summary>
		/// <param name="uri">The uri which the ASX file was retrieved from.  This will be
		/// used as the url for the single entry playlist, since the player parses asx files
		/// directly.</param>
		public WMetafileParser(Uri uri) {
			items = new List<IMediaItem>();
			_metadata = new Dictionary<string, string[]>();
			_uri = uri.ToString();
		}

		/// <summary>
		/// Parse the metafile, extracting all values and assigning them as metadata to
		/// the video item represented by this playlist.
		/// </summary>
		/// <param name="sourceUri">The source uri for this playlist</param>
		/// <param name="input">The XML Stream to parse</param>
		public void load(string sourceUri, Stream input) {
			_uri = sourceUri;
			load(input);
		}

		/// <summary>
		/// Parse the metafile, extracting all values and assigning them as metadata to
		/// the single video item represented by this playlist.
		/// </summary>
		/// <param name="input">The XML Stream to parse</param>
		public void load(System.IO.Stream input) {

			//This is bit of a hack to fix ASX's non-standard use of ampersands
			StreamReader sr = new StreamReader(input);
			String s = sr.ReadToEnd();
			if (s.Contains("&") && s.Contains("ref")) {
				int a = -1;
				bool flag = false;
				while ((a = s.IndexOf('&', a + 1)) > 0) {
					int sc = s.IndexOf(';', a);
					if (sc < 0 || sc > 5) {
						s = s.Insert(a + 1, "amp;");
						flag = true;
					}
				}
				if (flag) {
					byte[] b = Encoding.UTF8.GetBytes(s);
					input = new MemoryStream(b);
				}
			}

			System.Diagnostics.Debug.WriteLine("Load complete. parsing windows meta file");
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.CheckCharacters = false;
			settings.CloseInput = true;
			settings.IgnoreProcessingInstructions = true;
			//settings.ConformanceLevel = ConformanceLevel.Fragment;
			//settings.DtdProcessing = DtdProcessing.Ignore;

			XmlReader reader = XmlReader.Create(input, settings);

			//loading data to items List
			bool readingEntry = false;
			Stack<string> elements = new Stack<string>();
			bool popImmediately = false;    // will be used with empty elements in the stack
			IMediaItem vi = new VideoItem();
			vi.Url = _uri;

			try {
				while (reader.Read()) {
					switch (reader.NodeType) {
						case XmlNodeType.Element:
							elements.Push(reader.Name.ToLower());  // we use ToLower because WMP is case insensitive
							popImmediately = reader.IsEmptyElement;
							switch (reader.Name.ToLower()) {
								case "entry":
									readingEntry = true;
									break;
								case "param":
									if (readingEntry) {
										vi.AddMeta(reader.GetAttribute("NAME").ToString(), reader.GetAttribute("VALUE").ToString());
									}
									break;
								case "ref":
									string hr = reader.GetAttribute("href");
									if (!string.IsNullOrEmpty(hr)) {
										if (readingEntry) {
											vi.Url = hr;
										} else {
											this._uri = hr;
										}
									}
									break;
							}
							if (popImmediately) {
								//pop it immediately if its empty because we won't hit an
								//end element type
								elements.Pop();
							}
							break;

						case XmlNodeType.Text:
							string tag = elements.Peek();
							switch (tag) {
								case "title":
									if (readingEntry) {
										vi.Title = reader.Value;
									} else {
										this._title = reader.Value;
									}
									break;
								case "author":
									if (readingEntry) {
										vi.Author = reader.Value;
									} else {
										this._author = reader.Value;
									}
									break;
								default:
									if (readingEntry) {
										vi.AddMeta(tag, reader.Value);
									} else {
										this.addMeta(tag, reader.Value);
									}
									break;
							}
							break;

						case XmlNodeType.EndElement:
							string e = elements.Pop();
							if (e == "entry") {
								readingEntry = false;
							}
							break;

						case XmlNodeType.Whitespace:
							break;

						default:
							break;
					}
				}
			} catch (Exception ex) {
				//it's actually quite likely that we'll catch an exception here due to commonly
				//malformed XML in ASX files.
				Debug.WriteLine("Ouch! " + ex);
				//TODO: Be sure we have enough info in the video item and add it if it doesn't exist
			}

			items.Add(vi);
		}

		/// <summary>
		/// Retrieves a IMediaItem at the specified index.
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		/// <returns>The specified media item.</returns>
		public IMediaItem getItemAt(int index) {
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
