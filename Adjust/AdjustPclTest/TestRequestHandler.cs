using AdjustSdk;
using AdjustSdk.Pcl;
using System.Net;

namespace AdjustTest.Pcl
{
    public class TestRequestHandler : TestTemplate
    {
        private RequestHandler RequestHandler { get; set; }
        private MockPackageHandler MockPackageHandler { get; set; }
        private MockHttpMessageHandler MockHttpMessageHandler { get; set; }
        private ActivityPackage SessionPackage { get; set; }
        

        public TestRequestHandler(DeviceUtil deviceUtil, IAssert assert)
            : base(deviceUtil, assert)
        { }

        public override void SetUp()
        {
            base.SetUp();

            MockHttpMessageHandler = new MockHttpMessageHandler(MockLogger);
            AdjustFactory.SetHttpMessageHandler(MockHttpMessageHandler);

            MockPackageHandler = new MockPackageHandler(MockLogger);
            RequestHandler = new RequestHandler(MockPackageHandler);
            /*
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = DeviceUtil.GetUserAgent(),
                ClientSdk = DeviceUtil.ClientSdk,
            };
            SessionPackage = packageBuilder.BuildSessionPackage();
             * */
        }

        public override void TearDown()
        {
            AdjustFactory.SetHttpMessageHandler(null);
            AdjustFactory.Logger = null;
        }
        /*
        public void TestSendFirstPackage()
        {
            // send a default session package
            RequestHandler.SendPackage(SessionPackage);

            DeviceUtil.Sleep(1000);

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

        public void TestErrorSendPackage()
        {
            // set the response error different from the web server 500 or 501
            MockHttpMessageHandler.ResponseStatusCode = HttpStatusCode.BadRequest;
            MockHttpMessageHandler.ResponseMessage = "Test BadRequest";

            // send a default session package
            RequestHandler.SendPackage(SessionPackage);

            DeviceUtil.Sleep(1000);

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
         * */
    }
}