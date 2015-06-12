using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS
{
    [TestClass]
    public class TestActivityHandlerWS
    {
        private static TestActivityHandler TestActivityHandler;

        [ClassInitialize]
        public static void InitializeTestActivityHandlerWS(TestContext testContext)
        {
            TestActivityHandler = new TestActivityHandler(new UtilWS(), new AssertTestWS(), TargetPlatform.wstore);
        }

        [TestInitialize]
        public void SetUp() { TestActivityHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWS() { TestActivityHandler.TestFirstSession(); }

        [TestMethod]
        public void TestEventsBufferedWS() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWS() {TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TesChecksWS() { TestActivityHandler.testChecks(); }

        [TestMethod]
        public void TestSessionsWS() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestDisableWS() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWS() { TestActivityHandler.TestOpenUrl(); }


        [TestMethod]
        public void TestFinishedTrackingActivityWS() { TestActivityHandler.TestFinishedTrackingActivity(); }

        /*
         * 
        [TestMethod]
        public void TestSessionsWS() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestEventsNotBufferedWS() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TestChecksWS() { TestActivityHandler.TestChecks(); }

        [TestMethod]
        public void TestDisableWS() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWS() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestUserAgentWS() { TestActivityHandler.TestUserAgent(); }
         * */
    }
}