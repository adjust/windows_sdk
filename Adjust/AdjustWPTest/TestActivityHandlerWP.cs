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
    public class TestActivityHandlerWP
    {
        private static TestActivityHandler TestActivityHandler;

        //[ClassInitialize]
        //public static void InitializeTestActivityHandlerWP(TestContext testContext)
        //{
        //    TestActivityHandler = new TestActivityHandler(new UtilWP(), new AssertTestTools());
        //}

        [TestInitialize]
        public void SetUp()
        {
            TestActivityHandler = new TestActivityHandler(new UtilWP(), new AssertTestTools());
            TestActivityHandler.SetUp();
        }

        [TestCleanup]
        public void TearDown() { TestActivityHandler.TearDown(); }

        [TestMethod]
        public void TestFirstSessionWP() { TestActivityHandler.TestFirstSession("wphone3.3.0"); }

        [TestMethod]
        public void TestSessionsWP() { TestActivityHandler.TestSessions(); }

        [TestMethod]
        public void TestEventsBufferedWP() { TestActivityHandler.TestEventsBuffered(); }

        [TestMethod]
        public void TestEventsNotBufferedWP() { TestActivityHandler.TestEventsNotBuffered(); }

        [TestMethod]
        public void TestChecksWP() { TestActivityHandler.TestChecks(); }

        [TestMethod]
        public void TestDisable() { TestActivityHandler.TestDisable(); }

        [TestMethod]
        public void TestOpenUrl() { TestActivityHandler.TestOpenUrl(); }
    }
}