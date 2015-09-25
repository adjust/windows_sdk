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
    public class TestPackageHandlerUAP10
    {
        private static TestPackageHandler TestPackageHandler;

        [ClassInitialize]
        public static void InitializeTestPackageHandlerUAP10(TestContext testContext)
        {
            TestPackageHandler = new TestPackageHandler(new UtilUAP10(), new AssertTestUAP10());
        }

        [TestInitialize]
        public void SetUp() { TestPackageHandler.SetUp(); }

        [TestCleanup]
        public void TearDown() { TestPackageHandler.TearDown(); }

        [TestMethod]
        public void TestAddPackageUAP10() { TestPackageHandler.TestAddPackage(); }

        [TestMethod]
        public void TestSendFirstUAP10() { TestPackageHandler.TestSendFirst(); }

        [TestMethod]
        public void TestSendNextUAP10() { TestPackageHandler.TestSendNext(); }

        [TestMethod]
        public void TestCloseFirstPackageUAP10() { TestPackageHandler.TestCloseFirstPackage(); }

        [TestMethod]
        public void TestCallsUAP10() { TestPackageHandler.TestCalls(); }

    }
}
