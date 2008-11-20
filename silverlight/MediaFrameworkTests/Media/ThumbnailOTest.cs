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
using org.OpenVideoPlayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Media;

namespace MediaFrameworkTests.Media
{
    [TestClass]
    public class ThumbnailOTest
    {
        [TestMethod]
        public void CheckUrl() {
            Thumbnail t = new Thumbnail();
            t.url = "url";
            Assert.AreEqual(t.url, "url");
        }

        [TestMethod]
        public void CheckWidth() {
            Thumbnail t = new Thumbnail();
            t.width = 320;
            Assert.AreEqual(t.width, 320);
        }

        [TestMethod]
        public void CheckHeight() {
            Thumbnail t = new Thumbnail();
            t.height = 200;
            Assert.AreEqual(t.height, 200);
        }

        [TestMethod]
        public void VanillaConstructor() {
            Thumbnail t = new Thumbnail();
            Assert.AreEqual(t.height, 0);
            Assert.AreEqual(t.width, 0);
            Assert.IsNull(t.url);
        }

        [TestMethod]
        public void UrlConstructor() {
            Thumbnail t = new Thumbnail("SOMEURL");
            Assert.AreEqual(t.height, 0);
            Assert.AreEqual(t.height, 0);
            Assert.AreEqual(t.url, "SOMEURL");
        }

        [TestMethod]
        public void FullConstructor() {
            Thumbnail t = new Thumbnail("URL",100,200);
            Assert.AreEqual(t.height, 200);
            Assert.AreEqual(t.width, 100);
            Assert.AreEqual(t.url, "URL");
        }
    }
}
