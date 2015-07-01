using AdjustSdk.Pcl;
using System.Net.Http;

namespace AdjustTest.Pcl
{
    public class TestAttributionHandler : TestTemplate
    {
        private MockActivityHandler MockActivityHandler { get; set; }
        private MockHttpMessageHandler MockHttpMessageHandler { get; set; }
        private ActivityPackage AttributionPackage { get; set; }
        private TargetPlatform TargetPlatform { get; set; }

        public TestAttributionHandler(DeviceUtil deviceUtil, IAssert assert, TargetPlatform targetPlatform)
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

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // start activity handler with config
            ActivityHandler activityHandler = ActivityHandler.GetInstance(config, DeviceUtil);
            
            DeviceUtil.Sleep(3000);

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
            //wrongJsonTest(attributionHandler);

            // test empty response
            //emptyJsonResponseTest(attributionHandler);

            // test server error
            //serverErrorTest(attributionHandler);

            // test ok response with message
            //okMessageTest(attributionHandler);
        }

        private void NullClientTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler: attributionHandler, responseType: ResponseType.NULL);

            // check error
            Assert.Error("Failed to get attribution (Object reference not set to an instance of an object.)");

            // check response was not logged
            Assert.NotVerbose("Response");
        }

        private void clientExceptionTest(AttributionHandler attributionHandler)
        {
            StartGetAttributionTest(attributionHandler, ResponseType.CLIENT_PROTOCOL_EXCEPTION);

            // check the client error
            Assert.Error("Failed to get attribution (testResponseError)");
        }

        private void StartGetAttributionTest(AttributionHandler attributionHandler, ResponseType responseType)
        {
            MockHttpMessageHandler.ResponseType = responseType;

            attributionHandler.AskAttribution();

            DeviceUtil.Sleep(1000);

            RequestTest(MockHttpMessageHandler.HttpRequestMessage);
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
