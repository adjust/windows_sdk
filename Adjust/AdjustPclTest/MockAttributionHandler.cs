using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockAttributionHandler : IAttributionHandler
    {
        private readonly MockLogger _mockLogger;

        private const string Prefix = "AttributionHandler";

        public MockAttributionHandler(MockLogger mockLogger)
        {
            _mockLogger = mockLogger;
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused)
        {
            _mockLogger.Test("{0} Init, startPaused {1}", Prefix, startPaused);
        }

        public void CheckAttribution(Dictionary<string, string> jsonDict)
        {
            _mockLogger.Test("{0} CheckAttribution {1}", Prefix, jsonDict);
        }

        public void AskAttribution()
        {
            _mockLogger.Test("{0} AskAttribution", Prefix);
        }

        public void CheckSessionResponse(SessionResponseData sessionResponseData)
        {
            _mockLogger.Test("{0} CheckSessionResponse, {1}", Prefix, sessionResponseData);
        }

        public void CheckSdkClickResponse(SdkClickResponseData sdkClickResponseData)
        {
            _mockLogger.Test("{0} CheckSdkClickResponse, {1}", Prefix, sdkClickResponseData);
        }

        public void GetAttribution()
        {
            _mockLogger.Test("{0} GetAttribution", Prefix);
        }

        public void PauseSending()
        {
            _mockLogger.Test("{0} PauseSending", Prefix);
        }

        public void ResumeSending()
        {
            _mockLogger.Test("{0} ResumeSending", Prefix);
        }
    }
}
