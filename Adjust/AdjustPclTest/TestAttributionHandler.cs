using AdjustSdk.Pcl;
using System.Collections.Generic;
using System.Net.Http;

namespace AdjustTest.Pcl
{
    public class TestAttributionHandler : TestTemplate
    {
        private MockActivityHandler MockActivityHandler { get; set; }
        private MockHttpMessageHandler MockHttpMessageHandler { get; set; }
        private ActivityPackage AttributionPackage { get; set; }
        private TargetPlatform TargetPlatform { get; set; }

        public TestAttributionHandler(IDeviceUtil deviceUtil, IAssert assert, TargetPlatform targetPlatform)
            : base(deviceUtil, assert)
        {
            TargetPlatform = targetPlatform;
            TestActivityPackage.Assert = Assert;
            TestActivityPackage.TargetPlatform = TargetPlatform;
        }

        public override void SetUp()
        {
            base.SetUp();

            MockActivityHandler = new MockActivityHandler(MockLogger);
            MockHttpMessageHandler = new MockHttpMessageHandler(MockLogger);

            AdjustFactory.Logger = MockLogger;
            AdjustFactory.SetActivityHandler(MockActivityHandler);
            AdjustFactory.SetHttpMessageHandler(MockHttpMessageHandler);

            AttributionPackage = GetAttributionPackage();
        }

        public override void TearDown()
        {
            AdjustFactory.SetActivityHandler(null);
            AdjustFactory.SetHttpMessageHandler(null);
            AdjustFactory.Logger = null;
        }

        private ActivityPackage GetAttributionPackage()
        {
            MockAttributionHandler mockAttributionHandler = new MockAttributionHandler(MockLogger);
            MockPackageHandler mockPackageHandler = new MockPackageHandler(MockLogger);

            AdjustFactory.SetAttributionHandler(mockAttributionHandler);
            AdjustFactory.SetPackageHandler(mockPackageHandler);

            // start activity handler with config
            ActivityHandler activityHandler = UtilTest.GetActivityHandler(MockLogger, IDeviceUtil);
            
            ActivityPackage attributionPackage = activityHandler.GetAttributionPackage();
            
            TestActivityPackage attributionPackageTest = new TestActivityPackage(attributionPackage);

            attributionPackageTest.TestAttributionPackage();
            
            MockLogger.Reset();
            
            return attributionPackage;
        }

        public void TestAskAttribution()
        {
            AttributionHandler attributionHandler = GetAttributionHandler(startPaused: false, hasDelegate: true);

            // test null client
            NullClientTest(attributionHandler);

            // test client exception
            ClientExceptionTest(attributionHandler);

            // test wrong json response
            WrongJsonTest(attributionHandler);

            // test empty response
            EmptyJsonResponseTest(attributionHandler);

            // test server error
            ServerErrorTest(attributionHandler);

            // test ok response with message
            OkMessageTest(attributionHandler);
        }

        public void TestCheckAttribution()
        {
            AttributionHandler attributionHandler = new AttributionHandler(
                activityHandler: MockActivityHandler,
                attributionPackage: AttributionPackage, 
                startPaused: false, 
                hasDelegate: true);

            var response = "Response: {{ \"attribution\" : " +
                    "{{\"tracker_token\" : \"ttValue\" , " +
                    "\"tracker_name\"  : \"tnValue\" , " +
                    "\"network\"       : \"nValue\" , " +
                    "\"campaign\"      : \"cpValue\" , " +
                    "\"adgroup\"       : \"aValue\" , " +
                    "\"creative\"      : \"ctValue\" , " +
                    "\"click_label\"   : \"clValue\" }} }}";

            CallCheckAttributionWithGet(attributionHandler, ResponseType.ATTRIBUTION, response);

            // check attribution was called without ask_in
            Assert.Test("ActivityHandler UpdateAttribution, tt:ttValue tn:tnValue net:nValue cam:cpValue adg:aValue cre:ctValue cl:clValue");

            // updated set askingAttribution to false
            Assert.Test("ActivityHandler SetAskingAttribution, False");

            // it did not update to true
            Assert.NotTest("ActivityHandler SetAskingAttribution, True");

            // and waiting for query
            Assert.NotDebug("Waiting to query attribution");
        }

        public void TestAskIn()
        {
            AttributionHandler attributionHandler = new AttributionHandler(
                activityHandler: MockActivityHandler,
                attributionPackage: AttributionPackage,
                startPaused: false,
                hasDelegate: true);

            var response = "Response: {{ \"ask_in\" : 4000 }}";

            CallCheckAttributionWithGet(attributionHandler, ResponseType.ASK_IN, response);

            // change the response to avoid a cycle;
            MockHttpMessageHandler.ResponseType = ResponseType.MESSAGE;

            // check attribution was called with ask_in
            Assert.NotTest("ActivityHandler UpdateAttribution");

            // it did update to true
            Assert.Test("ActivityHandler SetAskingAttribution, True");

            // and waited to for query
            Assert.Debug("Waiting to query attribution in 4000 milliseconds");

            DeviceUtil.Sleep(2000);

            var askInJsonResponse = new Dictionary<string, string>{{ "ask_in", "5000" }};

            attributionHandler.CheckAttribution(askInJsonResponse);

            DeviceUtil.Sleep(3000);

            // it did update to true
            Assert.Test("ActivityHandler SetAskingAttribution, True");

            // and waited to for query
            Assert.Debug("Waiting to query attribution in 5000 milliseconds");

            // it was been waiting for 1000 + 2000 + 3000 = 6 seconds
            // check that the mock http client was not called because the original clock was reseted
            Assert.NotTest("HttpMessageHandler SendAsync");

            // check that it was finally called after 6 seconds after the second ask_in
            DeviceUtil.Sleep(3000);

            OkMessageTestLogs();

            RequestTest(MockHttpMessageHandler.HttpRequestMessage);
        }

        public void TestPause()
        {
            AttributionHandler attributionHandler = new AttributionHandler(
                activityHandler: MockActivityHandler,
                attributionPackage: AttributionPackage,
                startPaused: true,
                hasDelegate: true);

            MockHttpMessageHandler.ResponseType = ResponseType.MESSAGE;

            attributionHandler.AskAttribution();

            DeviceUtil.Sleep(1000);

            // check that the activity handler is paused
            Assert.Debug("Attribution handler is paused");

            // and it did not call the http client
            Assert.Null(MockHttpMessageHandler.HttpRequestMessage);

            Assert.NotTest("HttpMessageHandler SendAsync");
        }

        public void TestWithoutListener()
        {
            AttributionHandler attributionHandler = new AttributionHandler(
                activityHandler: MockActivityHandler,
                attributionPackage: AttributionPackage,
                startPaused: false,
                hasDelegate: false);

            MockHttpMessageHandler.ResponseType = ResponseType.MESSAGE;

            attributionHandler.AskAttribution();

            DeviceUtil.Sleep(1000);

            // check that the activity handler is not paused
            Assert.NotDebug("Attribution handler is paused");

            // but it did not call the http client
            Assert.Null(MockHttpMessageHandler.HttpRequestMessage);

            Assert.NotTest("HttpMessageHandler SendAsync");
        }

        private void CallCheckAttributionWithGet(AttributionHandler attributionHandler,
                                         ResponseType responseType,
                                         string response)
        {
            StartGetAttributionTest(attributionHandler, responseType);

            // the response logged
            Assert.Verbose(response);
        }


        private void NullClientTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler: attributionHandler, responseType: ResponseType.NULL);

            // check error
            Assert.Error("Failed to get attribution (Object reference not set to an instance of an object.)");

            // check response was not logged
            Assert.NotVerbose("Response");
        }

        private void ClientExceptionTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.CLIENT_PROTOCOL_EXCEPTION);

            // check the client error
            Assert.Error("Failed to get attribution (testResponseError)");
        }

        private void WrongJsonTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.WRONG_JSON);

            Assert.Verbose("Response: not a json response");

            Assert.Error("Failed to parse json response (Unexpected character encountered while parsing");
        }

        private void EmptyJsonResponseTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.EMPTY_JSON);

            Assert.Verbose("Response: {{ }}");

            Assert.Info("No message found");

            // check attribution was called without ask_in
            Assert.Test("ActivityHandler UpdateAttribution, Null");

            Assert.Test("ActivityHandler SetAskingAttribution, False");
        }

        private void ServerErrorTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.INTERNAL_SERVER_ERROR);

            // the response logged
            Assert.Verbose("Response: {{ \"message\": \"testResponseError\"}}");

            // the message in the response
            Assert.Error("testResponseError");

            // check attribution was called without ask_in
            Assert.Test("ActivityHandler UpdateAttribution, Null");

            Assert.Test("ActivityHandler SetAskingAttribution, False");
        }

        private void OkMessageTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.MESSAGE);

            OkMessageTestLogs();
        }

        private void OkMessageTestLogs()
        {
            // the response logged
            Assert.Verbose("Response: {{ \"message\": \"response OK\"}}");

            // the message in the response
            Assert.Info("response OK");

            // check attribution was called without ask_in
            Assert.Test("ActivityHandler UpdateAttribution, Null");

            Assert.Test("ActivityHandler SetAskingAttribution, False");
        }


        private void StartGetAttributionTest(AttributionHandler attributionHandler, ResponseType responseType)
        {
            MockHttpMessageHandler.ResponseType = responseType;

            attributionHandler.AskAttribution();

            DeviceUtil.Sleep(1000);

            RequestTest(MockHttpMessageHandler.HttpRequestMessage);

            Assert.Test("HttpMessageHandler SendAsync");
        }

        private void RequestTest(HttpRequestMessage request)
        {
            if (request == null) return;
                        
            var uri = request.RequestUri;

            Assert.AreEqual("https", uri.Scheme);

            Assert.AreEqual("app.adjust.com", uri.Authority);

            Assert.AreEqual(HttpMethod.Get, request.Method);
            
            //TODO what else can/should be tested?
        }

        private AttributionHandler GetAttributionHandler(bool startPaused = false, bool hasDelegate = false)
        {
            return new AttributionHandler(activityHandler: MockActivityHandler, attributionPackage: AttributionPackage,
                startPaused: startPaused, hasDelegate: hasDelegate);
        }
    }
}
