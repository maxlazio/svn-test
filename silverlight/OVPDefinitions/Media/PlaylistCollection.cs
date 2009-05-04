using System;
using System.Globalization;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Media {
	public class PlaylistCollection : ObservableCollection<IMediaItem> {

		/// <summary>
		/// Property containing the playlist title
		/// </summary>
		public string Title { get; set;}

		/// <summary>
		/// Property containing the Playlist author
		/// </summary>
		public string Author { get; set; }

		/// <summary>
		/// Property containing the image url of the playlist
		/// </summary>
		public string ImageURL { get; set; }

		/// <summary>
		/// Property optionally containing the root url of where the playlist was gathered from.
		/// </summary>
		public string SourceURI { get; set; }

		protected override void InsertItem(int index, IMediaItem item) {
			item.OwnerCollection = this;
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index) {
			this[index].OwnerCollection = null;
			base.RemoveItem(index);
		}

		public event RoutedEventHandler LoadComplete;

		public void LoadCompleted() {
			if(LoadComplete!=null) LoadComplete(this, new RoutedEventArgs());
		}
	}
}
