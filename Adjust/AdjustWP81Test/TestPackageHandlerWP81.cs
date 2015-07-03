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
        /*
        [TestCleanup]
        public void TearDown() { TestPackageHandler.TearDown(); }

        [TestMethod]
        public void TestFirstPackageWP81() { TestPackageHandler.TestFirstPackage(); }

        [TestMethod]
        public void TestPauseWP81() { TestPackageHandler.TestPause(); }

        [TestMethod]
        public void TestMultiplePackagesWP81() { TestPackageHandler.TestMultiplePackages(); }
         * */
    }
}
