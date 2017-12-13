using System;

namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void Init(Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback);

        void SendPackage(ActivityPackage package, int queueSize);

        void SendPackageSync(ActivityPackage activityPackage, int queueSize);
    }
}