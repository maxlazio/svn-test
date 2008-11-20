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
using org.OpenVideoPlayer.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaFrameworkTests.Media
{
    [TestClass]
    public class ContentObjectTest
    {
        [TestMethod]
        public void CheckBitrate() {
            ContentObject fixture = new ContentObject();
            fixture.bitrate = 1;
            Assert.AreEqual(fixture.bitrate, 1);
        }

        [TestMethod]
        public void CheckDuration() {
            ContentObject fixture = new ContentObject();
            fixture.duration = "100";
            Assert.AreEqual(fixture.duration, "100");
        }

        [TestMethod]
        public void CheckFileSize() {
            ContentObject fixture = new ContentObject();
            fixture.fileSize = 1024;
            Assert.AreEqual(fixture.fileSize, 1024);
        }

        [TestMethod]
        public void CheckFrameRate() {
            ContentObject fixture = new ContentObject();
            fixture.framerate = 1111;
            Assert.AreEqual(fixture.framerate, 1111);
        }

        [TestMethod]
        public void CheckHeight() {
            ContentObject fixture = new ContentObject();
            fixture.height = 120;
            Assert.AreEqual(fixture.height, 120);
        }

        [TestMethod]
        public void CheckLang() {
            ContentObject fixture = new ContentObject();
            fixture.lang = "english";
            Assert.AreEqual(fixture.lang, "english");
        }

        [TestMethod]
        public void CheckType() {
            ContentObject fixture = new ContentObject();
            fixture.type = "video";
            Assert.AreEqual(fixture.type, "video");
        }

        [TestMethod]
        public void CheckUrl() {
            ContentObject fixture = new ContentObject();
            fixture.url = "some url";
            Assert.AreEqual(fixture.url, "some url");
        }

        [TestMethod]
        public void CheckWidth() {
            ContentObject fixture = new ContentObject();
            fixture.width = 111;
            Assert.AreEqual(fixture.width, 111);
        }
    }
}
