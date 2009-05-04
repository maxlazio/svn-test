using System;
using org.OpenVideoPlayer.Parsers;

namespace org.OpenVideoPlayer.Parsers {
	/// <summary>
	/// Implements the IPlaylistParserFactory for parsing RSS Feeds
	/// </summary>
	public class RssFactory : IPlaylistParserFactory {

		#region IPlaylistParserFactory Members

		/// <summary>
		/// Determines if this class knows how to handle the passed in data
		/// </summary>
		/// <param name="input">The input stream</param>
		/// <param name="uri">The uri</param>
		/// <returns>A IPlaylistParser capable of handling this stream, or null.</returns>
		public IPlaylistParser getParser(System.IO.Stream input, Uri uri) {
			//check to see what the stream looks like and reply if we can handle it
			//NOTE - this doesn't work, many RSS feeds are generated from php, etc..
			if (isHttp(uri)) {//} && isRSSFile(uri)) {
				return new MediaRssParser();
			} else {
				return null;
			}
		}

		#endregion

		#region Private Methods

		private bool isRSSFile(Uri uri) {
			if (uri.ToString().ToLower().EndsWith(".rss") ||
				 uri.ToString().ToLower().EndsWith(".xml")) {
				return true;
			} else {
				return false;
			}
		}

		private bool isHttp(Uri uri) {
			if (uri.Scheme == "http" || uri.Scheme == "https") {
				return true;
			} else {
				return false;
			}
		}

		#endregion
	}
}
