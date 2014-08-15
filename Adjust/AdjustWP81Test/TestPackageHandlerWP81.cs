using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
namespace AdjustTest.WP81
{
    [TestClass]
    class TestPackageHandlerWP81
    {
        private static TestPackageHandler TestPackageHandler;

        [ClassInitialize]
        public static void InitializeTestPackageHandlerWS(TestContext testContext)
        {
            TestPackageHandler = new TestPackageHandler(new UtilWP81(), new AssertTestWP81());
        }

        [TestInitialize]
        public void SetUp() { TestPackageHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestPackageHandler.TearDown(); }

        [TestMethod]
        public void TestFirstPackageWS() { TestPackageHandler.TestFirstPackage(); }

        [TestMethod]
        public void TestPauseWS() { TestPackageHandler.TestPause(); }

        [TestMethod]
        public void TestMultiplePackages() { TestPackageHandler.TestMultiplePackages(); }
    }
}
