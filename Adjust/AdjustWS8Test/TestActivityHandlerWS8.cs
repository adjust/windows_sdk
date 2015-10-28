using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS8
{
    [TestClass]
    public class TestActivityHandlerWS8
    {
        private static TestActivityHandler TestActivityHandler;

        [ClassInitialize]
        public static void InitializeTestActivityHandlerWS8(TestContext testContext)
        {
            TestActivityHandler = new TestActivityHandler(new UtilWS8(), new AssertTestWS8(), TargetPlatform.wstore80);
        }

        [TestInitialize]
        public void SetUp() { TestActivityHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWS8() { TestActivityHandler.TestFirstSession(); }

        [TestMethod]
        public void TestEventsBufferedWS8() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWS8() {TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TesChecksWS8() { TestActivityHandler.testChecks(); }

        [TestMethod]
        public void TestSessionsWS8() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestDisableWS8() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWS8() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestFinishedTrackingActivityWS8() { TestActivityHandler.TestFinishedTrackingActivity(); }

        [TestMethod]
        public void TestUpdateAttributionWS8() { TestActivityHandler.TestUpdateAttribution(); }

        [TestMethod]
        public void TestOfflineModeWS8() { TestActivityHandler.TestOfflineMode(); }

        [TestMethod]
        public void TestCheckAttributionStateWS8() { TestActivityHandler.TestCheckAttributionState(); }

        [TestMethod]
        public void TestTimerWS8() { TestActivityHandler.TestTimer(); }
    }
}