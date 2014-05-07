using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
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

        public string ClientSdk { get { return "wstore3.3.1"; } }

        public string GetMd5Hash(string input)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        public string GetDeviceUniqueId()
        {
            return null; //deviceUniqueId is from WP
        }

        public string GetHardwareId()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];

            dataReader.ReadBytes(bytes);

            return Convert.ToBase64String(bytes);
        }

        public string GetNetworkAdapterId()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        public string GetUserAgent()
        {
            var userAgent = String.Join(" ",
                getAppDisplayName(),
                getAppVersion(),
                getAppPublisher(),
                getDeviceType(),
                getDeviceName(),
                getDeviceManufacturer(),
                getArchitecture(),
                getOsName(),
                getOsVersion(),
                getLanguage(),
                getCountry());

            return userAgent;
        }

        public void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData)
        {
            // TODO fallback to threadpool if null?
            if (Dispatcher != null)
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => responseDelegate(responseData));
        }

        public void Sleep(int milliseconds)
        {
            SleepAsync(milliseconds).Wait();
        }

        private async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        #region User Agent

        private string getAppName()
        {
            PackageId package = getPackage();
            string packageName = package.Name;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private string getAppVersion()
        {
            PackageId package = getPackage();
            PackageVersion pv = package.Version;
            string version = Util.f("{0}.{1}", pv.Major, pv.Minor);
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

        private string getDeviceType()
        {
            var deviceType = SystemInfoEstimate.GetDeviceCategoryAsync().Result;
            switch (deviceType)
            {
                case "Computer.Lunchbox": return "pc";
                case "Computer.Tablet": return "tablet";
                default: return "unknown";
            }
        }

        private string getDeviceName()
        {
            var deviceModel = SystemInfoEstimate.GetDeviceModelAsync().Result;
            return sanitizeString(deviceModel);
        }

        private string getDeviceManufacturer()
        {
            var deviceManufacturer = SystemInfoEstimate.GetDeviceManufacturerAsync().Result;
            return sanitizeString(deviceManufacturer);
        }

        private string getArchitecture()
        {
            ProcessorArchitecture architecture = SystemInfoEstimate.GetProcessorArchitectureAsync().Result;
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

        private string getOsName()
        {
            return "windows";
        }

        private string getOsVersion()
        {
            return SystemInfoEstimate.GetWindowsVersionAsync().Result;
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

        private PackageId getPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
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

        private string sanitizeString(string s, string defaultString = "unknown")
        {
            if (s == null)
            {
                return defaultString;
            }

            s = s.Replace('=', '_');
            s = s.Replace(',', '.');
            string result = Regex.Replace(s, @"\s+", "");
            if (result.Length == 0)
            {
                return defaultString;
            }

            return result;
        }

        #endregion User Agent
    }
}