using AdjustSdk.Pcl;
using AdjustSdk.Pcl.Test;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.WS.Test
{
    [TestClass]
    public class TestActivityHandlerWS
    {
        private MockLogger MockLogger;
        private MockPackageHandler MockPackageHandler;
        private UtilWS UtilWS;

        [TestInitialize]
        public void SetUp()
        {
            UtilWS = new UtilWS();

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

        [TestMethod]
        public void TestFirstSessionWS()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", UtilWS);

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            UtilWS.Sleep(1000).Wait();

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOActivityState (not found)"),
                MockLogger.ToString());

            // whenever a session starts the package handler should be called to resume sending
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler ResumeSending"),
                MockLogger.ToString());

            // if the package was built, it is sent to the package handler
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler AddPackage"),
                MockLogger.ToString());

            // after added, the package handler is signaled to send the next package on the queue
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // verify that the activity state is written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state"),
                MockLogger.ToString());

            // ending the first session
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "First session"),
                MockLogger.ToString());

            UtilWS.Sleep(1000).Wait();
            // by this time also the timer should have fired
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // checking the default values of the first session package
            // should only have one package
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            var firstSessionPackage = MockPackageHandler.PackageQueue[0];

            // check the Sdk verion being tested
            Assert.AreEqual("wstore3.0.0", firstSessionPackage.ClientSdk,
                firstSessionPackage.ExtendedString());

            // the activity kind shoud be session
            Assert.AreEqual("session", ActivityKindUtil.ToString(firstSessionPackage.ActivityKind),
                firstSessionPackage.ExtendedString());

            // the path of the call should be /session
            Assert.AreEqual(@"/startup", firstSessionPackage.Path,
                firstSessionPackage.ExtendedString());

            var parameters = firstSessionPackage.Parameters;

            // session atributes
            // session count should be 1 because it's the first session
            Assert.IsTrue(IsParameterEqual(1, parameters, "session_count"),
                firstSessionPackage.ExtendedString());

            // subsession count should be -1, because we didn't any subsessions yet
            // because values < 0 are added discarded, it shouldn't be present on the parameters
            Assert.IsFalse(parameters.ContainsKey("subsession_count"),
                firstSessionPackage.ExtendedString());

            // session lenght should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("session_lenght"),
                firstSessionPackage.ExtendedString());

            // time spent should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("time_spent"),
                firstSessionPackage.ExtendedString());

            // created at TODO

            // last interval shoule be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("last_interval"),
                firstSessionPackage.ExtendedString());
        }

        private bool IsParameterEqual(int expected, Dictionary<string, string> parameter, string key)
        {
            string value;
            if (!parameter.TryGetValue(key, out value))
                return false;

            int actual;
            if (!Int32.TryParse(value, out actual))
                return false;

            return expected == actual;
        }
    }
}