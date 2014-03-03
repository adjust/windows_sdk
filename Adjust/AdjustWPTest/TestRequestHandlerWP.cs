using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustWPTest
{
    [TestClass]
    public class TestRequestHandlerWP
    {
        private static TestRequestHandler TestRequestHandler;

        //[ClassInitialize]
        //public static void InitializeTestRequestHandlerWP(TestContext testContext)
        //{
        //    TestRequestHandler = new TestRequestHandler(new UtilWP(), new AssertTestTools());
        //}

        [TestInitialize]
        public void SetUp()
        {
            TestRequestHandler = new TestRequestHandler(new UtilWP(), new AssertTestTools());
            TestRequestHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }

        [TestMethod]
        public void TestSendFirstPackageWS() { TestRequestHandler.TestSendFirstPackage(); }

        [TestMethod]
        public void TestErrorSendPackageWS() { TestRequestHandler.TestErrorSendPackage(); }
    }
}