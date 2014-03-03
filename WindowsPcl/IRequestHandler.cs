using System;

namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void SendPackage(ActivityPackage package);

        void SetResponseDelegate(Action<AdjustSdk.ResponseData> responseDelegate);
    }
}