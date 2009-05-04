using System;
using System.Globalization;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Media {
	public class PlaylistCollection : ObservableCollection<IMediaItem> {

		protected override void InsertItem(int index, IMediaItem item) {
			item.OwnerCollection = this;
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index) {
			this[index].OwnerCollection = null;
			base.RemoveItem(index);
		}
	}
}
