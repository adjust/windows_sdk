using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public interface IPackageHandler
    {
        void AddPackage(ActivityPackage activityPackage);

        void CloseFirstPackage();

        void FinishedTrackingActivity(ActivityPackage activityPackage, AdjustSdk.ResponseData responseData, Dictionary<string, string> jsonDict);

        void PauseSending();

        void ResumeSending();

        void SendFirstPackage();

        void SendNextPackage();
    }
}