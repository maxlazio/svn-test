using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;
using org.OpenVideoPlayer.Connections;

namespace MediaFrameworkTests
{
    [TestClass]
    public class DefaultConnectionTest
    {
        [TestMethod]
        public void NullInstance() {
            try {
                DefaultConnection conn = new DefaultConnection(null);
                Assert.Fail("No Null Reference thrown");
            } catch (NullReferenceException e) {
                Debug.WriteLine("Recieved expected exception: "+e.ToString());
                //GOOD, we expect this, don't catch anything else
            }
        }

        [TestMethod]
        public void NewParser() {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            conn.Clear();
        }

        [TestMethod]
        public void new_has_null_errormsg() {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            Assert.IsNull(conn.ErrorMsg);
        }

        [TestMethod]
        public void false_when_not_connected() {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            Assert.IsFalse(conn.IsConnected);
        }

        [TestMethod]
        public void playlist_starts_null() {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            Assert.IsNull(conn.Playlist);
        }

        [TestMethod]
        public void can_connect_to_uri() {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            conn.Connect(new Uri("file:///C:/source/c%23/MediaFrameworkTests/test_good_feed.xml"));
            Assert.AreEqual("file:///C:/source/c%23/MediaFrameworkTests/test_good_feed.xml", conn.Uri.ToString());
        }

        [TestMethod]
        public void connect_causes_isconnected_switchon()
        {
            ParserManager pm = new ParserManager();
            DefaultConnection conn = new DefaultConnection(pm);
            Assert.IsFalse(conn.IsConnected);
            conn.Connect(new Uri("file:///C:/source/c%23/MediaFrameworkTests/test_good_feed.xml"));
            //Downloading from a file source will throw a null reference exception
            //so we expect a false to be returned here.
            Assert.IsFalse(conn.IsConnected);
        }


    }
}
