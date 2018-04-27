using System;

namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void Init(
            IActivityHandler activityHandler, 
            Action<ResponseData> successCallbac, Action<ResponseData,
            ActivityPackage> failureCallback);

        void SendPackage(ActivityPackage package, string basePath, int queueSize);

        void SendPackageSync(ActivityPackage activityPackage, string basePath, int queueSize);

        void Teardown();
    }
}