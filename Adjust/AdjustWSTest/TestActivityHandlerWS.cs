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
            TestActivityHandler = new TestActivityHandler(new UtilWS(), new AssertTestPlatform());
        }

        [TestInitialize]
        public void SetUp() { TestActivityHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWS() { TestActivityHandler.TestFirstSession("wstore3.3.0"); }

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
    }
}