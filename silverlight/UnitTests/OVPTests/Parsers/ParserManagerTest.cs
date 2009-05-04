using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;

namespace MediaFrameworkTests.Parsers
{
    [TestClass]
    public class ParserManagerTest
    {
        [TestMethod]
        public void TestGetParser()
        {
            ParserManager pm = new ParserManager();
            List<IPlaylistParserFactory> parserFactories = new List<IPlaylistParserFactory>();

            //Add expected parser factories here
            parserFactories.Add(new RssFactory());
            parserFactories.Add(new BossFactory());

            //load the parsers we've selected into the parser manager and return it
            pm.LoadParsers(parserFactories.ToArray());
            Assert.IsNotNull(pm.getPlaylistParser(S(), new Uri("http://host/file.asx")));
            Assert.IsNotNull(pm.getPlaylistParser(S(), new Uri("http://host/file.rss")));
            Assert.IsInstanceOfType(pm.getPlaylistParser(S(), new Uri("http://host/file.rss")), typeof(MediaRssParser));
            Assert.IsInstanceOfType(pm.getPlaylistParser(S(), new Uri("http://host/file.asx")), typeof(WMetafileParser));
            Assert.IsNull(pm.getPlaylistParser(S(), new Uri("http://host/file.text")));
        }

        public static Stream S() {
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes("NOTHING"));
        }
    }
}
