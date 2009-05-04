using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Media {
	public interface IAdaptiveSegment {
		Size Resolution {get;set;}
		long Bitrate { get; set; }
		string Codec { get; set; }
		bool Selected { get; set; }
	}

	//for now we dont have much reason to care about audio, just get video info
	public class AdaptiveSegment : IAdaptiveSegment {
		public Size Resolution { get; set; }
		public long Bitrate { get; set; }
		public string Codec { get; set; }
		public bool Selected { get; set; }
	}
}
