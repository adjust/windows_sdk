using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace adeven.AdjustIo.PCL
{
    internal class PackageBuilder
    {
        // possible Ids
        internal string DeviceUniqueId { get; set; }

        internal string HardwareId { get; set; }

        internal string NetworkAdapterId { get; set; }

        // general
        internal string AppToken { get; set; }

        internal string Environment { get; set; }

        internal string UserAgent { get; set; }

        internal string ClientSdk { get; set; }

        // session
        internal int SessionCount { get; set; }

        internal int SubSessionCount { get; set; }

        internal DateTime? CreatedAt { get; set; }

        internal TimeSpan? SessionLength { get; set; }

        internal TimeSpan? TimeSpent { get; set; }

        internal TimeSpan? LastInterval { get; set; }

        // events
        internal int EventCount { get; set; }

        internal string EventToken { get; set; }

        internal Dictionary<string, string> CallbackParameters { get; set; }

        internal double AmountInCents { get; set; }

        // defaults
        private ActivityPackage activityPackage { get; set; }

        internal PackageBuilder()
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
            SaveParameter("environment", Environment);
            // session related (used for events as well)
            SaveParameter("session_count", SessionCount);
            SaveParameter("subsession_count", SubSessionCount);
            SaveParameter("session_length", SessionLength);
            SaveParameter("time_spent", TimeSpent);
        }

        internal ActivityPackage BuildSessionPackage()
        {
            FillDefaults();
            SaveParameter("last_interval", LastInterval);

            activityPackage.Path = @"/startup";
            activityPackage.Kind = "session start";
            activityPackage.Suffix = "";

            return activityPackage;
        }

        internal ActivityPackage BuildEventPackage()
        {
            FillDefaults();
            InjectEventParameters();

            activityPackage.Path = @"/event";
            activityPackage.Kind = "event";
            activityPackage.Suffix = this.EventSuffix();

            return activityPackage;
        }

        internal ActivityPackage BuildRevenuePackage()
        {
            FillDefaults();
            SaveParameter("amount", AmountInCents.ToString());
            InjectEventParameters();

            activityPackage.Path = @"/revenue";
            activityPackage.Kind = "revenue";
            activityPackage.Suffix = this.RevenueSuffx();

            return activityPackage;
        }

        private string EventSuffix()
        {
            return String.Format(" '{0}'", EventToken);
        }

        private string RevenueSuffx()
        {
            if (EventToken != null)
            {
                return String.Format(" ({0:.0} cent, '{1}')", AmountInCents, EventToken);
            }
            else
            {
                return String.Format(" ({0:.0} cent)", AmountInCents);
            }
        }

        private void InjectEventParameters()
        {
            SaveParameter("event_count", EventCount);
            SaveParameter("event_token", EventToken);
            SaveParameter("params", CallbackParameters);
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

            var timeZone = value.Value.ToString("zzz");
            var rfc822TimeZone = timeZone.Remove(3, 1);
            var sDTwOutTimeZone = value.Value.ToString("yyyy-MM-ddTHH:mm:ss");
            var sDateTime = String.Format("{0}Z{1}", sDTwOutTimeZone, rfc822TimeZone);
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