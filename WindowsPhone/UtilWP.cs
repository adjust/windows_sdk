using Microsoft.Phone.Info;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using Windows.ApplicationModel.Store;

namespace adeven.AdjustIo
{
    public static class Util
    {
        //public const string BaseUrl = "https://app.adjust.io";
        public const string BaseUrl = "https://stage.adjust.io";
        public const string ClientSdk = "winphone1.0";
        public const string LogTag = "AdjustIo";

        public static string GetDeviceId()
        {
            object id;
            if (!DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out id))
            {
                Debug.WriteLine("[{0}] This SDK requires the capability ID_CAP_IDENTITY_DEVICE. You might need to adjust your manifest file. See the README for details.", Util.LogTag);
                return null;
            }

            string deviceId = Convert.ToBase64String(id as byte[]);
            return deviceId;
        }

        internal static string GetMd5Hash(string input)
        {
            var md5 = new MD5.MD5();

            md5.Value = input;
            return md5.FingerPrint;
        }

        public static string GetUserAgent()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(getAppName());
            builder.Append(" " + getAppVersion());
            builder.Append(" " + getAppAuthor());
            builder.Append(" " + getAppPublisher());
            builder.Append(" " + getDeviceType());
            builder.Append(" " + getDeviceName());
            builder.Append(" " + getDeviceManufacturer());
            builder.Append(" " + getOsName());
            builder.Append(" " + getOsVersion());
            builder.Append(" " + getLanguage());
            builder.Append(" " + getCountry());

            string userAgent = builder.ToString();
            return userAgent;
        }

        public static string GetStringEncodedParameters(Dictionary<string, string> parameters)
        {
            string paramString = string.Empty;
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                paramString += "&" + pair.Key + "=" + pair.Value;
            }
            paramString = paramString.Substring(1);
            return paramString;
        }

        public static string GetBase64EncodedParameters(Dictionary<string, string> parameters)
        {
            string json = JsonConvert.SerializeObject(parameters);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string encoded = Convert.ToBase64String(bytes);
            return encoded;
        }

        private static string getDeviceManufacturer()
        {
            string manufacturer = DeviceStatus.DeviceManufacturer;
            string sanitized = sanitizeString(manufacturer);
            return sanitized;
        }

        private static string getDeviceName()
        {
            string deviceName = DeviceStatus.DeviceName;
            string sanitized = sanitizeString(deviceName);
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

        private static string getAppId()
        {
            Guid guid = CurrentApp.AppId;
            string appId = guid.ToString();
            string sanitized = sanitizeString(appId);
            return sanitized;
        }

        private static string getAppUrl()
        {
            Uri uri = CurrentApp.LinkUri;
            string url = uri.ToString();
            string sanitized = sanitizeString(url);
            return sanitized;
        }

        private static string getAppFullName()
        {
            string fullName = Application.Current.GetType().FullName;
            string sanitized = sanitizeString(fullName);
            return sanitized;
        }

        private static XDocument getManifest()
        {
            XDocument manifest = XDocument.Load("WMAppManifest.xml");
            return manifest;
        }

        private static string getAppName()
        {
            string title = getManifest().Root.Element("App").Attribute("Title").Value;
            string sanitized = sanitizeString(title);
            return sanitized;
        }

        private static string getAppVersion()
        {
            string version = getManifest().Root.Element("App").Attribute("Version").Value;

            string[] splits = version.Split('.');
            if (splits.Length >= 2)
            {
                version = string.Format("{0}.{1}", splits[0], splits[1]);
            }

            string sanitized = sanitizeString(version);
            return sanitized;
        }

        private static string getAppAuthor()
        {
            string author = getManifest().Root.Element("App").Attribute("Author").Value;
            string sanitized = sanitizeString(author);
            return sanitized;
        }

        private static string getAppPublisher()
        {
            string publisher = getManifest().Root.Element("App").Attribute("Publisher").Value;
            string sanitized = sanitizeString(publisher);
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
            string sanitized = sanitizeString(language, "zz");
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
            string sanitized = sanitizeString(country, "zz");
            return sanitized;
        }

        private static string getOsName()
        {
            return "windowsphone";
        }

        private static string getOsVersion()
        {
            Version v = System.Environment.OSVersion.Version;
            string version = string.Format("{0}.{1}", v.Major, v.Minor);
            string sanitized = sanitizeString(version);
            return sanitized;
        }

        private static string sanitizeString(string s, string defaultString = "unknown")
        {
            if (s == null)
            {
                return defaultString;
            }

            s = s.Replace('=', '_');
            string result = Regex.Replace(s, @"\s+", "");
            if (result.Length == 0)
            {
                return defaultString;
            }

            return result;
        }
    }
}
