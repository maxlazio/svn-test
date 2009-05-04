using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Parsers
{
    public class MSPlaylistFactory : IPlaylistParserFactory
    {
        public IPlaylistParser getParser(Stream input, Uri uri)
        {
			if (uri.ToString().ToLower().Contains(".xml")) {

				//Right now we don't support any other xml format but the default
				//expression template format
				return new MSPlaylistParser(uri.ToString());
			} else return null;
        }
    }
}
