using System;
namespace AdjustSdk.Pcl
{
    interface IRequestHandler
    {
        void SendPackage(ActivityPackage package);
        void SetResponseDelegate(Action<AdjustSdk.ResponseData> responseDelegate);
    }
}
