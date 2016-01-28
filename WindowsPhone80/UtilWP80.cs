using AdjustSdk.Pcl;
using Microsoft.Phone.Info;
using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace AdjustSdk
{
    public class UtilWP80 : DeviceUtil
    {
        public UtilWP80()
        { }

        public DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                ClientSdk = GetClientSdk(),
                DeviceUniqueId = GetDeviceUniqueId(),
                AppName = GetAppName(),
                AppVersion = GetAppVersion(),
                AppAuthor = GetAppAuthor(),
                AppPublisher = GetAppPublisher(),
                DeviceType = GetDeviceType(),
                DeviceName = GetDeviceName(),
                DeviceManufacturer = getDeviceManufacturer(),
                OsName = getOsName(),
                OsVersion = getOsVersion(),
                Language = getLanguage(),
                Country = getCountry(),
                AdvertisingId = GetAdvertisingId()
            };
        }


        public void RunAttributionChanged(Action<AdjustAttribution> attributionChanged, AdjustAttribution adjustAttribution)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => attributionChanged(adjustAttribution));
        }

        public void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public void LauchDeeplink(Uri deepLinkUri)
        {
            Windows.System.Launcher.LaunchUriAsync(deepLinkUri);
        }

        private string GetClientSdk() { return "wphone80-4.0.3"; }
                
        private string GetDeviceUniqueId()
        {
            var logger = AdjustFactory.Logger;
            object id;
            if (!DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out id))
            {
                logger.Error("This SDK requires the capability ID_CAP_IDENTITY_DEVICE. You might need to adjust your manifest file. See the README for details.");
                return null;
            }
            string deviceId = Convert.ToBase64String(id as byte[]);
                        
            return deviceId;
        }

        private static string GetAppName()
        {
            return getAppAttributeValue("Title");
        }

        private static string GetAppVersion()
        {
            var version = getAppAttributeValue("Version");
            if (version == null) { return null; }

            string[] splits = version.Split('.');
            if (splits.Length >= 2)
            {
                version = Util.f("{0}.{1}", splits[0], splits[1]);
            }

            return version;
        }

        private static string GetAppAuthor()
        {
            return getAppAttributeValue("Author");
        }

        private static string GetAppPublisher()
        {
            return getAppAttributeValue("Publisher");
        }

        private static string GetDeviceType()
        {
            var deviceType = Microsoft.Devices.Environment.DeviceType;
            switch (deviceType)
            {
                case Microsoft.Devices.DeviceType.Device: return "phone";
                case Microsoft.Devices.DeviceType.Emulator: return "emulator";
                default: return "unknown";
            }
        }

        private static string GetDeviceName()
        {
            return DeviceStatus.DeviceName;
        }

        private static string getDeviceManufacturer()
        {
            return DeviceStatus.DeviceManufacturer;
        }

        private static string getOsName()
        {
            return "windows-phone";
        }

        private static string getOsVersion()
        {
            Version v = System.Environment.OSVersion.Version;
            return Util.f("{0}.{1}", v.Major, v.Minor);
        }

        private static string getLanguage()
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            string cultureName = currentCulture.Name;
            if (cultureName.Length < 2)
            {
                return null;
            }

            var language = cultureName.Substring(0, 2);
            return language;
        }

        private static string getCountry()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var cultureName = currentCulture.Name;
            var length = cultureName.Length;
            if (length < 2)
            {
                return null;
            }

            var substring = cultureName.Substring(length - 2, 2);
            var country = substring.ToLower();
            return country;
        }

        private static string getAppAttributeValue(string attributeName)
        {
            var manifest = XDocument.Load("WMAppManifest.xml");
            if (manifest == null) { return null; }

            var appElement = manifest.Root.Element("App");
            if (appElement == null) { return null; }

            var attribute = appElement.Attribute(attributeName);
            if (attribute == null) { return null; }

            return attribute.Value;
        }

        public static string GetAdvertisingId()
        {
            var type = Type.GetType("Windows.System.UserProfile.AdvertisingManager, Windows, Version = 255.255.255.255, Culture = neutral, PublicKeyToken = null, ContentType = WindowsRuntime");
            return type != null ? (string) type.GetProperty("AdvertisingId").GetValue(null, null) : "";
        }
    }
}