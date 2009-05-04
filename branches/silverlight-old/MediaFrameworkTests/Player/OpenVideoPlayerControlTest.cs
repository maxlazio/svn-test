using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.OpenVideoPlayer.Player;

namespace MediaFrameworkTests.Player
{
    [TestClass]
    public class OpenVideoPlayerControlTest
    {
        [TestMethod]
        public void TestCreation()
        {
            OpenVideoPlayerControl fixture = new OpenVideoPlayerControl();
            Assert.IsNotNull(fixture, "new OpenVideoPlayerControl evaluated to null");
            Assert.IsInstanceOfType(fixture, typeof(IMediaControl), "new OpenVideoPlayerControl evaluated to the wrong class type");
        }
    }
}
