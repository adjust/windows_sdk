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
        private ActivityPackage ClickPackage { get; set; }
        
        public TestRequestHandler(DeviceUtil deviceUtil, IAssert assert)
            : base(deviceUtil, assert)
        { }

        public override void SetUp()
        {
            base.SetUp();

            AdjustFactory.Logger = MockLogger;

            var activityHandler = UtilTest.GetActivityHandler(MockLogger, DeviceUtil);

            MockLogger.Reset();

            ClickPackage = UtilTest.CreateClickPackage(activityHandler, "");

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

        public void TestSend()
        {
            NullResponseTest();

            ClientExceptionTest();

            ServerErrorTest();

            WrongJsonTest();

            EmptyJsonTest();

            MessageTest();
        }

        private void NullResponseTest()
        {
            MockHttpMessageHandler.ResponseType = ResponseType.NULL;

            RequestHandler.SendPackage(ClickPackage);
            DeviceUtil.Sleep(1000);

            Assert.Test("HttpMessageHandler SendAsync");

            Assert.Test("PackageHandler CloseFirstPackage");
        }

        private void ClientExceptionTest() 
        {
            MockHttpMessageHandler.ResponseType = ResponseType.CLIENT_PROTOCOL_EXCEPTION;

            RequestHandler.SendPackage(ClickPackage);
            DeviceUtil.Sleep(1000);

            Assert.Test("HttpMessageHandler SendAsync");

            Assert.Error("Failed to track click. (testResponseError, Status code: Null). Will retry later");

            Assert.Test("PackageHandler CloseFirstPackage");
        }

        private void ServerErrorTest()
        {
            MockHttpMessageHandler.ResponseType = ResponseType.INTERNAL_SERVER_ERROR;

            RequestHandler.SendPackage(ClickPackage);
            DeviceUtil.Sleep(1000);

            Assert.Test("HttpMessageHandler SendAsync, responseType: INTERNAL_SERVER_ERROR");

            Assert.Verbose("Response: {{ \"message\": \"testResponseError\"}}");

            Assert.Error("testResponseError");

            Assert.Error("Failed to track click. (Status code: 500).");

            Assert.Test("PackageHandler FinishedTrackingActivity, [message, testResponseError]");

            Assert.Test("PackageHandler SendNextPackage");
        }

        private void WrongJsonTest()
        {
            MockHttpMessageHandler.ResponseType = ResponseType.WRONG_JSON;

            RequestHandler.SendPackage(ClickPackage);
            DeviceUtil.Sleep(2000);

            Assert.Test("HttpMessageHandler SendAsync, responseType: WRONG_JSON");

            Assert.Verbose("Response: not a json response");

            Assert.Error("Failed to parse json response (Unexpected character encountered while parsing");

            Assert.Test("PackageHandler CloseFirstPackage");
        }

        private void EmptyJsonTest()
        {
            MockHttpMessageHandler.ResponseType = ResponseType.EMPTY_JSON;

            RequestHandler.SendPackage(ClickPackage);
            DeviceUtil.Sleep(1000);

            Assert.Test("HttpMessageHandler SendAsync, responseType: EMPTY_JSON");

            Assert.Verbose("Response: {{ }}");

            Assert.Info("No message found");

            Assert.Test("PackageHandler FinishedTrackingActivity,");

            Assert.Test("PackageHandler SendNextPackage");
        }

        private void MessageTest()
        {
            MockHttpMessageHandler.ResponseType = ResponseType.MESSAGE;

            RequestHandler.SendPackage(ClickPackage);
            
            DeviceUtil.Sleep(1000);

            Assert.Test("HttpMessageHandler SendAsync, responseType: MESSAGE");

            Assert.Verbose("Response: {{ \"message\": \"response OK\"}}");

            Assert.Info("response OK");

            Assert.Test("PackageHandler FinishedTrackingActivity, [message, response OK]");

            Assert.Test("PackageHandler SendNextPackage");
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