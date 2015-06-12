using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public interface IAttributionHandler
    {
        void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate);

        void CheckAttribution(Dictionary<string, string> jsonDict);

        void AskAttribution();

        void PauseSending();

        void ResumeSending();
    }
}