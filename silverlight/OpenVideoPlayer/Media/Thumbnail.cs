
namespace org.OpenVideoPlayer.Media
{
    /// <summary>
    /// Thumbnail object represents the attributes of a thumbnail
    /// for use throughout the framework
    /// </summary>
    public class Thumbnail
    {
        /// <summary>
        /// The url of the thumbnail
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// The width of the thumbnail
        /// </summary>
        public int width { get; set; }

        /// <summary>
        /// The height of the thumbnail
        /// </summary>
        public int height { get; set; }

        /// <summary>
        /// Default constructor, creates a thumbnail object
        /// </summary>
        public Thumbnail()
        {
            url = null;
            width = 0;
            height = 0;
        }

        /// <summary>
        /// Creates a thumbnail object
        /// </summary>
        /// <param name="url">The url to use for the thumbnail</param>
        public Thumbnail(string url)
        {
            this.url = url;
            width = 0;
            height = 0;
        }

        /// <summary>
        /// Creates a thumbnail object
        /// </summary>
        /// <param name="url">The url to use for the thumbnail</param>
        /// <param name="width">The height of the thumbnail</param>
        /// <param name="height">The width of the thumbnail</param>
        public Thumbnail(string url, int width, int height)
        {
            this.url = url;
            this.width = width;
            this.height = height;
        }
    }
}
