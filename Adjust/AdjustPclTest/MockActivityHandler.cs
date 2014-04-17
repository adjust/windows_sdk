using AdjustSdk;
using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockActivityHandler : IActivityHandler
    {
        private MockLogger MockLogger;
        private const string prefix = "ActivityHandler";

        public AdjustApi.Environment Environment
        {
            get
            {
                MockLogger.Test("{0} Environment", prefix);
                return AdjustApi.Environment.Unknown;
            }
        }

        public MockActivityHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public void FinishTrackingWithResponse(ResponseData responseData)
        {
            MockLogger.Test("{0} FinishTrackingWithResponse", prefix);
        }

        public bool IsBufferedEventsEnabled
        {
            get
            {
                MockLogger.Test("{0} IsBufferedEventsEnabled", prefix);
                return false;
            }
        }

        public void SetBufferedEvents(bool enabledEventBuffering)
        {
            MockLogger.Test("{0} SetBufferedEvents", prefix);
        }

        public void SetEnvironment(AdjustApi.Environment enviornment)
        {
            MockLogger.Test("{0} SetEnvironment", prefix);
        }

        public void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            MockLogger.Test("{0} SetResponseDelegate", prefix);
        }

        public void TrackEvent(string eventToken, Dictionary<string, string> callbackParameters)
        {
            MockLogger.Test("{0} TrackEvent", prefix);
        }

        public void TrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            MockLogger.Test("{0} TrackRevenue", prefix);
        }

        public void TrackSubsessionEnd()
        {
            MockLogger.Test("{0} TrackSubsessionEnd", prefix);
        }

        public void TrackSubsessionStart()
        {
            MockLogger.Test("{0} TrackSubsessionStart", prefix);
        }

        public void SetEnabled(bool enabled)
        {
            MockLogger.Test("{0} SetEnabled, {1}", prefix, enabled);
        }

        public bool IsEnabled()
        {
            MockLogger.Test("{0} IsEnabled", prefix);
            return true;
        }

        public void ReadOpenUrl(Uri url)
        {
            MockLogger.Test("{0} ReadOpenUrl, {1}", prefix, url);
        }
    }
}