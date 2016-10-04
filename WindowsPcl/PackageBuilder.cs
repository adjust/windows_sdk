using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    internal class PackageBuilder
    {
        private AdjustConfig _Config;
        private DeviceInfo _DeviceInfo;
        private ActivityState _ActivityState;
        private DateTime _CreatedAt;

        public Dictionary<string, string> ExtraParameters { get; set; }
        public string Deeplink { get; set; }
        public AdjustAttribution Attribution { get; set; }
        public DateTime ClickTime { get; set;}

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo, ActivityState activityState, DateTime createdAt)
            : this(adjustConfig, deviceInfo, createdAt)
        {
            _ActivityState = activityState.Clone();
        }

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo,
            DateTime createdAt)
        {
            _Config = adjustConfig;
            _DeviceInfo = deviceInfo;
            _CreatedAt = createdAt;
        }

        internal ActivityPackage BuildSessionPackage()
        {
            var parameters = GetDefaultParameters();

            AddTimeSpan(parameters, "last_interval", _ActivityState.LastInterval);
            AddString(parameters, "default_tracker", _Config.DefaultTracker);

            return new ActivityPackage(ActivityKind.Session, _DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildEventPackage(AdjustEvent adjustEvent)
        {
            var parameters = GetDefaultParameters();

            AddInt(parameters, "event_count", _ActivityState.EventCount);
            AddString(parameters, "event_token", adjustEvent.EventToken);
            AddDouble(parameters, "revenue", adjustEvent.Revenue);
            AddString(parameters, "currency", adjustEvent.Currency);
            AddDictionaryJson(parameters, "callback_params", adjustEvent.CallbackParameters);
            AddDictionaryJson(parameters, "partner_params", adjustEvent.PartnerParameters);

            return new ActivityPackage(ActivityKind.Event, _DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildClickPackage(string source)
        {
            var parameters = GetIdsParameters();

            AddString(parameters, "source", source);
            AddDateTime(parameters, "click_time", ClickTime);
            AddDictionaryJson(parameters, "params", ExtraParameters);

            if (Attribution != null)
            {
                AddString(parameters, "tracker", Attribution.TrackerName);
                AddString(parameters, "campaign", Attribution.Campaign);
                AddString(parameters, "adgroup", Attribution.Adgroup);
                AddString(parameters, "creative", Attribution.Creative);
            }

            return new ActivityPackage(ActivityKind.Click, _DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildAttributionPackage()
        {
            var parameters = GetIdsParameters();

            return new ActivityPackage(ActivityKind.Attribution, _DeviceInfo.ClientSdk, parameters);
        }

        private Dictionary<string, string> GetIdsParameters()
        {
            var parameters = new Dictionary<string, string>();

            InjectDeviceInfoIds(parameters);
            InjectConfig(parameters);
            InjectCreatedAt(parameters);

            return parameters;
        }

        private Dictionary<string, string> GetDefaultParameters()
        {
            var parameters = new Dictionary<string, string>();

            InjectDeviceInfo(parameters);
            InjectConfig(parameters);
            InjectActivityState(parameters);
            InjectCreatedAt(parameters);

            return parameters;
        }

        private void InjectCreatedAt(Dictionary<string, string> parameters)
        {
            AddDateTime(parameters, "created_at", _CreatedAt);
        }

        private void InjectDeviceInfoIds(Dictionary<string, string> parameters)
        {
            AddString(parameters, "win_udid", _DeviceInfo.DeviceUniqueId);
            AddString(parameters, "win_hwid", _DeviceInfo.HardwareId);
            AddString(parameters, "win_naid", _DeviceInfo.NetworkAdapterId);
            AddString(parameters, "win_adid", _DeviceInfo.ReadWindowsAdvertisingId());
        }

        private void InjectDeviceInfo(Dictionary<string, string> parameters)
        {
            InjectDeviceInfoIds(parameters);

            AddString(parameters, "app_display_name", _DeviceInfo.AppDisplayName);
            AddString(parameters, "app_name", _DeviceInfo.AppName);
            AddString(parameters, "app_version", _DeviceInfo.AppVersion);
            AddString(parameters, "app_publisher", _DeviceInfo.AppPublisher);
            AddString(parameters, "app_author", _DeviceInfo.AppAuthor);
            AddString(parameters, "device_type", _DeviceInfo.DeviceType);
            AddString(parameters, "device_name", _DeviceInfo.DeviceName);
            AddString(parameters, "device_manufacturer", _DeviceInfo.DeviceManufacturer);
            AddString(parameters, "architecture", _DeviceInfo.Architecture);
            AddString(parameters, "os_name", _DeviceInfo.OsName);
            AddString(parameters, "os_version", _DeviceInfo.OsVersion);
            AddString(parameters, "language", _DeviceInfo.Language);
            AddString(parameters, "country", _DeviceInfo.Country);

            AddString(parameters, "eas_name", _DeviceInfo.EasFriendlyName);
            AddString(parameters, "eas_id", _DeviceInfo.EasId);
            AddString(parameters, "eas_os", _DeviceInfo.EasOperatingSystem);
            AddString(parameters, "eas_firmware_version", _DeviceInfo.EasSystemFirmwareVersion);
            AddString(parameters, "eas_hardware_version", _DeviceInfo.EasSystemHardwareVersion);
            AddString(parameters, "eas_system_manufacturer", _DeviceInfo.EasSystemManufacturer);
            AddString(parameters, "eas_product_name", _DeviceInfo.EasSystemProductName);
            AddString(parameters, "eas_system_sku", _DeviceInfo.EasSystemSku);
        }

        private void InjectConfig(Dictionary<string, string> parameters)
        {
            AddString(parameters, "app_token", _Config.AppToken);
            AddString(parameters, "environment", _Config.Environment);
            AddBool(parameters, "needs_response_details", _Config.HasResponseDelegate);
        }

        private void InjectActivityState(Dictionary<string, string> parameters)
        {
            AddInt(parameters, "session_count", _ActivityState.SessionCount);
            AddInt(parameters, "subsession_count", _ActivityState.SubSessionCount);
            AddTimeSpan(parameters, "session_length", _ActivityState.SessionLenght);
            AddTimeSpan(parameters, "time_spent", _ActivityState.TimeSpent);
            AddString(parameters, "win_uuid", _ActivityState.Uuid.ToString());
        }

        #region AddParameter

        private void AddString(Dictionary<string, string> parameters, string key, string value)
        {
            if (String.IsNullOrEmpty(value)) { return; }

            parameters.Add(key, value);
        }

        private void AddDateTime(Dictionary<string, string> parameters, string key, DateTime? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0) { return; }

            var sDateTime = Util.DateFormat(value.Value);

            AddString(parameters, key, sDateTime);
        }

        private void AddInt(Dictionary<string, string> parameters, string key, int? value)
        {
            if (!value.HasValue || value.Value < 0) { return; }

            var sInt = value.Value.ToString();
            AddString(parameters, key, sInt);
        }

        private void AddBool(Dictionary<string, string> parameters, string key, bool? value)
        {
            if (!value.HasValue) { return; }

            var iBool = value.Value ? 1 : 0;

            AddInt(parameters, key, iBool);
        }

        private void AddTimeSpan(Dictionary<string, string> parameters, string key, TimeSpan? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0) { return; }

            double roundedSeconds = Math.Round(value.Value.TotalSeconds, 0, MidpointRounding.AwayFromZero);

            AddInt(parameters, key, (int)roundedSeconds);
        }

        private void AddDictionaryJson(Dictionary<string, string> parameters, string key, Dictionary<string, string> value)
        {
            if (value == null) { return; }
            if (value.Count == 0) { return; }

            string json = JsonConvert.SerializeObject(value);

            AddString(parameters, key, json);
        }

        private void AddDouble(Dictionary<string, string> parameters, string key, double? value)
        {
            if (value == null) { return; }

            string sDouble = Util.f("{0:0.00000}", value);

            AddString(parameters, key, sDouble);
        }

        #endregion AddParameter
    }
}