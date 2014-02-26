using AdjustSdk.Pcl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AdjustSdk.Test
{
    [TestClass]
    public class TestActivityHandler
    {
        private MockLogger MockLogger;
        private MockPackageHandler MockPackageHandler;

        [TestInitialize]
        public void SetUp()
        {
            MockLogger = new MockLogger();
            AdjustFactory.Logger = MockLogger;

            MockPackageHandler = new MockPackageHandler(MockLogger);
            AdjustFactory.SetPackageHandler(MockPackageHandler);
        }

        [TestCleanup]
        public void TearDown()
        {
            AdjustFactory.SetPackageHandler(null);
            AdjustFactory.Logger = null;
        }

        public void TestFirstSession()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            var activityHandler = new ActivityHandler("123456789012", new MockDeviceUtil(MockLogger));

            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOActivityState (not found)"),
                MockLogger.ToString());
        }
    }
}