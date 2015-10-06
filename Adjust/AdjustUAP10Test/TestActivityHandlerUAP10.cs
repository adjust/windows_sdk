using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.UAP10
{
    [TestClass]
    public class TestActivityHandlerUAP10
    {
        private static TestActivityHandler TestActivityHandler;

        [ClassInitialize]
        public static void InitializeTestActivityHandlerUAP10(TestContext testContext)
        {
            TestActivityHandler = new TestActivityHandler(new UtilUAP10(), new AssertTestUAP10(), TargetPlatform.wuap);
        }

        [TestInitialize]
        public void SetUp() { TestActivityHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionUAP10() { TestActivityHandler.TestFirstSession(); }

        [TestMethod]
        public void TestEventsBufferedUAP10() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedUAP10() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TesChecksUAP10() { TestActivityHandler.testChecks(); }

        [TestMethod]
        public void TestSessionsUAP10() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestDisableUAP10() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlUAP10() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestFinishedTrackingActivityUAP10() { TestActivityHandler.TestFinishedTrackingActivity(); }

        [TestMethod]
        public void TestUpdateAttributionUAP10() { TestActivityHandler.TestUpdateAttribution(); }

        [TestMethod]
        public void TestOfflineModeUAP10() { TestActivityHandler.TestOfflineMode(); }

        [TestMethod]
        public void TestCheckAttributionStateUAP10() { TestActivityHandler.TestCheckAttributionState(); }

        [TestMethod]
        public void TestTimerUAP10() { TestActivityHandler.TestTimer(); }
    }
}
