using AdjustSdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdjustSdk.Pcl
{
    internal class PackageBuilder
    {
        private AdjustConfig AdjustConfig { get; set; }
        private DeviceInfo DeviceInfo { get; set; }
        private ActivityState ActivityState { get; set; }
        private DateTime CreatedAt { get; set; }

        public Dictionary<string, string> ExtraParameters { get; set; }

        /*
        // possible Ids
        public string DeviceUniqueId { get; set; }
        public string HardwareId { get; set; }
        public string NetworkAdapterId { get; set; }

        // general
        public string AppToken { get; set; }
        public AdjustApi.Environment Environment { get; set; }
        public string UserAgent { get; set; }
        public string ClientSdk { get; set; }
        public Guid Uuid { get; set; }

        // session
        public int SessionCount { get; set; }
        public int SubSessionCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public TimeSpan? SessionLength { get; set; }
        public TimeSpan? TimeSpent { get; set; }
        public TimeSpan? LastInterval { get; set; }

        // events
        public int EventCount { get; set; }
        public string EventToken { get; set; }
        public Dictionary<string, string> CallbackParameters { get; set; }
        public double AmountInCents { get; set; }

        // reattributions
        public Dictionary<string, string> DeepLinksParameters { get; set; }

        // defaults
        private ActivityPackage activityPackage { get; set; }
        */ 

        internal PackageBuilder(AdjustConfig adjustConfig, DeviceInfo deviceInfo,
            ActivityState activityState, DateTime createdAt)
        {
            AdjustConfig = adjustConfig;
            DeviceInfo = deviceInfo;
            ActivityState = activityState.Clone();
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

        internal ActivityPackage BuildClickPackage(string source, DateTime clickTime)
        {
            var parameters = GetDefaultParameters();

            AddString(parameters, "source", source);
            AddDateTime(parameters, "click_time", clickTime);
            AddDictionaryJson(parameters, "params", ExtraParameters);

            return new ActivityPackage(ActivityKind.Click, DeviceInfo.ClientSdk, parameters);
        }

        private Dictionary<string, string> GetDefaultParameters()
        {
            var parameters = new Dictionary<string, string>();

            InjectDeviceInfo(parameters);
            InjectConfig(parameters);
            InjectActivityState(parameters);
            AddDateTime(parameters, "created_at", CreatedAt);

            return parameters;
        }

        private void InjectDeviceInfo(Dictionary<string, string> parameters)
        {
            AddString(parameters, "win_udid", DeviceInfo.DeviceUniqueId);
            AddString(parameters, "win_hwid", DeviceInfo.HardwareId);
            AddString(parameters, "win_naid", DeviceInfo.NetworkAdapterId);
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