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
    public class TestPackageHandlerWP
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
            TestPackageHandler = new TestPackageHandler(new UtilWP(), new AssertTestWP());
            TestPackageHandler.SetUp();
        }

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