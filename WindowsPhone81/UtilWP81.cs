using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Core;

namespace AdjustSdk
{
    public class UtilWP81 : DeviceUtil
    {
        private CoreDispatcher Dispatcher;

        public UtilWP81()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                Dispatcher = coreWindow.Dispatcher;
        }

        public string ClientSdk { get { return "wphone81-3.5.0"; } }

        public string GetMd5Hash(string input)
        {
            return UtilUap.GetMd5Hash(input);
        }

        public string GetDeviceUniqueId()
        {
            return null;
        }

        public string GetHardwareId()
        {
            return UtilUap.GetHardwareId();
        }

        public string GetNetworkAdapterId()
        {
            return UtilUap.GetNetworkAdapterId();
        }

        public string GetUserAgent()
        {
            var deviceInfo = new EasClientDeviceInformation();

            var userAgent = String.Join(" ",
                UtilUap.getAppDisplayName(),
                UtilUap.getAppVersion(),
                UtilUap.getAppPublisher(),
                UtilUap.getDeviceType(deviceInfo),
                UtilUap.getDeviceName(deviceInfo),
                UtilUap.getDeviceManufacturer(deviceInfo),
                UtilUap.getArchitecture(),
                getOsName(),
                UtilUap.getOsVersion(),
                UtilUap.getLanguage(),
                UtilUap.getCountry());

            return userAgent;
        }

        public static string getOsName()
        {
            return "windows-phone";
        }

        public void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData)
        {
            UtilUap.runInForeground(Dispatcher, () => responseDelegate(responseData));
        }

        public void Sleep(int milliseconds)
        {
            UtilUap.SleepAsync(milliseconds).Wait();
        }

        public void LauchDeepLink(Uri deepLinkUri)
        {
            UtilUap.runInForeground(Dispatcher, () => Windows.System.Launcher.LaunchUriAsync(deepLinkUri));
        }
    }
}