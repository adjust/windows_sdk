using AdjustSdk.Pcl;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using System.Reflection;

namespace AdjustSdk
{
    public class UtilWS8 : DeviceUtil
    {
        private CoreDispatcher Dispatcher;

        public UtilWS8()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                Dispatcher = coreWindow.Dispatcher;
        }
        public DeviceInfo GetDeviceInfo()
        {
            var easClientDeviceInformation = new EasClientDeviceInformation();

            return new DeviceInfo
            {
                ClientSdk = GetClientSdk(),
                AppVersion = GetAppVersion(),
                AppPublisher = GetAppPublisher(),
                DeviceType = GetDeviceType(),
                DeviceManufacturer = GetDeviceManufacturer(),
                OsName = GetOsName(),
                AdvertisingId = GetAdvertisingId(),
                HardwareId = GetHardwareId(),
                NetworkAdapterId = GetNetworkAdapterId(),
                AppDisplayName = GetAppDisplayName(),
                Architecture = GetArchitecture(),
                OsVersion = GetOsVersion(),
                Language = GetLanguage(),
                Country = GetCountry(),
                EasFriendlyName = ExceptionWrap(() => easClientDeviceInformation.FriendlyName),
                EasId = ExceptionWrap(() => easClientDeviceInformation.Id.ToString()),
                EasOperatingSystem = ExceptionWrap(() => easClientDeviceInformation.OperatingSystem),
                EasSystemManufacturer = ExceptionWrap(() => easClientDeviceInformation.SystemManufacturer),
                EasSystemProductName = ExceptionWrap(() => easClientDeviceInformation.SystemProductName),
                EasSystemSku = ExceptionWrap(() => easClientDeviceInformation.SystemSku),
            };
        }


        public void Sleep(int milliseconds)
        {
            SleepAsync(milliseconds).Wait();
        }
        
        public void RunAttributionChanged(Action<AdjustAttribution> attributionChanged, AdjustAttribution adjustAttribution)
        {
            RunInForeground(Dispatcher, () => attributionChanged(adjustAttribution));
        }
        
        public void LauchDeeplink(Uri deepLinkUri)
        {
            RunInForeground(Dispatcher, () => Windows.System.Launcher.LaunchUriAsync(deepLinkUri));
        }

        private void RunInForeground(CoreDispatcher Dispatcher, Action actionToRun)
        {
            if (Dispatcher != null)
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => actionToRun());
            else
                Windows.System.Threading.ThreadPool.RunAsync(handler => actionToRun());
        }

        private async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        private string GetHardwareId()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];

            dataReader.ReadBytes(bytes);

            return Convert.ToBase64String(bytes);
        }

        private string GetNetworkAdapterId()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        private string GetAppDisplayName()
        {
            string displayName = null;
            try
            {
                string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
                XElement element = XDocument.Load("appxmanifest.xml").Root;
                element = element.Element(XName.Get("Properties", namespaceName));
                element = element.Element(XName.Get("DisplayName", namespaceName));
                displayName = element.Value;
            }
            catch (Exception e)
            { }

            return displayName;
        }

        private string GetAppVersion()
        {
            PackageId package = GetPackage();
            PackageVersion pv = package.Version;
            return Util.f("{0}.{1}", pv.Major, pv.Minor);
        }

        private PackageId GetPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
        }

        private string GetAppPublisher()
        {
            PackageId package = GetPackage();
            return package.Publisher;
        }

        private string GetDeviceType()
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

        private string GetDeviceManufacturer()
        {
            return SystemInfoEstimate.GetDeviceManufacturerAsync().Result;
        }

        private string GetArchitecture()
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

        private string GetOsVersion()
        {
            return SystemInfoEstimate.GetWindowsVersionAsync().Result;
        }

        private string GetLanguage()
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

        private string GetCountry()
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

        private string ExceptionWrap(Func<string> function)
        {
            try
            {
                return function();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        private string GetClientSdk() { return "wstore80-4.0.2"; }

        private string GetAdvertisingId()
        {
            var type = Type.GetType("Windows.System.UserProfile.AdvertisingManager, Windows, Version = 255.255.255.255, Culture = neutral, PublicKeyToken = null, ContentType = WindowsRuntime");
            return type != null ? (string)type.GetRuntimeProperty("AdvertisingId").GetValue(null, null) : "";
        }

        private string GetOsName()
        {
            return "windows";
        }
    }
}