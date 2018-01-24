using System;

namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void Init(Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback);

        void SendPackage(ActivityPackage package, string basePath, int queueSize);

        void SendPackageSync(ActivityPackage activityPackage, string basePath, int queueSize);

        void Teardown();
    }
}