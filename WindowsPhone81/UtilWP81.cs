using AdjustSdk.Pcl;
using AdjustSdk.Uap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.UI.Core;

namespace AdjustSdk
{
    public class UtilWP81 : IDeviceUtil
    {
        private readonly CoreDispatcher _dispatcher;
        private DeviceInfo _deviceInfo;
        private readonly ApplicationDataContainer _localSettings;

        public UtilWP81()
        {
            // must be called from the UI thread
            var coreWindow = CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
                _dispatcher = coreWindow.Dispatcher;

            _localSettings = ApplicationData.Current.LocalSettings;
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

        public void PersistValue(string key, string value)
        {
            // reason to use ApplicationDataCompositeValue:
            // each setting can be up to 8K bytes in size and each composite setting (ApplicationDataCompositeValue) can be up to 64K bytes in size
            var compositeValue = new ApplicationDataCompositeValue
                {new KeyValuePair<string, object>(key, value)};
            _localSettings.Values[key] = compositeValue;
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
            object takenValue;
            if (_localSettings.Values.TryGetValue(key, out takenValue))
            {
                var compositeValue = takenValue as ApplicationDataCompositeValue;
                if (compositeValue != null)
                {
                    value = compositeValue[key] as string;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private string GetClientSdk()
        {
            return "wphone81-4.0.3";
        }

        private static string GetOsName()
        {
            return "windows-phone";
        }
    }
}