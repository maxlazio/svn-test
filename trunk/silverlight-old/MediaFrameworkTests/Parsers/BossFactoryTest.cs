using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Parsers;

namespace MediaFrameworkTests.Parsers
{
    [TestClass]
    public class BossFactoryTest
    {
        [TestMethod]
        public void TestGetParser()
        {
            BossFactory b = new BossFactory();
            Assert.IsNull(b.getParser(S(), new Uri("http://test/file.xml")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.asx")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.wvx")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.wax")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.asf")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.wmv")));
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.wma")));
            //Check off case too
            Assert.IsNotNull(b.getParser(S(), new Uri("http://test/file.ASF")));
        }

        /// <summary>
        /// Lazy helper method to create a Stream
        /// </summary>
        /// <returns></returns>
        protected static Stream S() {
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes("THIS DOESN'T MATTER"));
        }
    }
}
