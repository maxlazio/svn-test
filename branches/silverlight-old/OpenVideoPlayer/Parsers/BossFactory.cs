using System;

namespace org.OpenVideoPlayer.Parsers
{
    public class BossFactory : IPlaylistParserFactory
    {

        #region IPlaylistParserFactory Members

        public IPlaylistParser getParser(System.IO.Stream input, Uri uri)
        {
            //check to see what the stream looks like and reply if we can handle it
            string matcher = uri.ToString().ToLower();
            if (matcher.EndsWith(".asx") ||
                matcher.EndsWith(".asf") ||
                matcher.EndsWith(".wvx") ||
                matcher.EndsWith(".wmv") ||
                matcher.EndsWith(".wax") ||
                matcher.EndsWith(".wma")) {
                return new WMetafileParser(uri);
            } else {
                return null;
            }
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
