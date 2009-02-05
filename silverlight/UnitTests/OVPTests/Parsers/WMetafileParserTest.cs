using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;

namespace MediaFrameworkTests.Parsers
{
    [TestClass]
    public class WMetafileParserTest
    {
        [TestMethod]
        public void Test_new_properties_are_null()
        {
            WMetafileParser wvx = g();
            Assert.IsNull(wvx.Author);
            Assert.IsNull(wvx.ImageURL);
            Assert.AreEqual(wvx.Count, 0);
            Assert.IsNull(wvx.Title);
        }

        [TestMethod]
        public void Test_load() {
            WMetafileParser wvx = g();
            wvx.load(getstream());
            Assert.AreEqual(wvx.Title, "my title");
            Assert.AreEqual(wvx.Author, "some author");
            Assert.AreEqual(wvx.Count, 1);
        }

        /// <summary>
        /// Just a quick shortcut so we don't have to type the url a lot.
        /// </summary>
        /// <returns>A WMetafileParser object</returns>
        protected WMetafileParser g() {
            return new WMetafileParser(new Uri("http://framework.streamos.com/wmedia/framework/file.wvx"));
        }

        /// <summary>
        /// Another shortcut so we don't have to form up a stream of a wvx
        /// </summary>
        /// <returns>standard wvx in a stream</returns>
        protected static Stream getstream() {
            return new System.IO.MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(
                "<asx version=\"3.0\"><title>my title</title><author>some author</author><copyright>2008</copyright><entry><title>some title</title><author>some author</author><copyright>2008</copyright><ref href=\"mms://SERVER/FILE.wmv?qry=qstring\"/></entry></asx>"));
        }
    }
}
