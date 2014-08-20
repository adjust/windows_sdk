using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WP81
{
    [TestClass]
    public class TestRequestHandlerWP81
    {
        private static TestRequestHandler TestRequestHandler;

        [ClassInitialize]
        public static void InitializeTestRequestHandlerWS(TestContext testContext)
        {
            TestRequestHandler = new TestRequestHandler(new UtilWP81(), new AssertTestWP81());
        }

        [TestInitialize]
        public void SetUp() { TestRequestHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }

        [TestMethod]
        public void TestSendFirstPackageWP81() { TestRequestHandler.TestSendFirstPackage(); }

        [TestMethod]
        public void TestErrorSendPackageWP81() { TestRequestHandler.TestErrorSendPackage(); }
    }
}