using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.UAP10
{
    [TestClass]
    public class TestRequestHandlerUAP10
    {
        private static TestRequestHandler TestRequestHandler;

        [ClassInitialize]
        public static void InitializeTestRequestHandlerUAP10(TestContext testContext)
        {
            TestRequestHandler = new TestRequestHandler(new UtilUAP10(), new AssertTestUAP10());
        }

        [TestInitialize]
        public void SetUp() { TestRequestHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }

        [TestMethod]
        public void TestSendUAP10() { TestRequestHandler.TestSend(); }

    }
}
