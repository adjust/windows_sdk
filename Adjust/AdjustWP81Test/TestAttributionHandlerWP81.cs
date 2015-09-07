using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WP81
{
    [TestClass]
    public class TestAttributionHandlerWP81
    {
        private static TestAttributionHandler TestAttributionHandler;

        [ClassInitialize]
        public static void InitializeTestAttributionHandlerWP81(TestContext testContext)
        {
            TestAttributionHandler = new TestAttributionHandler(new UtilWP81(), new AssertTestWP81(), TargetPlatform.wphone81);
        }

        [TestInitialize]
        public void SetUp() { TestAttributionHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestAttributionHandler.TearDown(); }

        [TestMethod]
        public void TestAskAttributionWP81() { TestAttributionHandler.TestAskAttribution(); }

        [TestMethod]
        public void TestCheckAttributionWP81() { TestAttributionHandler.TestCheckAttribution(); }

        [TestMethod]
        public void TestAskInWP81() { TestAttributionHandler.TestAskIn(); }

        [TestMethod]
        public void TestPauseWP81() { TestAttributionHandler.TestPause(); }

        [TestMethod]
        public void TestWithoutListenerWP81() { TestAttributionHandler.TestWithoutListener(); }
    }
}