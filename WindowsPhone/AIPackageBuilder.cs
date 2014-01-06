using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    public class AIPackageBuilder
    {
        //general
        internal string AppToken { get; set; }
        internal string MacSha1 { get; set; }
        internal string MacShortMD5 { get; set; }
        internal string IdForAdvertisers { get; set; }
        internal string FbAttributionId { get; set; }
        internal string Environment { get; set; }
        internal string UserAgent { get; set; }
        internal string ClientSdk { get; set; }
        internal bool IsTrackingEnable { get; set; }

        //session
        internal int SessionCount { get; set; }
        internal int SubSessionCount { get; set; }
        internal DateTime? CreatedAt { get; set; }
        internal TimeSpan? SessionLength { get; set; }
        internal TimeSpan? TimeSpent { get; set; }
        internal TimeSpan? LastInterval { get; set; }

        //events
        internal int EventCount { get; set; }
        internal string EventToken { get; set; }
        internal Dictionary<string, string> CallBackParameters { get; set; }
        internal double AmountInCents { get; set; }

        //defaults
        private AIActivityPackage activityPackage { get; set; }

        //TODO change ToString() to serializer, possible ServiceStack.Text

        internal AIPackageBuilder()
        {
            activityPackage = new AIActivityPackage
            {
                Parameters = new Dictionary<string, string>()
            };
        }

        private void FillDefaults()
        {
            activityPackage.UserAgent = this.UserAgent;
            activityPackage.ClientSdk = this.ClientSdk;
            
            //general
            SaveParameter("created_at"      , CreatedAt);
            SaveParameter("app_token"       , AppToken);
            SaveParameter("mac_sha1"        , MacSha1);
            SaveParameter("mac_md5"         , MacShortMD5);
            SaveParameter("idfa"            , IdForAdvertisers);
            SaveParameter("fb_id"           , FbAttributionId);
            SaveParameter("environment"     , Environment);
            SaveParameter("tracking_enable" , IsTrackingEnable );
            //    //session related (used for events as well)
            SaveParameter("session_count"   , SessionCount);
            SaveParameter("subsession_count", SubSessionCount);
            SaveParameter("session_length"  , SessionLength);
            SaveParameter("time_spent"      , TimeSpent);
        }

        internal AIActivityPackage BuildSessionPackage()
        {
            FillDefaults();
            SaveParameter("last_interval", LastInterval);

            activityPackage.Path = @"/startup";
            activityPackage.Kind = "session start";
            activityPackage.Suffix = "";

            return activityPackage;
        }

        internal AIActivityPackage BuildEventPackage()
        {
            FillDefaults();
            InjectEventParameters();

            activityPackage.Path = @"/event";
            activityPackage.Kind = "event";
            activityPackage.Suffix = this.EventSuffix();

            return activityPackage;
        }

        internal AIActivityPackage BuildRevenuePackage()
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
            SaveParameter("params", CallBackParameters);
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
            var rfc822TimeZone = timeZone.Remove(3,1);
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

        #endregion
    }
}
