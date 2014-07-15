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

        public string ClientSdk { get { return "wstore3.4.0"; } }

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
            runInForeground(() => responseDelegate(responseData));
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
            string sanitized = Util.SanitizeUserAgent(packageName);
            return sanitized;
        }

        private string getAppVersion()
        {
            PackageId package = getPackage();
            PackageVersion pv = package.Version;
            string version = Util.f("{0}.{1}", pv.Major, pv.Minor);
            string sanitized = Util.SanitizeUserAgent(version);
            return sanitized;
        }

        private string getAppPublisher()
        {
            PackageId package = getPackage();
            string publisher = package.Publisher;
            string sanitized = Util.SanitizeUserAgent(publisher);
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
            return Util.SanitizeUserAgent(deviceModel);
        }

        private string getDeviceManufacturer()
        {
            var deviceManufacturer = SystemInfoEstimate.GetDeviceManufacturerAsync().Result;
            return Util.SanitizeUserAgent(deviceManufacturer);
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
            string sanitized = Util.SanitizeUserAgent(language, "zz");
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
            string sanitized = Util.SanitizeUserAgent(country, "zz");
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
            string sanitized = Util.SanitizeUserAgent(packageName);
            return sanitized;
        }

        private string getAppFullName()
        {
            PackageId package = getPackage();
            string fullName = package.FullName;
            string sanitized = Util.SanitizeUserAgent(fullName);
            return sanitized;
        }

        private string getAppDisplayName()
        {
            string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
            XElement element = XDocument.Load("appxmanifest.xml").Root;
            element = element.Element(XName.Get("Properties", namespaceName));
            element = element.Element(XName.Get("DisplayName", namespaceName));
            string displayName = element.Value;
            string sanitized = Util.SanitizeUserAgent(displayName);
            return sanitized;
        }

        #endregion User Agent


        public void LauchDeepLink(Uri deepLinkUri)
        {
            runInForeground(() => Windows.System.Launcher.LaunchUriAsync(deepLinkUri));
        }

        private void runInForeground(Action actionToRun)
        {
            if (Dispatcher != null)
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => actionToRun());
            else
                Windows.System.Threading.ThreadPool.RunAsync(handler => actionToRun());
        }
    }
}