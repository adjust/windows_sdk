using System;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public interface DeviceUtil
    {
        DeviceInfo GetDeviceInfo();

        Task RunActionInForeground(Action action, Task previousTask = null);

        void Sleep(int milliseconds);

        Task LauchDeeplink(Uri deeplinkUri, Task previousTask = null);

        string ReadWindowsAdvertisingId();
    }
}