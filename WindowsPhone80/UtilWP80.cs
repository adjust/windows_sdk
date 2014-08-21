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
        public string ClientSdk { get { return "wphone80-3.4.2"; } }

        public string GetMd5Hash(string input)
        {
            return MD5Core.GetHashString(input);
        }

        public string GetDeviceUniqueId()
        {
            var logger = AdjustFactory.Logger;
            object id;
            if (!DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out id))
            {
                logger.Error("This SDK requires the capability ID_CAP_IDENTITY_DEVICE. You might need to adjust your manifest file. See the README for details.");
                return null;
            }
            string deviceId = Convert.ToBase64String(id as byte[]);

            logger.Debug("Device unique Id ({0})", deviceId);

            return deviceId;
        }

        public string GetHardwareId()
        {
            return null; // hardwareId is from WS
        }

        public string GetNetworkAdapterId()
        {
            return null; // networkAdapter is from WS
        }

        public string GetUserAgent()
        {
            var userAgent = String.Join(" ",
                getAppName(),
                getAppVersion(),
                getAppAuthor(),
                getAppPublisher(),
                getDeviceType(),
                getDeviceName(),
                getDeviceManufacturer(),
                getOsName(),
                getOsVersion(),
                getLanguage(),
                getCountry());

            return userAgent;
        }

        public void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => responseDelegate(responseData));
        }

        public void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        #region User Agent

        private static string getAppName()
        {
            string title = getManifest().Root.Element("App").Attribute("Title").Value;
            string sanitized = Util.SanitizeUserAgent(title);
            return sanitized;
        }

        private static string getAppVersion()
        {
            string version = getManifest().Root.Element("App").Attribute("Version").Value;

            string[] splits = version.Split('.');
            if (splits.Length >= 2)
            {
                version = Util.f("{0}.{1}", splits[0], splits[1]);
            }

            string sanitized = Util.SanitizeUserAgent(version);
            return sanitized;
        }

        private static string getAppAuthor()
        {
            string author = getManifest().Root.Element("App").Attribute("Author").Value;
            string sanitized = Util.SanitizeUserAgent(author);
            return sanitized;
        }

        private static string getAppPublisher()
        {
            string publisher = getManifest().Root.Element("App").Attribute("Publisher").Value;
            string sanitized = Util.SanitizeUserAgent(publisher);
            return sanitized;
        }

        private static string getDeviceType()
        {
            var deviceType = Microsoft.Devices.Environment.DeviceType;
            switch (deviceType)
            {
                case Microsoft.Devices.DeviceType.Device: return "phone";
                case Microsoft.Devices.DeviceType.Emulator: return "emulator";
                default: return "unknown";
            }
        }

        private static string getDeviceName()
        {
            string deviceName = DeviceStatus.DeviceName;
            string sanitized = Util.SanitizeUserAgent(deviceName);
            return sanitized;
        }

        private static string getDeviceManufacturer()
        {
            string manufacturer = DeviceStatus.DeviceManufacturer;
            string sanitized = Util.SanitizeUserAgent(manufacturer);
            return sanitized;
        }

        private static string getOsName()
        {
            return "windows-phone";
        }

        private static string getOsVersion()
        {
            Version v = System.Environment.OSVersion.Version;
            string version = Util.f("{0}.{1}", v.Major, v.Minor);
            string sanitized = Util.SanitizeUserAgent(version);
            return sanitized;
        }

        private static string getLanguage()
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            string cultureName = currentCulture.Name;
            if (cultureName.Length < 2)
            {
                return "zz";
            }

            string language = cultureName.Substring(0, 2);
            string sanitized = Util.SanitizeUserAgent(language, "zz");
            return sanitized;
        }

        private static string getCountry()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string cultureName = currentCulture.Name;
            int length = cultureName.Length;
            if (length < 2)
            {
                return "zz";
            }

            string substring = cultureName.Substring(length - 2, 2);
            string country = substring.ToLower();
            string sanitized = Util.SanitizeUserAgent(country, "zz");
            return sanitized;
        }

        private static XDocument getManifest()
        {
            XDocument manifest = XDocument.Load("WMAppManifest.xml");
            return manifest;
        }

        #endregion User Agent


        public void LauchDeepLink(Uri deepLinkUri)
        {
            Windows.System.Launcher.LaunchUriAsync(deepLinkUri);
        }
    }
}