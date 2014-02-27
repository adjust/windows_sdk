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

        [TestCleanup]
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

            // set the mock request handler to respond to the package handler
            MockRequestHandler.SetPackageHandler(packageHandler);

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

            // create a package handler to simulate a new launch
            packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            UtilWS.Sleep(1000).Wait();

            // check that it reads the empty queue file
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler read 0 packages"),
                MockLogger.ToString());
        }

        [TestMethod]
        public void TestPauseWS()
        {
            // initialize the package handler
            var packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            // set the mock request handler to respond to the package handler
            MockRequestHandler.SetPackageHandler(packageHandler);

            // disable sending packages
            packageHandler.PauseSending();

            // build a package
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = UtilWS.GetUserAgent(),
                ClientSdk = UtilWS.ClientSdk,
            };
            var sessionPackage = packageBuilder.BuildSessionPackage();

            // and add it to the queue
            packageHandler.AddPackage(sessionPackage);

            // send the first package
            packageHandler.SendFirstPackage();

            UtilWS.Sleep(1000).Wait();

            // check that the package handler is paused
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler is paused"),
                MockLogger.ToString());
        }

        [TestMethod]
        public void TestMultiplePackages()
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

            // and add it 3 times to the queue
            packageHandler.AddPackage(sessionPackage);
            packageHandler.AddPackage(sessionPackage);
            packageHandler.AddPackage(sessionPackage);

            // send the first two packages without closing the first
            packageHandler.SendFirstPackage();
            packageHandler.SendFirstPackage();

            UtilWS.Sleep(1000).Wait();

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOPackageQueue (not found)"),
                MockLogger.ToString());

            // check that added the 3 session packages
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Added package 3 (session)"),
                MockLogger.ToString());

            // verify that the 3 packages from the queue are written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 3 packages"),
                MockLogger.ToString());

            // check that the package handler was already sending one package when a second one was tried
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Verbose, "Package handler is already sending"),
                MockLogger.ToString());

            // create a package handler to simulate a new launch
            packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            UtilWS.Sleep(1000).Wait();

            // check that it reads the 3 packages from the file to the queue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler read 3 packages"),
                MockLogger.ToString());
        }
    }
}