using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockAttributionHandler : IAttributionHandler
    {
        private MockLogger MockLogger;

        private const string prefix = "AttributionHandler";

        public MockAttributionHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate)
        {
            MockLogger.Test("{0} Init, startPaused {1}, hasDelegate {2}", prefix, startPaused, hasDelegate);
        }

        public void CheckAttribution(Dictionary<string, string> jsonDict)
        {
            MockLogger.Test("{0} CheckAttribution {1}", prefix, jsonDict);
        }

        public void AskAttribution()
        {
            MockLogger.Test("{0} AskAttribution", prefix);
        }

        public void PauseSending()
        {
            MockLogger.Test("{0} PauseSending", prefix);
        }

        public void ResumeSending()
        {
            MockLogger.Test("{0} ResumeSending", prefix);
        }
    }
}
