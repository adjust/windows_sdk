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
            TestActivityHandler = new TestActivityHandler(new UtilWP80(), new AssertTestWP80(), TargetPlatform.wphone80);
            TestActivityHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWP80() { TestActivityHandler.TestFirstSession(); }

        [TestMethod]
        public void TestEventsBufferedWP80() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWP80() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TesChecksWP80() { TestActivityHandler.testChecks(); }

        [TestMethod]
        public void TestSessionsWP80() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestDisableWP80() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWP80() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestFinishedTrackingActivityWP80() { TestActivityHandler.TestFinishedTrackingActivity(); }

        [TestMethod]
        public void TestUpdateAttributionWP80() { TestActivityHandler.TestUpdateAttribution(); }

        [TestMethod]
        public void TestOfflineModeWP80() { TestActivityHandler.TestOfflineMode(); }

        [TestMethod]
        public void TestCheckAttributionStateWP80() { TestActivityHandler.TestCheckAttributionState(); }

        [TestMethod]
        public void TestTimerWP80() { TestActivityHandler.TestTimer(); }
        /*
        [TestMethod]
        public void TestFirstSessionWP80() { TestActivityHandler.TestFirstSession("wphone80-3.5.1"); }

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
         * */
    }
}