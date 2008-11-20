using System;
using System.ComponentModel;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Media
{
    public class ChapterItem : INotifyPropertyChanged, IChapterItem
    {
        #region privates
        /// <summary>
        /// Determines whether seeking to this chapter triggers an interstitial
        /// event.  For now we're setting this to false until ad support is vetted.
        /// </summary>
        protected bool hasInterstitial = false;
        protected string title;
        protected double m_position;
        protected string thumbnail;

        #endregion

        #region Properties
        public bool HasInterstitial
        {
            get { return hasInterstitial; }
            set { hasInterstitial = value; }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                OnPropertyChanged("Title");
            }
        }

        public double Position
        {
            get { return m_position; }
            set
            {
                m_position = value;
                OnPropertyChanged("Position");
            }
        }

        public string PositionText
        {
            get
            {
            	TimeSpan pos = TimeSpan.FromSeconds(m_position);
				return string.Format("{0}:{1}:{2}", pos.Hours.ToString("00"), pos.Minutes.ToString("00"), pos.Seconds.ToString("00"));
					//TimeSpanStringConverter.ConvertToString(TimeSpan.FromSeconds(m_position), ConverterModes.TenthSecond);
            }
        }

        public string ThumbSource
        {
            get { return thumbnail; }
            set
            {
                thumbnail = value;
                OnPropertyChanged("ThumbSource");
            }
        }

        #endregion

        public ChapterItem() { }

        public ChapterItem(string title, double position, string thumbnail)
        {
            Position = position;
            Title = title;
            ThumbSource = thumbnail;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string memberName)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
        #endregion
    }
}
