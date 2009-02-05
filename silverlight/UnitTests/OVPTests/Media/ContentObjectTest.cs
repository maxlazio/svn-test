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
            fixture.Bitrate = 1;
            Assert.AreEqual(fixture.Bitrate, 1);
        }

        [TestMethod]
        public void CheckDuration() {
            ContentObject fixture = new ContentObject();
            fixture.Duration = "100";
            Assert.AreEqual(fixture.Duration, "100");
        }

        [TestMethod]
        public void CheckFileSize() {
            ContentObject fixture = new ContentObject();
            fixture.FileSize = 1024;
            Assert.AreEqual(fixture.FileSize, 1024);
        }

        [TestMethod]
        public void CheckFrameRate() {
            ContentObject fixture = new ContentObject();
            fixture.Framerate = 1111;
            Assert.AreEqual(fixture.Framerate, 1111);
        }

        [TestMethod]
        public void CheckHeight() {
            ContentObject fixture = new ContentObject();
            fixture.Height = 120;
            Assert.AreEqual(fixture.Height, 120);
        }

        [TestMethod]
        public void CheckLang() {
            ContentObject fixture = new ContentObject();
            fixture.Lang = "english";
            Assert.AreEqual(fixture.Lang, "english");
        }

        [TestMethod]
        public void CheckType() {
            ContentObject fixture = new ContentObject();
            fixture.Type = "video";
            Assert.AreEqual(fixture.Type, "video");
        }

        [TestMethod]
        public void CheckUrl() {
            ContentObject fixture = new ContentObject();
            fixture.Url = "some url";
            Assert.AreEqual(fixture.Url, "some url");
        }

        [TestMethod]
        public void CheckWidth() {
            ContentObject fixture = new ContentObject();
            fixture.Width = 111;
            Assert.AreEqual(fixture.Width, 111);
        }
    }
}
