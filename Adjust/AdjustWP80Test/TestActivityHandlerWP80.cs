using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.WP
{
    [TestClass]
    public class TestActivityHandlerWP80
    {
        private static TestActivityHandler TestActivityHandler;

        //[ClassInitialize]
        //public static void InitializeTestActivityHandlerWP(TestContext testContext)
        //{
        //    TestActivityHandler = new TestActivityHandler(new UtilWP(), new AssertTestTools());
        //}

        [TestInitialize]
        public void SetUp()
        {
            TestActivityHandler = new TestActivityHandler(new UtilWP80(), new AssertTestWP80());
            TestActivityHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWP80() { TestActivityHandler.TestFirstSession("wphone80-3.5.0"); }

        [TestMethod]
        public void TestSessionsWP80() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestEventsBufferedWP80() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWP80() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TestChecksWP80() { TestActivityHandler.TestChecks(); }

        [TestMethod]
        public void TestDisableWP80() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWP80() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestUserAgentWP80() { TestActivityHandler.TestUserAgent(); }
    }
}