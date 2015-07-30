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
    public class TestPackageHandlerWP80
    {
        private static TestPackageHandler TestPackageHandler;

        //[ClassInitialize]
        //public static void InitializeTestPackageHandlerWP(TestContext testContext)
        //{
        //    TestPackageHandler = new TestPackageHandler(new UtilWP(), new AssertTestTools());
        //}

        [TestInitialize]
        public void SetUp()
        {
            TestPackageHandler = new TestPackageHandler(new UtilWP80(), new AssertTestWP80());
            TestPackageHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestPackageHandler.TearDown(); }

        [TestMethod]
        public void TestAddPackageWP80() { TestPackageHandler.TestAddPackage(); }

        [TestMethod]
        public void TestSendFirstWP80() { TestPackageHandler.TestSendFirst(); }

        [TestMethod]
        public void TestSendNextWP80() { TestPackageHandler.TestSendNext(); }

        [TestMethod]
        public void TestCloseFirstPackageWP80() { TestPackageHandler.TestCloseFirstPackage(); }

        [TestMethod]
        public void TestCallsWP80() { TestPackageHandler.TestCalls(); }
    }
}