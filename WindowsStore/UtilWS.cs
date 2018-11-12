using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using AdjustSdk.FileSystem;
using AdjustSdk.Pcl.FileSystem;

namespace AdjustSdk
{
    public class UtilWS : IDeviceUtil
    {
        private readonly CoreDispatcher _dispatcher;
        private DeviceInfo _deviceInfo;
        private readonly ApplicationDataContainer _localSettings;
        private readonly StorageFolder _localFolder;

        private const string PREFS_KEY_INSTALL_TRACKED = "install_tracked";

        private const double PersistValueMaxWaitSeconds = 60;

        public UtilWS()
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

        public bool ClearSimpleValue(string key)
        {
            return _localSettings.Values.Remove(key);
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

        public void PersistSimpleValue(string key, string value)
        {
            _localSettings.Values[key] = value;
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

        public bool TryTakeSimpleValue(string key, out string value)
        {
            value = null;

            object objectValue;
            if (!_localSettings.Values.TryGetValue(key, out objectValue)) return false;

            value = objectValue.ToString();

            return true;
        }

        public async Task<IFile> GetLegacyStorageFile(string fileName)
        {
            if (fileName == null)
                return null;

            var currentAppData = ApplicationData.Current;
            var wrappedFolder = currentAppData.LocalFolder;

            try
            {
                //  make sure the storage folder exists
                await StorageFolder.GetFolderFromPathAsync(wrappedFolder.Path).AsTask().ConfigureAwait(false);

                var wrtFile = await wrappedFolder.GetFileAsync(fileName).AsTask().ConfigureAwait(false);
                return new WinRTFile(wrtFile);
            }
            catch// (FileNotFoundException ex)
            {
                //throw new FileNotFoundException(ex.Message, ex);
                return null;
            }
        }

        //     No connectivity => None = 0
        //     Local network access only => LocalAccess = 1
        //     Limited internet access => ConstrainedInternetAccess = 2
        //     Local and Internet access => InternetAccess = 3
        private int? GetConnectivityType()
        {
            var internetConnProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetConnProfile == null)
                return null;

            return (int)internetConnProfile.GetNetworkConnectivityLevel();
        }

        //None = 0,
        //Gprs = 1,
        //Edge = 2,
        //Umts = 4,
        //Hsdpa = 8,
        //Hsupa = 16,
        //LteAdvanced = 32,
        //Cdma1xRtt = 65536,
        //Cdma1xEvdo = 131072,
        //Cdma1xEvdoRevA = 262144,
        //Cdma1xEvdv = 524288,
        //Cdma3xRtt = 1048576,
        //Cdma1xEvdoRevB = 2097152,
        //CdmaUmb = 4194304,
        //Custom = 2147483648
        private int? GetNetworkType()
        {
            var internetConnProfile = NetworkInformation.GetInternetConnectionProfile();
            if (internetConnProfile == null)
                return null;

            if (!internetConnProfile.IsWwanConnectionProfile)
                return null;

            return (int)internetConnProfile.WwanConnectionProfileDetails.GetCurrentDataClass();
        }

        public string HashStringUsingSha256(string stringValue)
        {
            return HashString(HashAlgorithmNames.Sha256, stringValue);
        }

        public string HashStringUsingSha512(string stringValue)
        {
            return HashString(HashAlgorithmNames.Sha512, stringValue);
        }

        public string HashStringUsingShaMd5(string stringValue)
        {
            return HashString(HashAlgorithmNames.Md5, stringValue);
        }

        private string HashString(string algorithmName, string stringValue)
        {
            // Convert the message string to binary data.
            IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(stringValue, BinaryStringEncoding.Utf8);

            // Create a HashAlgorithmProvider object.
            HashAlgorithmProvider hashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(algorithmName);

            // Hash the message.
            IBuffer buffHash = hashAlgorithmProvider.HashData(buffUtf8Msg);

            // Verify that the hash length equals the length specified for the algorithm.
            if (buffHash.Length != hashAlgorithmProvider.HashLength)
            {
                //throw new Exception("There was an error creating the hash");
                return null;
            }

            // Convert to Hex
            var hashedStringHexValue = BitConverter.ToString(buffHash.ToArray());

            // Get rid of the dashes
            return hashedStringHexValue.Replace("-", "");
        }

        public void SetInstallTracked()
        {
            PersistValue(PREFS_KEY_INSTALL_TRACKED, bool.TrueString);
        }

        public bool IsInstallTracked()
        {
            string isInstallTracked;
            TryTakeValue(PREFS_KEY_INSTALL_TRACKED, out isInstallTracked);

            return string.Equals(isInstallTracked, bool.TrueString, StringComparison.CurrentCultureIgnoreCase);
        }

        private string GetClientSdk()
        {
            return "wstore4.16.0";
        }

        private string GetOsName()
        {
            return "windows";
        }
    }
}
