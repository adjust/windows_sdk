using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Core;

namespace AdjustSdk
{
    public class UtilUAP10 : DeviceUtil
    {
        private CoreDispatcher Dispatcher;

        public UtilUAP10()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                Dispatcher = coreWindow.Dispatcher;
        }

        public DeviceInfo GetDeviceInfo()
        {
            var easClientDeviceInformation = new EasClientDeviceInformation();
            
            return new DeviceInfo
            {
                ClientSdk = GetClientSdk(),
                HardwareId = UtilUap.GetHardwareId(),
                NetworkAdapterId = UtilUap.GetNetworkAdapterId(),
                AppDisplayName = UtilUap.GetAppDisplayName(),
                AppVersion = UtilUap.GetAppVersion(),
                AppPublisher = UtilUap.GetAppPublisher(),
                DeviceType = UtilUap.GetDeviceType(),
                DeviceManufacturer = UtilUap.GetDeviceManufacturer(),
                Architecture = UtilUap.GetArchitecture(),
                OsName = GetOsName(),
                OsVersion = UtilUap.GetOsVersion(),
                Language = UtilUap.GetLanguage(),
                Country = UtilUap.GetCountry(),
                AdvertisingId = UtilUap.GetAdvertisingId(),
                EasFriendlyName = UtilUap.ExceptionWrap(() => easClientDeviceInformation.FriendlyName),
                EasId = UtilUap.ExceptionWrap(() => easClientDeviceInformation.Id.ToString()),
                EasOperatingSystem = UtilUap.ExceptionWrap(() => easClientDeviceInformation.OperatingSystem),
                EasSystemFirmwareVersion = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemFirmwareVersion),
                EasSystemHardwareVersion = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemHardwareVersion),
                EasSystemManufacturer = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemManufacturer),
                EasSystemProductName = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemProductName),
                EasSystemSku = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemSku),
            };
        }

        public void RunAttributionChanged(Action<AdjustAttribution> attributionChanged, AdjustAttribution adjustAttribution)
        {
            UtilUap.runInForeground(Dispatcher, () => attributionChanged(adjustAttribution));
        }

        public void Sleep(int milliseconds)
        {
            UtilUap.SleepAsync(milliseconds).Wait();
        }

        public void LauchDeeplink(Uri deepLinkUri)
        {
            UtilUap.runInForeground(Dispatcher, () => Windows.System.Launcher.LaunchUriAsync(deepLinkUri));
        }

        private string GetClientSdk() { return "wuap4.0.2"; }

        private static string GetOsName()
        {
            return "windows";
        }
    }
}