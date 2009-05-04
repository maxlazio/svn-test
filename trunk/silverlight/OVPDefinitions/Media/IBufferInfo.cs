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
	public interface IBufferInfo {
		ulong Size { get; set; }
		TimeSpan Time { get; set; }
	}

	public class BufferInfo : IBufferInfo {
		public ulong Size { get; set; }
		public TimeSpan Time { get; set; }
	}
}
