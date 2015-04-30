using System;
using System.IO;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public interface DeviceUtil
    {
        DeviceInfo GetDeviceInfo();

        void RunAttributionChanged(Action<AdjustAttribution> attributionChanged, AdjustAttribution adjustAttribution);

        void Sleep(int milliseconds);

        void LauchDeeplink(Uri deeplinkUri);
    }
}