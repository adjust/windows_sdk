using System;

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