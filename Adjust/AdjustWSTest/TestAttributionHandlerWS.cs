using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.WS
{
    [TestClass]
    public class TestAttributionHandlerWS
    {
        private static TestAttributionHandler TestAttributionHandler;

        [ClassInitialize]
        public static void InitializeTestAttributionHandlerWS(TestContext testContext)
        {
            TestAttributionHandler = new TestAttributionHandler(new UtilWS(), new AssertTestWS(), TargetPlatform.wstore);
        }

        [TestInitialize]
        public void SetUp() { TestAttributionHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestAttributionHandler.TearDown(); }

        [TestMethod]
        public void TestAskAttributionWS() { TestAttributionHandler.TestAskAttribution(); }
    }
}
