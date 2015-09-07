using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
namespace AdjustTest.WP81
{
    [TestClass]
    public class TestPackageHandlerWP81
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
        public void TestAddPackageWP81() { TestPackageHandler.TestAddPackage(); }

        [TestMethod]
        public void TestSendFirstWP81() { TestPackageHandler.TestSendFirst(); }

        [TestMethod]
        public void TestSendNextWP81() { TestPackageHandler.TestSendNext(); }

        [TestMethod]
        public void TestCloseFirstPackageWP81() { TestPackageHandler.TestCloseFirstPackage(); }

        [TestMethod]
        public void TestCallsWP81() { TestPackageHandler.TestCalls(); }
    }
}
