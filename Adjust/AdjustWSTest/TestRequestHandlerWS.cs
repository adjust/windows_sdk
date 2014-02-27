using AdjustSdk.Pcl;
using AdjustSdk.Pcl.Test;
using AdjustSdk.Test.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.WS.Test
{
    [TestClass]
    public class TestRequestHandlerWS
    {
        private MockLogger MockLogger;
        private RequestHandler RequestHandler;
        private MockPackageHandler MockPackageHandler;
        private MockHttpMessageHandler MockHttpMessageHandler;
        private ActivityPackage SessionPackage;
        private UtilWS UtilWS;

        [TestInitialize]
        public void SetUp()
        {
            UtilWS = new UtilWS();

            MockLogger = new MockLogger();
            AdjustFactory.Logger = MockLogger;

            MockHttpMessageHandler = new MockHttpMessageHandler(MockLogger);
            AdjustFactory.SetHttpMessageHandler(MockHttpMessageHandler);

            MockPackageHandler = new MockPackageHandler(MockLogger);
            RequestHandler = new RequestHandler(MockPackageHandler);

            var packageBuilder = new PackageBuilder()
            {
                UserAgent = UtilWS.GetUserAgent(),
                ClientSdk = UtilWS.ClientSdk,
            };
            SessionPackage = packageBuilder.BuildSessionPackage();
        }

        [TestCleanup]
        public void TearDown()
        {
            AdjustFactory.SetHttpMessageHandler(null);
            AdjustFactory.Logger = null;
        }

        [TestMethod]
        public void TestSendFirstPackageWS()
        {
            // send a default session package
            RequestHandler.SendPackage(SessionPackage);

            UtilWS.Sleep(1000).Wait();

            // the mocked http message handler is called
            Assert.IsTrue(MockLogger.DeleteTestUntil("HttpMessageHandler SendAsync"),
                MockLogger.ToString());

            // check the status received is ok
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "Tracked session"),
                MockLogger.ToString());

            // verify that the package handler was called to pass the response data
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler FinishedTrackingActivity"),
                MockLogger.ToString());

            // verify that the package handler was called to send the next package
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendNextPackage"),
                MockLogger.ToString());
        }

        [TestMethod]
        public void TestErrorSendPackageWS()
        {
            // set the response error different from the web server 500 or 501
            MockHttpMessageHandler.ResponseStatusCode = HttpStatusCode.BadRequest;
            MockHttpMessageHandler.ResponseMessage = "Test BadRequest";

            // send a default session package
            RequestHandler.SendPackage(SessionPackage);

            UtilWS.Sleep(1000).Wait();

            // the mocked http message handler is called
            Assert.IsTrue(MockLogger.DeleteTestUntil("HttpMessageHandler SendAsync"),
                MockLogger.ToString());

            // check that the error response was received
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to track session. (400). Will retry later"),
                MockLogger.ToString());

            // verify that the PackageHandler was called with the response data
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler FinishedTrackingActivity"),
                MockLogger.ToString());

            // verify that the PackageHandler was called to retry the package
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler CloseFirstPackage"),
                MockLogger.ToString());
        }
    }
}