using System;
using System.IO;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public interface DeviceUtil
    {
        string ClientSdk { get; }

        string GetUserAgent();

        string GetMd5Hash(string input);

        string GetDeviceUniqueId();

        string GetHardwareId();

        string GetNetworkAdapterId();

        void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData);

        void Sleep(int milliseconds);

        void LauchDeepLink(Uri deepLinkUri);
    }
}