using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Core;

namespace AdjustSdk
{
    public class UtilWS : DeviceUtil
    {
        private CoreDispatcher Dispatcher;

        public UtilWS()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                Dispatcher = coreWindow.Dispatcher;
        }

        public DeviceInfo GetDeviceInfo()
        {
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
            };
        }

        public void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData)
        {
            UtilUap.runInForeground(Dispatcher, () => responseDelegate(responseData));
        }

        public void Sleep(int milliseconds)
        {
            UtilUap.SleepAsync(milliseconds).Wait();
        }

        public void LauchDeeplink(Uri deepLinkUri)
        {
            UtilUap.runInForeground(Dispatcher, () => Windows.System.Launcher.LaunchUriAsync(deepLinkUri));
        }

        private string GetClientSdk() { return "wstore3.5.1"; }

        private string GetOsName()
        {
            return "windows";
        }
    }
}