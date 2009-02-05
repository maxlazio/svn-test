using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Media;

namespace MediaFrameworkTests.Media
{
    [TestClass]
    public class VideoItemTest
    {
        [TestMethod]
        public void TestInstantiation() {
            VideoItem v = new VideoItem();
            Assert.IsNotNull(v);
            Assert.IsInstanceOfType(v, typeof(VideoItem));
        }

        [TestMethod]
        public void CheckAuthor() {
            VideoItem v = new VideoItem();
            v.Author = "myauthor";
            Assert.AreEqual(v.Author, "myauthor");
        }

        [TestMethod]
        public void CheckDesc() {
            VideoItem v = new VideoItem();
            Assert.IsNull(v.Description);
            v.Description = "testdesc";
            Assert.AreEqual(v.Description, "testdesc");
        }

        [TestMethod]
        public void CheckTitle() {
            VideoItem v = new VideoItem();
            Assert.IsNull(v.Title);
            v.Title = "mytitle";
            Assert.AreEqual(v.Title, "mytitle");
        }

        [TestMethod]
        public void CheckHeight() {
            VideoItem v = new VideoItem();
            Assert.AreEqual(v.Height, 0);
            v.Height = 100;
            Assert.AreEqual(v.Height, 100);
        }

        [TestMethod]
        public void CheckWidth() {
            VideoItem v = new VideoItem();
            Assert.AreEqual(v.Width, 0);
            v.Width = 200;
            Assert.AreEqual(v.Width, 200);
        }

        [TestMethod]
        public void TestAddingContentObjects() {
            VideoItem v = new VideoItem();
            Assert.AreEqual(v.ContentList.Count, 0);
            List<ContentObject> l = new List<ContentObject>();
            l.Add(new ContentObject());
            v.ContentList = l;
            Assert.AreEqual(v.ContentList.Count, 1);
        }

        [TestMethod]
        public void TestAddingThumbnails() {
            VideoItem v = new VideoItem();
            Assert.AreEqual(v.Thumbnails.Count, 0);
            List<Thumbnail> l = new List<Thumbnail>();
            l.Add(new Thumbnail());
            v.Thumbnails = l;
            Assert.AreEqual(v.Thumbnails.Count, 1);
        }

        [TestMethod]
        public void TestMetaValue() {
            VideoItem v = new VideoItem();
            v.AddMeta("somekey", "somevalue");
            string[] expected = new string[] {"somevalue"};
            Assert.AreEqual(v.GetMetaItem("somekey")[0], expected[0]);
        }

        [TestMethod]
        public void TestFullMeta() {
            VideoItem v = new VideoItem();
            v.AddMeta("somekey", "somevalue");
            Dictionary<string, string[]> rslt = v.getMeta();
            Dictionary<string, string[]> expected = new Dictionary<string, string[]>();
            string[] arr = {"somevalue"};
            expected.Add("somekey", arr);
            //Assert.AreSame(rslt, expected);
            Assert.AreEqual(rslt["somekey"][0], expected["somekey"][0]);
        }
    }
}
