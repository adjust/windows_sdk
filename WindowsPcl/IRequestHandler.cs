using System;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void Init(Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback);

        void SendPackage(ActivityPackage package);

        void SendPackageSync(ActivityPackage activityPackage);
    }
}