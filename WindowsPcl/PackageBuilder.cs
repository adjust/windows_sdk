using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    internal class PackageBuilder
    {
        private AdjustConfig AdjustConfig { get; set; }

        private DeviceInfo DeviceInfo { get; set; }

        private ActivityState ActivityState { get; set; }

        private DateTime CreatedAt { get; set; }

        public Dictionary<string, string> ExtraParameters { get; set; }

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo, ActivityState activityState, DateTime createdAt)
            : this(adjustConfig, deviceInfo, createdAt)
        {
            ActivityState = activityState.Clone();
        }

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo,
            DateTime createdAt)
        {
            AdjustConfig = adjustConfig;
            DeviceInfo = deviceInfo;
            CreatedAt = createdAt;
        }

        internal ActivityPackage BuildSessionPackage()
        {
            var parameters = GetDefaultParameters();

            AddTimeSpan(parameters, "last_interval", ActivityState.LastInterval);
            AddString(parameters, "default_tracker", AdjustConfig.DefaultTracker);

            return new ActivityPackage(ActivityKind.Session, DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildEventPackage(AdjustEvent adjustEvent)
        {
            var parameters = GetDefaultParameters();

            AddInt(parameters, "event_count", ActivityState.EventCount);
            AddString(parameters, "event_token", adjustEvent.EventToken);
            AddDouble(parameters, "revenue", adjustEvent.Revenue);
            AddString(parameters, "currency", adjustEvent.Currency);
            AddDictionaryJson(parameters, "callback_params", adjustEvent.CallbackParameters);
            AddDictionaryJson(parameters, "partner_params", adjustEvent.PartnerParameters);

            return new ActivityPackage(ActivityKind.Event, DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildClickPackage(string source, DateTime clickTime, AdjustAttribution attribution)
        {
            var parameters = GetIdsParameters();

            AddString(parameters, "source", source);
            AddDateTime(parameters, "click_time", clickTime);
            AddDictionaryJson(parameters, "params", ExtraParameters);

            if (attribution != null)
            {
                AddString(parameters, "tracker", attribution.TrackerName);
                AddString(parameters, "campaign", attribution.Campaign);
                AddString(parameters, "adgroup", attribution.Adgroup);
                AddString(parameters, "creative", attribution.Creative);
            }

            return new ActivityPackage(ActivityKind.Click, DeviceInfo.ClientSdk, parameters);
        }

        internal ActivityPackage BuildAttributionPackage()
        {
            var parameters = GetIdsParameters();

            return new ActivityPackage(ActivityKind.Attribution, DeviceInfo.ClientSdk, parameters);
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
            AddDateTime(parameters, "created_at", CreatedAt);
        }

        private void InjectDeviceInfoIds(Dictionary<string, string> parameters)
        {
            AddString(parameters, "win_udid", DeviceInfo.DeviceUniqueId);
            AddString(parameters, "win_hwid", DeviceInfo.HardwareId);
            AddString(parameters, "win_naid", DeviceInfo.NetworkAdapterId);
            AddString(parameters, "win_adid", DeviceInfo.AdvertisingId);
        }

        private void InjectDeviceInfo(Dictionary<string, string> parameters)
        {
            InjectDeviceInfoIds(parameters);

            AddString(parameters, "app_display_name", DeviceInfo.AppDisplayName);
            AddString(parameters, "app_name", DeviceInfo.AppName);
            AddString(parameters, "app_version", DeviceInfo.AppVersion);
            AddString(parameters, "app_publisher", DeviceInfo.AppPublisher);
            AddString(parameters, "app_author", DeviceInfo.AppAuthor);
            AddString(parameters, "device_type", DeviceInfo.DeviceType);
            AddString(parameters, "device_name", DeviceInfo.DeviceName);
            AddString(parameters, "device_manufacturer", DeviceInfo.DeviceManufacturer);
            AddString(parameters, "architecture", DeviceInfo.Architecture);
            AddString(parameters, "os_name", DeviceInfo.OsName);
            AddString(parameters, "os_version", DeviceInfo.OsVersion);
            AddString(parameters, "language", DeviceInfo.Language);
            AddString(parameters, "country", DeviceInfo.Country);
        }

        private void InjectConfig(Dictionary<string, string> parameters)
        {
            AddString(parameters, "app_token", AdjustConfig.AppToken);
            AddString(parameters, "environment", AdjustConfig.Environment);
            AddBool(parameters, "needs_attribution_data", AdjustConfig.HasDelegate);
        }

        private void InjectActivityState(Dictionary<string, string> parameters)
        {
            AddInt(parameters, "session_count", ActivityState.SessionCount);
            AddInt(parameters, "subsession_count", ActivityState.SubSessionCount);
            AddTimeSpan(parameters, "session_length", ActivityState.SessionLenght);
            AddTimeSpan(parameters, "time_spent", ActivityState.TimeSpent);
            AddString(parameters, "win_uuid", ActivityState.Uuid.ToString());
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