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
    public class TestAttributionHandlerUAP10
    {
        private static TestAttributionHandler TestAttributionHandler;

        [ClassInitialize]
        public static void InitializeTestAttributionHandlerUAP10(TestContext testContext)
        {
            TestAttributionHandler = new TestAttributionHandler(new UtilUAP10(), new AssertTestUAP10(), TargetPlatform.wuap);
        }

        [TestInitialize]
        public void SetUp() { TestAttributionHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestAttributionHandler.TearDown(); }

        [TestMethod]
        public void TestAskAttributionUAP10() { TestAttributionHandler.TestAskAttribution(); }

        [TestMethod]
        public void TestCheckAttributionUAP10() { TestAttributionHandler.TestCheckAttribution(); }

        [TestMethod]
        public void TestAskInUAP10() { TestAttributionHandler.TestAskIn(); }

        [TestMethod]
        public void TestPauseUAP10() { TestAttributionHandler.TestPause(); }

        [TestMethod]
        public void TestWithoutListenerUAP10() { TestAttributionHandler.TestWithoutListener(); }
    }
}
