using System;
using System.Globalization;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Media
{
    public class PlaylistCollection : ObservableCollection<IMediaItem>
    {
        public PlaylistCollection()
        {
        }

        /*
        public PlaylistCollection(Uri uriDocument, String playlistXml)
        {
            ParseXml(uriDocument, playlistXml);
        }

        public void ParseXml(Uri uriDocument, String playlistXml)
        {
            // TODO: Move to & use the MSPlaylistParser Class.
            XDocument xmlPlaylist = XDocument.Parse(playlistXml);
            for (IEnumerator<XElement> iTor = xmlPlaylist.Descendants("playListItem").GetEnumerator(); iTor.MoveNext(); ) {
                VideoItem toAdd = new VideoItem();
                toAdd.Url = uriDocument.ToString();

                if (iTor.Current.Attribute("title") != null)
                {
                    toAdd.Title = System.Uri.UnescapeDataString(iTor.Current.Attribute("title").Value);
                }

                if (iTor.Current.Attribute("thumbSource") != null) {
                    toAdd.Thumbnails.Add(new Thumbnail(Conversion.GetPathFromUri(uriDocument,
                                                        System.Uri.UnescapeDataString(
                                                            iTor.Current.Attribute("thumbSource").Value)).ToString()));
                }

                if (iTor.Current.Attribute("mediaSource") != null)
                {
                    toAdd.Url = Conversion.GetPathFromUri(uriDocument,
                                                          System.Uri.UnescapeDataString(
                                                              iTor.Current.Attribute("mediaSource").Value)).ToString();
                }

                if (iTor.Current.Attribute("description") != null)
                {
                    toAdd.Description = System.Uri.UnescapeDataString(iTor.Current.Attribute("description").Value);
                }

                if (iTor.Current.Attribute("adaptiveStreaming") != null)
                {
                    toAdd.DeliveryType = bool.Parse(iTor.Current.Attribute("adaptiveStreaming").Value) ? DeliveryTypes.Adaptive : DeliveryTypes.Progressive;
                }

                for (IEnumerator<XElement> iTorChapters = iTor.Current.Descendants("chapter").GetEnumerator(); iTorChapters.MoveNext(); )
                {
                    string chapterTitle = "";
                    double chapterPosition = 0.0;
                    string chapterThumb = "";
                    if (iTorChapters.Current.Attribute("title") != null)
                    {
                        chapterTitle = iTorChapters.Current.Attribute("title").Value;
                    }
                    if (iTorChapters.Current.Attribute("position") != null)
                    {
                        chapterPosition = double.Parse(iTorChapters.Current.Attribute("position").Value, CultureInfo.InvariantCulture);
                    }
                    if (iTorChapters.Current.Attribute("thumbnailSource") != null)
                    {
                        chapterThumb = iTorChapters.Current.Attribute("thumbnailSource").Value;
                    }
                    toAdd.Chapters.Add(new ChapterItem(chapterTitle, chapterPosition, chapterThumb));
                }
                
                Add(toAdd);
            }
        }
        */

        protected override void InsertItem(int index, IMediaItem item)
        {
            item.OwnerCollection = this;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this[index].OwnerCollection = null;
            base.RemoveItem(index);
        }
    }
}
