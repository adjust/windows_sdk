using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using AdjustSdk.FileSystem;
using AdjustSdk.Pcl.FileSystem;

namespace AdjustSdk
{
    public class UtilWP81 : IDeviceUtil
    {
        private readonly CoreDispatcher _dispatcher;
        private DeviceInfo _deviceInfo;
        private readonly ApplicationDataContainer _localSettings;
        private readonly StorageFolder _localFolder;

        private const double PersistValueMaxWaitSeconds = 60;

        public UtilWP81()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                _dispatcher = coreWindow.Dispatcher;

            _localSettings = ApplicationData.Current.LocalSettings;
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public DeviceInfo GetDeviceInfo()
        {
            if (_deviceInfo != null) return _deviceInfo;

            var easClientDeviceInformation = new EasClientDeviceInformation();
            _deviceInfo = new DeviceInfo
            {
                ClientSdk = GetClientSdk(),
                HardwareId = UtilUap.GetHardwareId(),
                NetworkAdapterId = UtilUap.GetNetworkAdapterId(),
                AppDisplayName = UtilUap.GetAppDisplayName(),
                AppVersion = UtilUap.GetAppVersion(),
                AppPublisher = UtilUap.GetAppPublisher(),
                DeviceType = UtilUap.GetDeviceType(),
                DeviceManufacturer = UtilUap.GetDeviceManufacturer(),
                Architecture = UtilUap.GetArchitecture(),
                OsName = GetOsName(),
                OsVersion = UtilUap.GetOsVersion(),
                Language = UtilUap.GetLanguage(),
                Country = UtilUap.GetCountry(),
                ReadWindowsAdvertisingId = ReadWindowsAdvertisingId,
                EasFriendlyName = UtilUap.ExceptionWrap(() => easClientDeviceInformation.FriendlyName),
                EasId = UtilUap.ExceptionWrap(() => easClientDeviceInformation.Id.ToString()),
                EasOperatingSystem = UtilUap.ExceptionWrap(() => easClientDeviceInformation.OperatingSystem),
                EasSystemFirmwareVersion = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemFirmwareVersion),
                EasSystemHardwareVersion = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemHardwareVersion),
                EasSystemManufacturer = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemManufacturer),
                EasSystemProductName = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemProductName),
                EasSystemSku = UtilUap.ExceptionWrap(() => easClientDeviceInformation.SystemSku),
                GetConnectivityType = GetConnectivityType,
                GetNetworkType = GetNetworkType
            };

            return _deviceInfo;
        }

        public Task RunActionInForeground(Action action, Task previousTask = null)
        {
            return UtilUap.RunInForeground(_dispatcher, () => action(), previousTask);
        }

        public void Sleep(int milliseconds)
        {
            UtilUap.SleepAsync(milliseconds).Wait();
        }

        public Task LauchDeeplink(Uri deepLinkUri, Task previousTask = null)
        {
            return UtilUap.RunInForeground(_dispatcher, () => Windows.System.Launcher.LaunchUriAsync(deepLinkUri), previousTask);
        }

        public string ReadWindowsAdvertisingId()
        {
            return UtilUap.GetAdvertisingId();
        }

        public void PersistObject(string key, Dictionary<string, object> objectValuesMap)
        {
            var objectValue = new ApplicationDataCompositeValue();
            foreach (var objectValueKvp in objectValuesMap)
                objectValue.Add(objectValueKvp);
            _localSettings.Values[key] = objectValue;
        }

        public bool PersistValue(string key, string value)
        {
            var valueFile = _localFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting).AsTask().Result;
            var valueBuffer = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);

            return FileIO.WriteBufferAsync(valueFile, valueBuffer)
                .AsTask().Wait(TimeSpan.FromSeconds(PersistValueMaxWaitSeconds));
        }

        public bool TryTakeObject(string key, out Dictionary<string, object> objectValuesMap)
        {
            objectValuesMap = null;

            object objectValue;
            if (!_localSettings.Values.TryGetValue(key, out objectValue)) return false;

            var applicationDataCompositeValue = objectValue as ApplicationDataCompositeValue;
            if (applicationDataCompositeValue == null) return false;

            objectValuesMap = new Dictionary<string, object>(applicationDataCompositeValue.Count);
            foreach (KeyValuePair<string, object> objectValueKvp in applicationDataCompositeValue)
            {
                objectValuesMap.Add(objectValueKvp.Key, objectValueKvp.Value);
            }

            return true;
        }

        public bool TryTakeValue(string key, out string value)
        {
            try
            {
                // may throw FileNotFoundException, UnauthorizedAccessException and ArgumentException
                var valueFile = _localFolder.GetFileAsync(key).AsTask().Result;
                var valueBuffer = FileIO.ReadBufferAsync(valueFile).AsTask().Result;
                using (var dataReader = DataReader.FromBuffer(valueBuffer))
                {
                    value = dataReader.ReadString(valueBuffer.Length);
                    return true;
                }
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public async Task<IFile> GetLegacyStorageFile(string fileName)
        {
            if (fileName == null)
                return null;

            var currentAppData = ApplicationData.Current;
            var localFolder = currentAppData.LocalFolder;

            try
            {
                //  make sure the storage folder exists
                await StorageFolder.GetFolderFromPathAsync(localFolder.Path).AsTask().ConfigureAwait(false);

                var wrtFile = await localFolder.GetFileAsync(fileName).AsTask().ConfigureAwait(false);
                return new WinRTFile(wrtFile);
            }
            catch// (FileNotFoundException ex)
            {
                //throw new FileNotFoundException(ex.Message, ex);
                return null;
            }
        }

        private string GetConnectivityType()
        {
            var internetConnProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetConnProfile == null)
                return null;

            bool hasNoInternetConnection = internetConnProfile.GetNetworkConnectivityLevel() ==
                                           NetworkConnectivityLevel.None;
            if (hasNoInternetConnection)
                return null;

            if (internetConnProfile.IsWlanConnectionProfile)
                return "wlan";

            if (internetConnProfile.IsWwanConnectionProfile)
                return "wwan";

            return null;
        }

        private string GetNetworkType()
        {
            //TODO: investigate whether it's possible to get Network Type information on Windows
            return "unknown";
        }

        private string GetClientSdk()
        {
            return "wphone81-4.12.0";
        }

        private static string GetOsName()
        {
            return "windows-phone";
        }
    }
}