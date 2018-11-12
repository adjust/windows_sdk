using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static AdjustSdk.Pcl.Constants;

namespace AdjustSdk.Pcl
{
    internal class PackageBuilder
    {
        private readonly AdjustConfig _config;
        private readonly DeviceInfo _deviceInfo;
        private readonly ActivityStateCopy _activityStateCopy;
        private readonly DateTime _createdAt;
        private readonly SessionParameters _sessionParameters;

        public Dictionary<string, string> ExtraParameters { get; set; }
        public string Deeplink { get; set; }
        public AdjustAttribution Attribution { get; set; }
        public DateTime ClickTime { get; set;}

        private class ActivityStateCopy
        {
            public TimeSpan? LastInterval { get; set; } = null;
            public int EventCount { get; set; } = -1;
            public Guid Uuid { get; set; } = Guid.Empty;
            public int SessionCount { get; set; } = -1;
            public int SubsessionCount { get; set; } = -1;
            public TimeSpan? SessionLength { get; set; } = null;
            public TimeSpan? TimeSpent { get; set; } = null;
            public string PushToken { get; set; } = null;

            public ActivityStateCopy(ActivityState activityState)
            {
                if (activityState == null)
                {
                    Uuid = Util.Uuid;
                    return;
                }

                LastInterval = activityState.LastInterval;
                EventCount = activityState.EventCount;
                Uuid = activityState.Uuid;
                SessionCount = activityState.SessionCount;
                SubsessionCount = activityState.SubSessionCount;
                SessionLength = activityState.SessionLength;
                TimeSpent = activityState.TimeSpent;
                PushToken = activityState.PushToken;
            }
        }

        internal PackageBuilder(AdjustConfig adjustConfig, 
            DeviceInfo deviceInfo,
            ActivityState activityState,
            SessionParameters sessionParameters,
            DateTime createdAt)
            : this(adjustConfig, deviceInfo, activityState, createdAt)
        {
            // no need to copy because all access is made synchronously 
            //  in Activity Handler inside internal functions.
            //  only exceptions are ´Enable´ and ´AskingAttribution´
            //  and they are not read here
            //_ActivityState = activityState;
            _sessionParameters = sessionParameters;
        }

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo,
            ActivityState activityState, DateTime createdAt)
        {
            _config = adjustConfig;
            _deviceInfo = deviceInfo;
            _activityStateCopy = new ActivityStateCopy(activityState);
            _createdAt = createdAt;
        }

        internal ActivityPackage BuildSessionPackage(bool isInDelay)
        {
            Dictionary<string, string> parameters = GetAttributableParameters(!isInDelay ? _sessionParameters : null);

            return new ActivityPackage(ActivityKind.Session, _deviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildEventPackage(AdjustEvent adjustEvent, bool isInDelay)
        {
            var parameters = GetDefaultParameters();

            AddInt(parameters, EVENT_COUNT, _activityStateCopy.EventCount);
            AddString(parameters, EVENT_TOKEN, adjustEvent.EventToken);
            AddDouble(parameters, REVENUE, adjustEvent.Revenue);
            AddString(parameters, CURRENCY, adjustEvent.Currency);
            AddString(parameters, EVENT_CALLBACK_ID, adjustEvent.CallbackId);
            if (!isInDelay)
            {
                AddDictionaryJson(parameters, "callback_params",
                Util.MergeParameters(target: _sessionParameters.CallbackParameters,
                                    source: adjustEvent.CallbackParameters,
                                    parametersName: "Callback"));
                AddDictionaryJson(parameters, "partner_params",
                    Util.MergeParameters(target: _sessionParameters.PartnerParameters,
                                        source: adjustEvent.PartnerParameters,
                                        parametersName: "Partner"));

                return new ActivityPackage(ActivityKind.Event, _deviceInfo.ClientSdk, parameters);
            }

            var eventPackage = new ActivityPackage(ActivityKind.Event, _deviceInfo.ClientSdk, parameters);
            eventPackage.CallbackParameters = adjustEvent.CallbackParameters;
            eventPackage.PartnerParameters = adjustEvent.PartnerParameters;

            return eventPackage;
        }

        internal ActivityPackage BuildClickPackage(string source)
        {
            var parameters = GetAttributableParameters(_sessionParameters);

            AddString(parameters, DEEPLINK, Deeplink);
            AddString(parameters, SOURCE, source);
            AddDateTime(parameters, "click_time", ClickTime);
            AddDictionaryJson(parameters, "params", ExtraParameters);

            if (Attribution != null)
            {
                AddString(parameters, TRACKER, Attribution.TrackerName);
                AddString(parameters, CAMPAIGN, Attribution.Campaign);
                AddString(parameters, ADGROUP, Attribution.Adgroup);
                AddString(parameters, CREATIVE, Attribution.Creative);
            }

            return new ActivityPackage(ActivityKind.Click, _deviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildInfoPackage(string source)
        {
            var parameters = GetIdsParameters();

            AddString(parameters, SOURCE, source);
            InjectPushToken(parameters);

            return new ActivityPackage(ActivityKind.Info, _deviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildAttributionPackage()
        {
            var parameters = GetIdsParameters();

            AddString(parameters, WIN_UUID, _activityStateCopy.Uuid.ToString());
            AddString(parameters, "os_name", _deviceInfo.OsName);
            AddString(parameters, "os_version", _deviceInfo.OsVersion);
            AddString(parameters, "app_version", _deviceInfo.AppVersion);
            AddString(parameters, "device_type", _deviceInfo.DeviceType);
            AddString(parameters, "device_name", _deviceInfo.DeviceName);

            return new ActivityPackage(ActivityKind.Attribution, _deviceInfo.ClientSdk, parameters);
        }

        private Dictionary<string, string> GetIdsParameters()
        {
            var parameters = new Dictionary<string, string>();

            InjectDeviceInfoIds(parameters);
            InjectConfig(parameters);
            InjectCommonParameters(parameters);

            return parameters;
        }

        public ActivityPackage BuildGdprPackage()
        {
            var parameters = GetIdsParameters();

            AddString(parameters, WIN_UUID, _activityStateCopy.Uuid.ToString());
            AddString(parameters, "os_name", _deviceInfo.OsName);
            AddString(parameters, "os_version", _deviceInfo.OsVersion);
            AddString(parameters, "app_version", _deviceInfo.AppVersion);
            AddString(parameters, "device_type", _deviceInfo.DeviceType);
            AddString(parameters, "device_name", _deviceInfo.DeviceName);

            return new ActivityPackage(ActivityKind.Gdpr, _deviceInfo.ClientSdk, parameters);
        }

        private Dictionary<string, string> GetAttributableParameters(SessionParameters sessionParameters)
        {
            Dictionary<string, string> parameters = GetDefaultParameters();

            AddTimeSpan(parameters, "last_interval", _activityStateCopy.LastInterval);
            AddString(parameters, "default_tracker", _config.DefaultTracker);

            if (sessionParameters != null)
            {
                AddDictionaryJson(parameters, "callback_params", sessionParameters.CallbackParameters);
                AddDictionaryJson(parameters, "partner_params", sessionParameters.PartnerParameters);
            }

            return parameters;
        }

        private Dictionary<string, string> GetDefaultParameters()
        {
            var parameters = new Dictionary<string, string>();

            InjectDeviceInfo(parameters);
            InjectConfig(parameters);
            InjectActivityState(parameters);
            InjectCommonParameters(parameters);

            return parameters;
        }

        private void InjectCommonParameters(Dictionary<string, string> parameters)
        {
            AddDateTime(parameters, CREATED_AT, _createdAt);
            AddBool(parameters, ATTRIBUTION_DEEPLINK, true);
        }

        private void InjectDeviceInfoIds(Dictionary<string, string> parameters)
        {
            AddString(parameters, WIN_ADID, _deviceInfo.ReadWindowsAdvertisingId());
            AddString(parameters, WIN_HWID, _deviceInfo.HardwareId);
            AddString(parameters, WIN_NAID, _deviceInfo.NetworkAdapterId);
            AddString(parameters, WIN_UDID, _deviceInfo.DeviceUniqueId);
            AddString(parameters, WIN_UUID, _activityStateCopy.Uuid.ToString());

            AddString(parameters, EAS_ID, _deviceInfo.EasId);
        }

        private void InjectDeviceInfo(Dictionary<string, string> parameters)
        {
            InjectDeviceInfoIds(parameters);

            AddString(parameters, "app_display_name", _deviceInfo.AppDisplayName);
            AddString(parameters, "app_name", _deviceInfo.AppName);
            AddString(parameters, "app_version", _deviceInfo.AppVersion);
            AddString(parameters, "app_publisher", _deviceInfo.AppPublisher);
            AddString(parameters, "app_author", _deviceInfo.AppAuthor);
            AddString(parameters, "device_type", _deviceInfo.DeviceType);
            AddString(parameters, "device_name", _deviceInfo.DeviceName);
            AddString(parameters, "device_manufacturer", _deviceInfo.DeviceManufacturer);
            AddString(parameters, "architecture", _deviceInfo.Architecture);
            AddString(parameters, "os_name", _deviceInfo.OsName);
            AddString(parameters, "os_version", _deviceInfo.OsVersion);
            AddString(parameters, "language", _deviceInfo.Language);
            AddString(parameters, "country", _deviceInfo.Country);

            AddString(parameters, "eas_name", _deviceInfo.EasFriendlyName);
            AddString(parameters, EAS_ID, _deviceInfo.EasId);
            AddString(parameters, "eas_os", _deviceInfo.EasOperatingSystem);
            AddString(parameters, "eas_firmware_version", _deviceInfo.EasSystemFirmwareVersion);
            AddString(parameters, "eas_hardware_version", _deviceInfo.EasSystemHardwareVersion);
            AddString(parameters, "eas_system_manufacturer", _deviceInfo.EasSystemManufacturer);
            AddString(parameters, "eas_product_name", _deviceInfo.EasSystemProductName);
            AddString(parameters, "eas_system_sku", _deviceInfo.EasSystemSku);

            string connectivityType = _deviceInfo.GetConnectivityType()?.ToString();
            string networkType = _deviceInfo.GetNetworkType()?.ToString();

            AddString(parameters, CONNECTIVITY_TYPE, connectivityType);
            AddString(parameters, NETWORK_TYPE, networkType);
        }

        private void InjectConfig(Dictionary<string, string> parameters)
        {
            AddString(parameters, "app_token", _config.AppToken);
            AddString(parameters, "environment", _config.Environment);
            AddBool(parameters, "device_known", _config.DeviceKnown);
            AddBool(parameters, "needs_response_details", true);
            AddString(parameters, SECRET_ID, _config.SecretId);
            AddString(parameters, APP_SECRET, _config.AppSecret);
            AddBool(parameters, "event_buffering_enabled", _config.EventBufferingEnabled);
        }

        private void InjectActivityState(Dictionary<string, string> parameters)
        {
            InjectPushToken(parameters);

            AddString(parameters, WIN_UUID, _activityStateCopy.Uuid.ToString());
            AddInt(parameters, "session_count", _activityStateCopy.SessionCount);
            AddInt(parameters, "subsession_count", _activityStateCopy.SubsessionCount);
            AddTimeSpan(parameters, "session_length", _activityStateCopy.SessionLength);
            AddTimeSpan(parameters, "time_spent", _activityStateCopy.TimeSpent);
        }
        
        private void InjectPushToken(Dictionary<string, string> parameters)
        {
            AddString(parameters, PUSH_TOKEN, _activityStateCopy.PushToken);
        }

        #region AddParameter

        internal static void AddString(Dictionary<string, string> parameters, string key, string value)
        {
            if (string.IsNullOrEmpty(value)) { return; }

            parameters.AddSafe(key, value);
        }

        internal static void AddDateTime(Dictionary<string, string> parameters, string key, DateTime? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0) { return; }

            var sDateTime = Util.DateFormat(value.Value);

            AddString(parameters, key, sDateTime);
        }

        internal static void AddInt(Dictionary<string, string> parameters, string key, int? value)
        {
            if (!value.HasValue || value.Value < 0) { return; }

            var sInt = value.Value.ToString();
            AddString(parameters, key, sInt);
        }

        internal static void AddBool(Dictionary<string, string> parameters, string key, bool? value)
        {
            if (!value.HasValue) { return; }

            var iBool = value.Value ? 1 : 0;

            AddInt(parameters, key, iBool);
        }

        internal static void AddTimeSpan(Dictionary<string, string> parameters, string key, TimeSpan? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0) { return; }

            double roundedSeconds = Math.Round(value.Value.TotalSeconds, 0, MidpointRounding.AwayFromZero);

            AddInt(parameters, key, (int)roundedSeconds);
        }

        internal static void AddDictionaryJson(Dictionary<string, string> parameters, string key, Dictionary<string, string> value)
        {
            if (value == null) { return; }
            if (value.Count == 0) { return; }

            string json = JsonConvert.SerializeObject(value);

            AddString(parameters, key, json);
        }

        internal static void AddDouble(Dictionary<string, string> parameters, string key, double? value)
        {
            if (value == null) { return; }

            string sDouble = Util.F("{0:0.00000}", value);

            AddString(parameters, key, sDouble);
        }
        
        #endregion AddParameter
    }
}