using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public interface IAttributionHandler
    {
        void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused);

        void CheckSessionResponse(SessionResponseData sessionResponseData);

        void GetAttribution();

        void PauseSending();

        void ResumeSending();
    }
}