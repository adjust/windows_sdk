using AdjustSdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdjustSdk.Pcl
{
    public class PackageBuilder
    {
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

        // defaults
        private ActivityPackage activityPackage { get; set; }

        public PackageBuilder()
        {
            activityPackage = new ActivityPackage
            {
                Parameters = new Dictionary<string, string>()
            };
        }

        private void FillDefaults()
        {
            activityPackage.UserAgent = this.UserAgent;
            activityPackage.ClientSdk = this.ClientSdk;

            // general
            SaveParameter("created_at", CreatedAt);
            SaveParameter("app_token", AppToken);
            SaveParameter("wp_udid", DeviceUniqueId);
            SaveParameter("ws_hwid", HardwareId);
            SaveParameter("ws_naid", NetworkAdapterId);
            SaveParameter("win_uuid", Uuid.ToString());
            SaveParameter("environment", Environment.ToString().ToLower());
            // session related (used for events as well)
            SaveParameter("session_count", SessionCount);
            SaveParameter("subsession_count", SubSessionCount);
            SaveParameter("session_length", SessionLength);
            SaveParameter("time_spent", TimeSpent);
        }

        public ActivityPackage BuildSessionPackage()
        {
            FillDefaults();
            SaveParameter("last_interval", LastInterval);

            activityPackage.Path = @"/startup";
            activityPackage.ActivityKind = ActivityKind.Session;
            activityPackage.Suffix = "";

            return activityPackage;
        }

        public ActivityPackage BuildEventPackage()
        {
            FillDefaults();
            InjectEventParameters();

            activityPackage.Path = @"/event";
            activityPackage.ActivityKind = ActivityKind.Event;
            activityPackage.Suffix = this.EventSuffix();

            return activityPackage;
        }

        public ActivityPackage BuildRevenuePackage()
        {
            FillDefaults();
            SaveParameter("amount", AmountToString());
            InjectEventParameters();

            activityPackage.Path = @"/revenue";
            activityPackage.ActivityKind = ActivityKind.Revenue;
            activityPackage.Suffix = this.RevenueSuffx();

            return activityPackage;
        }

        private string EventSuffix()
        {
            return Util.f(" '{0}'", EventToken);
        }

        private string RevenueSuffx()
        {
            if (EventToken != null)
            {
                return Util.f(" ({0:0.0} cent, '{1}')", AmountInCents, EventToken);
            }
            else
            {
                return Util.f(" ({0:0.0} cent)", AmountInCents);
            }
        }

        private void InjectEventParameters()
        {
            SaveParameter("event_count", EventCount);
            SaveParameter("event_token", EventToken);
            SaveParameter("params", CallbackParameters);
        }

        private string AmountToString()
        {
            int amountInMillis = (int)Math.Round(AmountInCents * 10, MidpointRounding.AwayFromZero);
            AmountInCents = amountInMillis / 10.0; // now rounded to one decimal point
            var amountString = amountInMillis.ToString();
            return amountString;
        }

        #region SaveParameter

        private void SaveParameter(string key, string value)
        {
            if (String.IsNullOrEmpty(value))
                return;

            activityPackage.Parameters.Add(key, value);
        }

        private void SaveParameter(string key, DateTime? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0)
                return;

            var sDateTime = Util.DateFormat(value.Value);

            activityPackage.Parameters.Add(key, sDateTime);
        }

        private void SaveParameter(string key, int value)
        {
            if (value < 0)
                return;

            activityPackage.Parameters.Add(key, value.ToString());
        }

        private void SaveParameter(string key, bool value)
        {
            SaveParameter(key, Convert.ToInt32(value));
        }

        private void SaveParameter(string key, TimeSpan? value)
        {
            if (!value.HasValue || value.Value.Ticks < 0)
                return;

            double roundedSeconds = Math.Round(value.Value.TotalSeconds, 0, MidpointRounding.AwayFromZero);

            SaveParameter(key, (int)roundedSeconds);
        }

        private void SaveParameter(string key, Dictionary<string, string> value)
        {
            if (value == null)
                return;

            string json = JsonConvert.SerializeObject(value);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string encoded = Convert.ToBase64String(bytes);

            activityPackage.Parameters.Add(key, encoded);
        }

        #endregion SaveParameter
    }
}