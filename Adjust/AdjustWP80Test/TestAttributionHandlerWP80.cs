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
    public class TestAttributionHandlerWP80
    {
        private static TestAttributionHandler TestAttributionHandler;

        //[ClassInitialize]
        //public static void InitializeTestActivityHandlerWP(TestContext testContext)
        //{
        //    TestActivityHandler = new TestActivityHandler(new UtilWP(), new AssertTestTools());
        //}

        [TestInitialize]
        public void SetUp()
        {
            TestAttributionHandler = new TestAttributionHandler(new UtilWP80(), new AssertTestWP80(), TargetPlatform.wphone80);
            TestAttributionHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestAttributionHandler.TearDown(); }

        [TestMethod]
        public void TestAskAttributionWP80() { TestAttributionHandler.TestAskAttribution(); }

        [TestMethod]
        public void TestCheckAttributionWP80() { TestAttributionHandler.TestCheckAttribution(); }

        [TestMethod]
        public void TestAskInWP80() { TestAttributionHandler.TestAskIn(); }

        [TestMethod]
        public void TestPauseWP80() { TestAttributionHandler.TestPause(); }

        [TestMethod]
        public void TestWithoutListenerWP80() { TestAttributionHandler.TestWithoutListener(); }
    }
}