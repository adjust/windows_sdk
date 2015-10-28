using AdjustSdk;
using AdjustTest.Pcl;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace AdjustTest.WS8
{
    [TestClass]
    public class TestRequestHandlerWS
    {
        private static TestRequestHandler TestRequestHandler;

        [ClassInitialize]
        public static void InitializeTestRequestHandlerWS(TestContext testContext)
        {
            TestRequestHandler = new TestRequestHandler(new UtilWS8(), new AssertTestWS8());
        }

        [TestInitialize]
        public void SetUp() { TestRequestHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }
    
        [TestMethod]
        public void TestSendWS8() { TestRequestHandler.TestSend(); }
    }
}