using AdjustSdk.Pcl;
using AdjustSdk.Pcl.Test;
using AdjustSdk.Test.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.WS.Test
{
    [TestClass]
    public class TestPackageHandlerWS
    {
        private MockLogger MockLogger;
        private MockRequestHandler MockRequestHandler;
        private UtilWS UtilWS;

        [TestInitialize]
        public void SetUp()
        {
            UtilWS = new UtilWS();

            MockLogger = new MockLogger();
            AdjustFactory.Logger = MockLogger;

            MockRequestHandler = new MockRequestHandler(MockLogger);
            AdjustFactory.SetRequestHandler(MockRequestHandler);
        }

        public void TearDown()
        {
            AdjustFactory.SetRequestHandler(null);
            AdjustFactory.Logger = null;
        }

        [TestMethod]
        public void TestFirstPackageWS()
        {
            // deleting previously created package queue file to make a new queue
            MockLogger.Test("Was the package queue file deleted? {0}", Util.DeleteFile("AdjustIOPackageQueue"));

            // initialize the package handler
            var packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            // enable sending packages to request handler
            packageHandler.ResumeSending();

            // build a package
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = UtilWS.GetUserAgent(),
                ClientSdk = UtilWS.ClientSdk,
            };
            var sessionPackage = packageBuilder.BuildSessionPackage();

            // and add it to the queue
            packageHandler.AddPackage(sessionPackage);

            MockRequestHandler.SetPackageHandler(packageHandler);

            // send the first package
            packageHandler.SendFirstPackage();

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            UtilWS.Sleep(1000).Wait();

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOPackageQueue (not found)"),
                MockLogger.ToString());

            // check that added first package to a previously empty queue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Added package 1 (session)"),
                MockLogger.ToString());

            // verify that the package queue is written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 1 packages"),
                MockLogger.ToString());

            // request handler should have receive the package
            Assert.IsTrue(MockLogger.DeleteTestUntil("RequestHandler SendPackage"),
                MockLogger.ToString());

            // request should have responded back to send the next package
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 0 packages"),
                MockLogger.ToString());
        }
    }
}