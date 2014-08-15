using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WP81
{
    [TestClass]
    public class TestActivityHandlerWP
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
        public void TestFirstSessionWS() { TestActivityHandler.TestFirstSession("wphone813.4.0"); }

        [TestMethod]
        public void TestSessionsWS() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestEventsBufferedWS() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWS() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TestChecksWS() { TestActivityHandler.TestChecks(); }

        [TestMethod]
        public void TestDisable() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrl() { TestActivityHandler.TestOpenUrl(); }

        [TestMethod]
        public void TestUserAgent() { TestActivityHandler.TestUserAgent(); }
    }
}