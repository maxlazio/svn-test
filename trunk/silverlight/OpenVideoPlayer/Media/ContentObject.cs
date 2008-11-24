
namespace org.OpenVideoPlayer.Media {
	/// <summary>
	/// A ContentObject is a representation of an individual bitrate of an individual
	/// asset.  It is a component of a IMediaItem object in that it is meant to represent
	/// a more granular component of the exeprience that the IMediaItem object represents
	/// </summary>
	public class ContentObject {
		public long FileSize { get; set; }
		public string Type { get; set; }
		public int Bitrate { get; set; }
		public int Framerate { get; set; }
		public string Duration { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }
		public string Lang { get; set; }
		public string Url { get; set; }
	}
}