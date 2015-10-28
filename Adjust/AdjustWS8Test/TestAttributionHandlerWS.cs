using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.WS8
{
    [TestClass]
    public class TestAttributionHandlerWS
    {
        private static TestAttributionHandler TestAttributionHandler;

        [ClassInitialize]
        public static void InitializeTestAttributionHandlerWS(TestContext testContext)
        {
            TestAttributionHandler = new TestAttributionHandler(new UtilWS8(), new AssertTestWS8(), TargetPlatform.wstore80);
        }

        [TestInitialize]
        public void SetUp() { TestAttributionHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestAttributionHandler.TearDown(); }

        [TestMethod]
        public void TestAskAttributionWS8() { TestAttributionHandler.TestAskAttribution(); }
        
        [TestMethod]
        public void TestCheckAttributionWS8() { TestAttributionHandler.TestCheckAttribution(); }

        [TestMethod]
        public void TestAskInWS8() { TestAttributionHandler.TestAskIn(); }

        [TestMethod]
        public void TestPauseWS8() { TestAttributionHandler.TestPause(); }

        [TestMethod]
        public void TestWithoutListenerWS8() { TestAttributionHandler.TestWithoutListener(); }
    }
}
