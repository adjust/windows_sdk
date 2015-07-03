using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;

namespace AdjustSdk.Uap
{
    public class UtilUap
    {
        public static string GetAdvertisingId()
        {
            return Windows.System.UserProfile.AdvertisingManager.AdvertisingId;
        }

        public static string GetAppName()
        {
            PackageId package = GetPackage();
            return package.Name;
        }

        public static string GetAppVersion()
        {
            PackageId package = GetPackage();
            PackageVersion pv = package.Version;
            return Util.f("{0}.{1}", pv.Major, pv.Minor);
        }

        public static string GetAppPublisher()
        {
            PackageId package = GetPackage();
            return package.Publisher;
        }

        public static string GetDeviceType()
        {
            var deviceType = SystemInfoEstimate.GetDeviceCategoryAsync().Result;
            switch (deviceType)
            {
                case "Computer.Lunchbox": return "pc";
                case "Computer.Tablet": return "tablet";
                case "Computer.Portable": return "phone";
                default: return "unknown";
            }
        }

        public static string GetDeviceType(EasClientDeviceInformation deviceInfo)
        {
            var deviceType = SystemInfoEstimate.GetDeviceCategoryAsync().Result;

            switch (deviceType)
            {
                case "Computer.Lunchbox": return "pc";
                case "Computer.Tablet": return "tablet";
                case "Computer.Portable": return "phone";
            }

            if (deviceInfo.SystemSku == "Microsoft Virtual")
            {
                return "emulator";
            }

            return "unknown";
        }

        public static string GetDeviceName() 
        {
            return SystemInfoEstimate.GetDeviceModelAsync().Result;
        }

        public static string GetDeviceName(EasClientDeviceInformation deviceInfo)
        {
            return deviceInfo.SystemProductName;
        }

        public static string GetDeviceManufacturer()
        {
            return SystemInfoEstimate.GetDeviceManufacturerAsync().Result;
        }

        public static string GetDeviceManufacturer(EasClientDeviceInformation deviceInfo)
        {
            return deviceInfo.SystemManufacturer;
        }

        public static string GetArchitecture()
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

        public static string GetOsVersion()
        {
            return SystemInfoEstimate.GetWindowsVersionAsync().Result;
        }

        public static string GetLanguage()
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            string cultureName = currentCulture.Name;
            if (cultureName.Length < 2)
            {
                return null;
            }

            string language = cultureName.Substring(0, 2);
            return language;
        }

        public static string GetCountry()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string cultureName = currentCulture.Name;
            int length = cultureName.Length;
            if (length < 2)
            {
                return null;
            }

            string substring = cultureName.Substring(length - 2, 2);
            string country = substring.ToLower();
            return country;
        }

        public static PackageId GetPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
        }

        public static string GetAppFamilyName()
        {
            PackageId package = GetPackage();
            return package.FamilyName;
        }

        public static string GetAppFullName()
        {
            PackageId package = GetPackage();
            return package.FullName;
        }

        public static string GetAppDisplayName()
        {
            /*
            string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
            XElement element = XDocument.Load("appxmanifest.xml").Root;
            element = element.Element(XName.Get("Properties", namespaceName));
            element = element.Element(XName.Get("DisplayName", namespaceName));
            string displayName = element.Value;
            return displayName;
             * */
            return null;
        }

        public static string GetHardwareId()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];

            dataReader.ReadBytes(bytes);

            return Convert.ToBase64String(bytes);
        }

        public static string GetNetworkAdapterId()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        public static void runInForeground(CoreDispatcher Dispatcher, Action actionToRun)
        {
            if (Dispatcher != null)
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => actionToRun());
            else
                Windows.System.Threading.ThreadPool.RunAsync(handler => actionToRun());
        }

        public static async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
