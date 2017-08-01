using AdjustSdk;
using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockActivityHandler : IActivityHandler
    {
        private readonly MockLogger _mockLogger;
        private const string Prefix = "ActivityHandler";

        public MockActivityHandler(MockLogger mockLogger)
        {
            _mockLogger = mockLogger;
        }

        public void Init(AdjustConfig adjustConfig, IDeviceUtil deviceUtil)
        {
            _mockLogger.Test("{0} Init", Prefix);
        }

        public void FinishedTrackingActivity(ResponseData responseData)
        {
            _mockLogger.Test("{0} FinishedTrackingActivity, {1}", Prefix, responseData);
        }
        
        public void TrackEvent(AdjustEvent adjustEvent)
        {
            _mockLogger.Test("{0} TrackEvent, {1}", Prefix, adjustEvent);
        }

        public void ApplicationDeactivated()
        {
            _mockLogger.Test("{0} TrackSubsessionEnd", Prefix);
        }

        public void ApplicationActivated()
        {
            _mockLogger.Test("{0} TrackSubsessionStart", Prefix);
        }

        public void SetEnabled(bool enabled)
        {
            _mockLogger.Test("{0} SetEnabled, {1}", Prefix, enabled);
        }

        public bool IsEnabled()
        {
            _mockLogger.Test("{0} IsEnabled", Prefix);
            return true;
        }

        public void SetOfflineMode(bool offline)
        {
            _mockLogger.Test("{0} SetOfflineMode, {1}", Prefix, offline);
        }

        public void OpenUrl(Uri uri)
        {
            _mockLogger.Test("{0} OpenUrl, {1}", Prefix, uri);
        }

        public void AddSessionCallbackParameter(string key, string value)
        {
            throw new NotImplementedException();
        }

        public void AddSessionPartnerParameter(string key, string value)
        {
            throw new NotImplementedException();
        }

        public void RemoveSessionCallbackParameter(string key)
        {
            throw new NotImplementedException();
        }

        public void RemoveSessionPartnerParameter(string key)
        {
            throw new NotImplementedException();
        }

        public void ResetSessionCallbackParameters()
        {
            throw new NotImplementedException();
        }

        public void ResetSessionPartnerParameters()
        {
            throw new NotImplementedException();
        }

        public void LaunchSessionResponseTasks(SessionResponseData sessionResponseData)
        {
            throw new NotImplementedException();
        }

        public void LaunchSdkClickResponseTasks(SdkClickResponseData sdkClickResponseData)
        {
            throw new NotImplementedException();
        }

        public void LaunchAttributionResponseTasks(AttributionResponseData attributionResponseData)
        {
            throw new NotImplementedException();
        }

        public bool UpdateAttribution(AdjustAttribution attribution)
        {
            _mockLogger.Test("{0} UpdateAttribution, {1}", Prefix, attribution);
            return false;
        }

        public void SetAskingAttribution(bool askingAttribution)
        {
            _mockLogger.Test("{0} SetAskingAttribution, {1}", Prefix, askingAttribution);
        }

        public ActivityPackage GetAttributionPackage()
        {
            _mockLogger.Test("{0} GetAttributionPackage", Prefix);
            return null;
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters, AdjustAttribution attribution, string deeplink)
        {
            _mockLogger.Test("{0} GetDeeplinkClickPackage", Prefix);
            return null;
        }

        public void SendFirstPackages()
        {
            throw new NotImplementedException();
        }

        public void SetPushToken(string pushToken)
        {
            throw new NotImplementedException();
        }

        public string GetAdid()
        {
            throw new NotImplementedException();
        }

        public AdjustAttribution GetAttribution()
        {
            throw new NotImplementedException();
        }
        
    }
}