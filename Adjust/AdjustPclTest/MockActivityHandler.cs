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

        public MockActivityHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public void Init(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            MockLogger.Test("{0} Init", prefix);
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            MockLogger.Test("{0} FinishedTrackingActivity, {1}", prefix, jsonDict);
        }

        public void TrackEvent(AdjustEvent adjustEvent)
        {
            MockLogger.Test("{0} TrackEvent, {1}", prefix, adjustEvent);
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

        public void SetOfflineMode(bool offline)
        {
            MockLogger.Test("{0} SetOfflineMode, {1}", prefix, offline);
        }

        public void OpenUrl(Uri uri)
        {
            MockLogger.Test("{0} OpenUrl, {1}", prefix, uri);
        }

        public bool UpdateAttribution(AdjustAttribution attribution)
        {
            MockLogger.Test("{0} UpdateAttribution, {1}", prefix, attribution);
            return false;
        }

        public void SetAskingAttribution(bool askingAttribution)
        {
            MockLogger.Test("{0} SetAskingAttribution, {1}", prefix, askingAttribution);
        }

        public ActivityPackage GetAttributionPackage()
        {
            MockLogger.Test("{0} GetAttributionPackage", prefix);
            return null;
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters, AdjustAttribution attribution)
        {
            MockLogger.Test("{0} GetDeeplinkClickPackage", prefix);
            return null;
        }
    }
}