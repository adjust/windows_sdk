using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS
{
    [TestClass]
    public class TestRequestHandlerWS
    {
        private static TestRequestHandler TestRequestHandler;

        [ClassInitialize]
        public static void InitializeTestRequestHandlerWS(TestContext testContext)
        {
            TestRequestHandler = new TestRequestHandler(new UtilWS(), new AssertTestWS());
        }

        [TestInitialize]
        public void SetUp() { TestRequestHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }
    
        [TestMethod]
        public void TestSendWS() { TestRequestHandler.TestSend(); }

        /*
        [TestMethod]
        public void TestSendFirstPackageWS() { TestRequestHandler.TestSendFirstPackage(); }

        [TestMethod]
        public void TestErrorSendPackageWS() { TestRequestHandler.TestErrorSendPackage(); }
         * */
    }
}