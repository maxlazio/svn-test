using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace org.OpenVideoPlayer.Media {
	/// <summary>
	/// An implementation of the IMediaItem interface, describes an item intended to handle video.
	/// </summary>
	public class VideoItem : IMediaItem, INotifyPropertyChanged {
		#region protected variables

		protected string title;
		protected string author;
		protected string description;
		protected long length;
		protected string url;
		protected int height;
		protected int width;
		protected bool skippable;
		protected MediaTypes mediaType;
		protected DeliveryTypes deliveryType;
		private List<Thumbnail> _mediaThumbnails = new List<Thumbnail>();
		private List<ContentObject> _mediaContentList = new List<ContentObject>();
		private Dictionary<string, string[]> _metadata = new Dictionary<string, string[]>();
		private ObservableCollection<ChapterItem> m_chapters = new ObservableCollection<ChapterItem>();
		private PlaylistCollection collectionParent;

		#endregion

		#region Properties
		/// <summary>
		/// The title of the video
		/// </summary>
		public string Title {
			get { return title; }
			set {
				title = value;
				OnPropertyChanged("Title");
			}
		}

		/// <summary>
		/// The author of the video
		/// </summary>
		public string Author {
			get { return author; }
			set {
				author = value;
				OnPropertyChanged("Author");
			}
		}

		private string comments;
		public string Comments {
			get { return comments; }
			set {
				comments = value;
				OnPropertyChanged("Comments");
			}
		}



		/// <summary>
		/// The description of the video
		/// </summary>
		public string Description {
			get { return description; }
			set {
				description = value;
				OnPropertyChanged("Description");
			}
		}

		/// <summary>
		/// The length of the video, typically in seconds
		/// </summary>
		public long Length {
			get { return length; }
			set {
				length = value;
				OnPropertyChanged("Length");
			}
		}

		/// <summary>
		/// The type of video
		/// </summary>
		public MediaTypes Type {
			get { return MediaTypes.Video; }
			set {
				//Do nothing
			}
		}

		public DeliveryTypes DeliveryType {
			get { return deliveryType; }
			set { deliveryType = value; }
		}

		/// <summary>
		/// The url of the video
		/// </summary>
		public string Url {
			get { return url; }
			set {
				url = value;
				OnPropertyChanged("Url");
			}
		}

		/// <summary>
		/// The height of the video
		/// </summary>
		public int Height {
			get { return height; }
			set {
				height = value;
				OnPropertyChanged("Height");
			}
		}

		/// <summary>
		/// The width of the video
		/// </summary>
		public int Width {
			get { return width; }
			set {
				width = value;
				OnPropertyChanged("Width");
			}
		}

		/// <summary>
		/// Is the video Skippable
		/// </summary>
		public bool Skippable {
			get { return skippable; }
			set {
				skippable = value;
				OnPropertyChanged("Skippable");
			}
		}

		/// <summary>
		/// Stores the list of thumbnails
		/// </summary>
		public List<Thumbnail> Thumbnails {
			get { return _mediaThumbnails; }

			set {
				_mediaThumbnails = value;
				OnPropertyChanged("Thumbnails");
			}
		}

		/// <summary>
		/// Stores a list of content objects that comprise this video
		/// </summary>
		public List<ContentObject> ContentList {
			get { return _mediaContentList; }

			set {
				_mediaContentList = value;
				OnPropertyChanged("ContentList");
			}
		}

		/// <summary>
		/// Stores a collection of the chapters in this video object
		/// </summary>
		public ObservableCollection<ChapterItem> Chapters {
			get { return m_chapters; }
		}

		/// <summary>
		/// Required to enable declaritve collections where playlistitems are instantiated in XAML with default constructor.
		/// </summary>
		public PlaylistCollection OwnerCollection {
			set {
				collectionParent = value;
			}
		}

		public string ThumbSource {
			get {
				if (_mediaThumbnails.Count > 0) {
					return _mediaThumbnails[0].Url;
				}
				return "";
			}
		}

		public int MyIndex {
			get {
				if (collectionParent != null)
					return collectionParent.IndexOf(this) + 1;
				return -1;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Returns the Metadata stored on this video item
		/// </summary>
		/// <returns>A dictionary of name/array-value pairs</returns>
		public Dictionary<string, string[]> getMeta() {
			return _metadata;
		}

		/// <summary>
		/// returns a specific keyword from the metadata stored on this video item
		/// </summary>
		/// <param name="keyword">The keyword to return</param>
		/// <returns>the metadata values stored on this keyword</returns>
		public string[] GetMetaItem(string keyword) {
			if (_metadata.ContainsKey(keyword)) {
				return _metadata[keyword];
			}
			return new string[0];
		}

		/// <summary>
		/// Adds a value to the metadata stored at this keyword
		/// </summary>
		/// <param name="keyword">The keyword to store this value at</param>
		/// <param name="value">The value to store</param>
		public void AddMeta(string keyword, string value) {
			if (_metadata.ContainsKey(keyword)) {
				List<string> myList = new List<string>(_metadata[keyword]);
				//_metadata[keyword].CopyTo(myList, 0);
				myList.Add(value);
				_metadata[keyword] = myList.ToArray();
			} else {
				_metadata[keyword] = new string[] { value };
			}
		}

		/// <summary>
		/// Replaces the metadata stored on this keyword with a single string value
		/// </summary>
		/// <param name="keyword">The keyword to replace the metadata for</param>
		/// <param name="value">The value to store</param>
		public void ReplaceMeta(string keyword, string value) {
			_metadata[keyword] = new string[] { value };
		}

		/// <summary>
		/// Replaces the metadata stored on this keyword with an array of metadata values
		/// </summary>
		/// <param name="keyword">The keyword to replace the metadata for</param>
		/// <param name="value">The value to store</param>
		public void ReplaceMeta(string keyword, List<string> value) {
			_metadata[keyword] = value.ToArray();
		}
		#endregion

		#region INotifyPropertyChanged

		protected void OnPropertyChanged(string memberName) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(memberName));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}