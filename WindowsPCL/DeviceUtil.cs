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

    }
}