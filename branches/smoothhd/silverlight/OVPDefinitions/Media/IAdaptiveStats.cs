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

	public class AdaptiveStats  {
		public IAdaptiveSegment[] Segments { get; set; }
		public double MaxBitrate { get; set; }
		public double CapBitrate { get; set; }
		public Size CapBitrateSize { get; set; }
		public double BitratePercent { get; set; }
		public DateTime LastUpdated = DateTime.Now;
	}
}
