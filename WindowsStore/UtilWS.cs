using adeven.AdjustIo.PCL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;

namespace adeven.AdjustIo
{
    internal class UtilWS : DeviceUtil
    {
        public override string AIEnvironmentSandbox { get { return "sandbox"; } }
        public override string AIEnvironmentProduction { get { return "production"; } }

        public override string ClientSdk { get { return "winstore1.0"; } }

        public override string GetDeviceId()
        {
            return GetDeviceIdHardware();
            //return GetDeviceIdNetwork();
        }

        public string GetDeviceIdHardware()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes);
        }

        public string GetDeviceIdNetwork()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        public override string GetMd5Hash(string input)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        public override string GetUserAgent()
        {
            return String.Join(" ", getAppName(),
                                    getAppVersion(),
                                    getAppPublisher(),
                                    getDeviceType(),
                                    getDeviceName(),
                                    getArchitecture(),
                                    getOsName(),
                                    getOsVersion(),
                                    getLanguage(),
                                    getCountry());
        }

        #region User Agent
        
        private string getAppDisplayName()
        {
            string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
            XElement element = XDocument.Load("appxmanifest.xml").Root;
            element = element.Element(XName.Get("Properties", namespaceName));
            element = element.Element(XName.Get("DisplayName", namespaceName));
            string displayName = element.Value;
            string sanitized = sanitizeString(displayName);
            return sanitized;
        }

        private string getDeviceName()
        {
            return "unknown";
        }

        private string getDeviceType()
        {
            return "unknown";
        }

        private string getArchitecture()
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

        private string getLanguage()
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

        private string getCountry()
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

        private string getOsName()
        {
            return "windows";
        }

        private string getOsVersion()
        {
            return "8.0";
        }

        private PackageId getPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
        }

        private string getAppName()
        {
            PackageId package = getPackage();
            string packageName = package.Name;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private string getAppFamilyName()
        {
            PackageId package = getPackage();
            string packageName = package.FamilyName;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private string getAppFullName()
        {
            PackageId package = getPackage();
            string fullName = package.FullName;
            string sanitized = sanitizeString(fullName);
            return sanitized;
        }

        private string getAppVersion()
        {
            PackageId package = getPackage();
            PackageVersion pv = package.Version;
            string version = string.Format("{0}.{1}", pv.Major, pv.Minor);
            string sanitized = sanitizeString(version);
            return sanitized;
        }

        private string getAppPublisher()
        {
            PackageId package = getPackage();
            string publisher = package.Publisher;
            string sanitized = sanitizeString(publisher);
            return sanitized;
        }

        private string sanitizeString(string s, string defaultString = "unknown")
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

        #endregion
    }
}
