using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public interface IPackageHandler
    {
        void Init(IActivityHandler activityHandler, bool startPaused);

        void AddPackage(ActivityPackage activityPackage);

        void SendFirstPackage();

        void SendNextPackage();

        void CloseFirstPackage(ActivityPackage activityPackage);

        void PauseSending();

        void ResumeSending();

        void FinishedTrackingActivity(Dictionary<string, string> jsonDict);
    }
}