using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;

//TODO: There's something wrong with the xml so load is failing and the test isn't ready yet
namespace MediaFrameworkTests.Parsers
{
    [TestClass]
    public class MediaRssParserTest
    {
        [TestMethod]
        public void TestPropertiesOnInstantiation()
        {
            MediaRssParser m = new MediaRssParser();
            Assert.IsNull(m.Author);
            Assert.IsNull(m.ImageURL);
            Assert.AreEqual(m.Count, 0);
            Assert.IsNull(m.Title);
        }

        [TestMethod]
        public void TestLoad() {
            MediaRssParser m = new MediaRssParser();
            m.load(S());
            Assert.AreEqual(m.Author, "");   //This will be blank because there's no author in our rss
            Assert.AreEqual(m.Title, "MPF WM Silverlight Test Feed (Download)");
            //Dropped the feed image for the moment. TODO: PUT THE FEED IMAGE CHECK BACK
            //Assert.AreEqual(m.ImageURL, "http://boss.streamos.com/download/products/rss_manager/mpf_wmsilverlight/metaliq/thumbnails/akam_sos.png");
            Assert.AreEqual(m.Count, 1);
        }

        protected static Stream S() {
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
"<rss xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\" xmlns:media=\"http://search.yahoo.com/mrss\" version=\"2.0\">" +
"<channel>\" +" +
    "<generator>Stream OS</generator>" +
    "<pubDate>Sat, 30 Aug 2008 00:19:09 +0000</pubDate>" +
    "<lastBuildDate>Sat, 30 Aug 2008 00:19:09 +0000</lastBuildDate>" +
    "<title>MPF WM Silverlight Test Feed (Download)</title>" +
    "<link>https://secure.streamos.com</link>" +
    "<description>This is a test feed containing Windows Media progressive download content for playback in a player application using the Windows Media Silverlight plug-in.</description>" +
    //"<image>" +
      //"<url>http://boss.streamos.com/download/products/rss_manager/mpf_wmsilverlight/metaliq/thumbnails/akam_sos.png</url>" +
      //"<link>https://secure.streamos.com</link>" +
      //"<width>64</width>" +
      //"<height>64</height>" +
    //"</image>" +
    "<itunes:block xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\">yes</itunes:block>" +
    "<itunes:explicit xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\">yes</itunes:explicit>" +
    "<item>" +
      "<title>Madness: Live at Finsbury 1992</title>" +
      "<author>Akamai Technologies</author>" +
      "<description>Madstock concert.</description>" +
      "<pubDate>Tue, 24 Jul 2007 15:06:47 +0000</pubDate>" +
      "<enclosure url=\"http://products.edgeboss.net/download/products/rss_manager/mpf_wmsilverlight/metaliq/madness_finsbury_92_300k_demo.wmv?rss_feedid=427\" length=\"11511882\" type=\"application/vnd.ms-asf\"/>" +
      "<media:content xmlns:media=\"http://search.yahoo.com/mrss\" fileSize=\"11511882\" type=\"application/vnd.ms-asf\" medium=\"video\" isDefault=\"\" expression=\"sample\" bitrate=\"300\" framerate=\"\" samplingrate=\"\" channels=\"\" duration=\"5:00\" height=\"240\" width=\"320\" lang=\"en\" url=\"http://products.edgeboss.net/download/products/rss_manager/mpf_wmsilverlight/metaliq/madness_finsbury_92_300k_demo.wmv?rss_feedid=427\"/>" +
      "<media:description xmlns:media=\"http://search.yahoo.com/mrss\">Madstock concert.</media:description>" +
      "<media:thumbnail xmlns:media=\"http://search.yahoo.com/mrss\" height=\"90\" width=\"120\" time=\"\" url=\"http://products.edgeboss.net/download/products/rss_manager/mpf_wmsilverlight/metaliq/thumbnails/madness_finsbury_92.png\"/>" +
      "<media:title xmlns:media=\"http://search.yahoo.com/mrss\">Madness: Live at Finsbury 1992</media:title>" +
    "</item>" +
  "</channel>" +
"</rss>"));
        }
    }
}
