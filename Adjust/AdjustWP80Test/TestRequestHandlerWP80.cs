using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.WP
{
    [TestClass]
    public class TestRequestHandlerWP80
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
            TestRequestHandler = new TestRequestHandler(new UtilWP80(), new AssertTestWP80());
            TestRequestHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestRequestHandler.TearDown(); }

        [TestMethod]
        public void TestSendFirstPackageWP80() { TestRequestHandler.TestSendFirstPackage(); }

        [TestMethod]
        public void TestErrorSendPackageWP80() { TestRequestHandler.TestErrorSendPackage(); }
    }
}