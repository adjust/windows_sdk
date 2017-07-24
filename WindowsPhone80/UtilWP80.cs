using AdjustSdk.Pcl;
using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Windows.Storage;

namespace AdjustSdk
{
    public class UtilWP80 : IDeviceUtil
    {
        private DeviceInfo _deviceInfo;
        private readonly ApplicationDataContainer _localSettings;

        public UtilWP80()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public DeviceInfo GetDeviceInfo()
        {
            if (_deviceInfo != null) return _deviceInfo;

            _deviceInfo = new DeviceInfo
            {
                ClientSdk = GetClientSdk(),
                DeviceUniqueId = GetDeviceUniqueId(),
                AppName = GetAppName(),
                AppVersion = GetAppVersion(),
                AppAuthor = GetAppAuthor(),
                AppPublisher = GetAppPublisher(),
                DeviceType = GetDeviceType(),
                DeviceName = GetDeviceName(),
                DeviceManufacturer = GetDeviceManufacturer(),
                OsName = GetOsName(),
                OsVersion = GetOsVersion(),
                Language = GetLanguage(),
                Country = GetCountry(),
                ReadWindowsAdvertisingId = ReadWindowsAdvertisingId
            };

            return _deviceInfo;
        }

        public Task RunActionInForeground(Action action, Task previousTask = null)
        {
            if (previousTask != null)
                return previousTask.ContinueWith(_ => Deployment.Current.Dispatcher.InvokeAsync(action));
            else
                return Deployment.Current.Dispatcher.InvokeAsync(action);
        }

        public void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public Task LauchDeeplink(Uri deepLinkUri, Task previousTask = null)
        {
            return RunActionInForeground(() => Windows.System.Launcher.LaunchUriAsync(deepLinkUri), previousTask);
        }

        public string ReadWindowsAdvertisingId()
        {
            return GetAdvertisingId();
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
            return GetAppAttributeValue("Title");
        }

        private static string GetAppVersion()
        {
            var version = GetAppAttributeValue("Version");
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
            return GetAppAttributeValue("Author");
        }

        private static string GetAppPublisher()
        {
            return GetAppAttributeValue("Publisher");
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

        private static string GetDeviceManufacturer()
        {
            return DeviceStatus.DeviceManufacturer;
        }

        private static string GetOsName()
        {
            return "windows-phone";
        }

        private static string GetOsVersion()
        {
            Version v = System.Environment.OSVersion.Version;
            return Util.f("{0}.{1}", v.Major, v.Minor);
        }

        private static string GetLanguage()
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

        private static string GetCountry()
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

        private static string GetAppAttributeValue(string attributeName)
        {
            var manifest = XDocument.Load("WMAppManifest.xml");
            
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

        public void PersistObject(string key, Dictionary<string, object> objectValuesMap)
        {
            var objectValue = new ApplicationDataCompositeValue();
            foreach (var objectValueKvp in objectValuesMap)
                objectValue.Add(objectValueKvp);
            _localSettings.Values[key] = objectValue;
        }

        public object TakeObject(string key)
        {
            return _localSettings.Values[key];
        }

        public bool TryTakeObject(string key, out object objectValuesMap)
        {
            return _localSettings.Values.TryGetValue(key, out objectValuesMap);
        }
    }
}