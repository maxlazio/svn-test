
namespace org.OpenVideoPlayer.Media
{
    /// <summary>
    /// A ContentObject is a representation of an individual bitrate of an individual
    /// asset.  It is a component of a IMediaItem object in that it is meant to represent
    /// a more granular component of the exeprience that the IMediaItem object represents
    /// </summary>
    public class ContentObject
    {
        public long fileSize { get; set; }
        public string type { get; set; }
        public int bitrate { get; set; }
        public int framerate { get; set; }
        public string duration { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public string lang { get; set; }
        public string url { get; set; }
    }
}