using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public interface IActivityHandler
    {
        void Init(AdjustConfig adjustConfig, DeviceUtil deviceUtil);

        void FinishedTrackingActivity(ResponseData responseData);

        void TrackEvent(AdjustEvent adjustEvent);

        void TrackSubsessionEnd();

        void TrackSubsessionStart();

        void SetEnabled(bool enabled);

        bool IsEnabled();

        void SetOfflineMode(bool offline);

        void OpenUrl(Uri uri);

        void LaunchSessionResponseTasks(SessionResponseData sessionResponseData);

        void LaunchAttributionResponseTasks(AttributionResponseData attributionResponseData);

        void SetAskingAttribution(bool askingAttribution);

        ActivityPackage GetAttributionPackage();

        ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters, 
            AdjustAttribution attribution, 
            string deeplink);
    }
}