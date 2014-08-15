using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS
{
    [TestClass]
    public class TestPackageHandlerWS
    {
        private static TestPackageHandler TestPackageHandler;

        [ClassInitialize]
        public static void InitializeTestPackageHandlerWS(TestContext testContext)
        {
            TestPackageHandler = new TestPackageHandler(new UtilWS(), new AssertTestWS());
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