using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WP81
{
    [TestClass]
    public class TestActivityHandlerWP81
    {
        private static TestActivityHandler TestActivityHandler;

        [ClassInitialize]
        public static void InitializeTestActivityHandlerWS(TestContext testContext)
        {
            TestActivityHandler = new TestActivityHandler(new UtilWP81(), new AssertTestWP81());
        }

        [TestInitialize]
        public void SetUp() { TestActivityHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWP81() { TestActivityHandler.TestFirstSession("wphone81-3.5.0"); }

        [TestMethod]
        public void TestSessionsWP81() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestEventsBufferedWP81() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWP81() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TestChecksWP81() { TestActivityHandler.TestChecks(); }

        [TestMethod]
        public void TestDisableWP81() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrlWP81() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestUserAgentWP81() { TestActivityHandler.TestUserAgent(); }
    }
}