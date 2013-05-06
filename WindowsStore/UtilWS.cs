using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.System;

namespace adeven.AdjustIo
{
    public static class Util
    {
        public const string BaseUrl = "http://app.adjust.io";
        public const string ClientSdk = "winstore1.0";
        public const string LogTag = "AdjustIo";

        public static string GetDeviceId()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        public static string GetUserAgent()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(getAppDisplayName());
            builder.Append(" " + getAppVersion());
            builder.Append(" " + getAppPublisher());
            builder.Append(" " + getDeviceType());
            builder.Append(" " + getDeviceName());
            builder.Append(" " + getArchitecture());
            builder.Append(" " + getOsName());
            builder.Append(" " + getOsVersion());
            builder.Append(" " + getLanguage());
            builder.Append(" " + getCountry());

            string userAgent = builder.ToString();
            return userAgent;
        }

        public static string GetBase64EncodedParameters(Dictionary<string, string> parameters)
        {
            string json = JsonConvert.SerializeObject(parameters);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            string encoded = Convert.ToBase64String(bytes);
            return encoded;
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

        private static string getAppDisplayName()
        {
            string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
            XElement element = XDocument.Load("appxmanifest.xml").Root;
            element = element.Element(XName.Get("Properties", namespaceName));
            element = element.Element(XName.Get("DisplayName", namespaceName));
            string displayName = element.Value;
            string sanitized = sanitizeString(displayName);
            return sanitized;
        }

        private static string getDeviceName()
        {
            return "unknown";
        }

        private static string getDeviceType()
        {
            return "unknown";
        }

        private static string getArchitecture()
        {
            PackageId package = getPackage();
            ProcessorArchitecture architecture = package.Architecture;
            switch (architecture)
            {
                case ProcessorArchitecture.Arm: return "arm";
                case ProcessorArchitecture.X86: return "x86";
                case ProcessorArchitecture.X64: return "x64";
                case ProcessorArchitecture.Neutral: return "neutral";
                case ProcessorArchitecture.Unknown: return "unknown";
                default: return "unknown";
            }
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
            return "windows";
        }

        private static string getOsVersion()
        {
            return "8.0";
        }

        private static PackageId getPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
        }

        private static string getAppName()
        {
            PackageId package = getPackage();
            string packageName = package.Name;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private static string getAppFamilyName()
        {
            PackageId package = getPackage();
            string packageName = package.FamilyName;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private static string getAppFullName()
        {
            PackageId package = getPackage();
            string fullName = package.FullName;
            string sanitized = sanitizeString(fullName);
            return sanitized;
        }

        private static string getAppVersion()
        {
            PackageId package = getPackage();
            PackageVersion pv = package.Version;
            string version = string.Format("{0}.{1}", pv.Major, pv.Minor);
            string sanitized = sanitizeString(version);
            return sanitized;
        }

        private static string getAppPublisher()
        {
            PackageId package = getPackage();
            string publisher = package.Publisher;
            string sanitized = sanitizeString(publisher);
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
