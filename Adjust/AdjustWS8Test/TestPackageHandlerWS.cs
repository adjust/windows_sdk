using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS8
{
    [TestClass]
    public class TestPackageHandlerWS
    {
        private static TestPackageHandler TestPackageHandler;

        [ClassInitialize]
        public static void InitializeTestPackageHandlerWS(TestContext testContext)
        {
            TestPackageHandler = new TestPackageHandler(new UtilWS8(), new AssertTestWS8());
        }

        [TestInitialize]
        public void SetUp() { TestPackageHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestPackageHandler.TearDown(); }

        [TestMethod]
        public void TestAddPackageWS8() { TestPackageHandler.TestAddPackage(); }

        [TestMethod]
        public void TestSendFirstWS8() { TestPackageHandler.TestSendFirst(); }

        [TestMethod]
        public void TestSendNextWS8() { TestPackageHandler.TestSendNext(); }

        [TestMethod]
        public void TestCloseFirstPackageWS8() { TestPackageHandler.TestCloseFirstPackage(); }

        [TestMethod]
        public void TestCallsWS8() { TestPackageHandler.TestCalls(); }
    }
}