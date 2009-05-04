using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;

namespace MediaFrameworkTests.Parsers
{
    [TestClass]
    public class RssFactoryTest
    {
        [TestMethod]
        public void TestGetParser()
        {
            RssFactory r = new RssFactory();
            Assert.IsNotNull(r.getParser(S(), new Uri("http://test/test.xml")));
            Assert.IsNotNull(r.getParser(S(), new Uri("http://test/test.rss")));
            Assert.IsNotNull(r.getParser(S(), new Uri("http://test/test.RSS")));
            Assert.IsNotNull(r.getParser(S(), new Uri("https://test/test.xml")));
            Assert.IsNull(r.getParser(S(), new Uri("rtsp://test/test.rss")));
            Assert.IsNull(r.getParser(S(), new Uri("http://test/test.asx")));
        }

        public static Stream S() {
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes("DOESN'T MATTER"));
        }
    }
}
