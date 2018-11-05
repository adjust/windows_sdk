using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AdjustSdk.Pcl
{
    public class ActivityState
    {
        // global counters
        internal int EventCount { get; set; }
        internal int SessionCount { get; set; }

        // session atributes
        internal int SubSessionCount { get; set; }
        internal TimeSpan? SessionLength { get; set; } // all duration in seconds
        internal TimeSpan? TimeSpent { get; set; }
        internal DateTime? LastActivity { get; set; } // all times in seconds sinze 1970
        internal TimeSpan? LastInterval { get; set; }

        // persistent data
        internal Guid Uuid { get; set; }
        internal bool Enabled { get; set; }
        internal bool IsGdprForgotten { get; set; }
        internal bool AskingAttribution { get; set; }
        internal bool UpdatePackages { get; set; }
        internal string PushToken { get; set; }

        internal string Adid { get; set; }

        internal LinkedList<string> PurchaseIds { get; private set; }
        private static int PURCHASE_ID_MAXCOUNT = 10;

        private static readonly string EVENT_COUNT = "EventCount";
        private static readonly string SESSION_COUNT = "SessionCount";
        private static readonly string SUBSESSION_COUNT = "SubSessionCount";
        private static readonly string SESSION_LENGTH = "SessionLenght";
        private static readonly string TIME_SPENT = "TimeSpent";
        private static readonly string LAST_ACTIVITY = "LastActivity";
        private static readonly string LAST_INTERVAL = "LastInterval";
        private static readonly string UUID = "Uuid";
        private static readonly string ENABLED = "Enabled";
        private static readonly string IS_GDPR_FORGOTTEN = "IsGdprForgotten";
        private static readonly string ASKING_ATTRIBUTION = "AskingAttribution";
        private static readonly string UPDATE_PACKAGES = "UpdatePackages";
        private static readonly string PUSH_TOKEN = "PushToken";
        private static readonly string ADID = "Adid";
        private static readonly string PURCHASE_IDS = "PurchaseIds";

        public ActivityState()
        {
            SubSessionCount = -1; // -1 means unknown
            Uuid = Util.Uuid;
            Enabled = true;
            IsGdprForgotten = false;
            AskingAttribution = false;
            UpdatePackages = false;
            Adid = null;
            PurchaseIds = null;
        }

        internal void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLength = new TimeSpan();
            TimeSpent = new TimeSpan();
            LastActivity = now;
            LastInterval = null;
        }

        public void AddPurchaseId(string purchaseId)
        {
            if (PurchaseIds == null)
                PurchaseIds = new LinkedList<string>();

            if (PurchaseIds.Count >= PURCHASE_ID_MAXCOUNT)
                PurchaseIds.RemoveLast();

            PurchaseIds.AddFirst(purchaseId);
        }

        public bool FindPurchaseId(string purchaseId)
        {
            if (PurchaseIds == null)
                return false;

            return PurchaseIds.Contains(purchaseId);
        }

        public override string ToString()
        {
            return Util.F("ec:{0} sc:{1} ssc:{2} sl:{3:.0} ts:{4:.0} la:{5:.0} adid:{6} isGdprForgotten:{7}",
                EventCount,
                SessionCount,
                SubSessionCount,
                SessionLength.SecondsFormat(),
                TimeSpent.SecondsFormat(),
                LastActivity.SecondsFormat(),
                Adid,
                IsGdprForgotten
            );
        }

        public static Dictionary<string, object> ToDictionary(ActivityState activityState)
        {
            string purchaseIdsString = null;
            if (activityState.PurchaseIds != null)
                purchaseIdsString = JsonConvert.SerializeObject(activityState.PurchaseIds);

            return new Dictionary<string, object>
            {
                {EVENT_COUNT, activityState.EventCount},
                {SESSION_COUNT, activityState.SessionCount},
                {SUBSESSION_COUNT, activityState.SubSessionCount},
                {SESSION_LENGTH, activityState.SessionLength},
                {TIME_SPENT, activityState.TimeSpent},
                {LAST_ACTIVITY, activityState.LastActivity?.Ticks ?? DateTime.MinValue.Ticks},
                {LAST_INTERVAL, activityState.LastInterval},
                {UUID, activityState.Uuid},
                {ENABLED, activityState.Enabled},
                {IS_GDPR_FORGOTTEN, activityState.IsGdprForgotten},
                {ASKING_ATTRIBUTION, activityState.AskingAttribution},
                {UPDATE_PACKAGES, activityState.UpdatePackages},
                {PUSH_TOKEN, activityState.PushToken},
                {ADID, activityState.Adid},
                {PURCHASE_IDS, purchaseIdsString }
            };
        }
        
        public static ActivityState FromDictionary(Dictionary<string, object> activityStateObjectMap)
        {
            var activityState = new ActivityState
            {
                EventCount = activityStateObjectMap.ContainsKey(EVENT_COUNT) ? (int) activityStateObjectMap[EVENT_COUNT] : 0,
                SessionCount = activityStateObjectMap.ContainsKey(SESSION_COUNT) ? (int) activityStateObjectMap[SESSION_COUNT] : 0,
                SubSessionCount = activityStateObjectMap.ContainsKey(SUBSESSION_COUNT) ? (int) activityStateObjectMap[SUBSESSION_COUNT] : 0,
                SessionLength = activityStateObjectMap.ContainsKey(SESSION_LENGTH) ? activityStateObjectMap[SESSION_LENGTH] as TimeSpan? : TimeSpan.Zero,
                TimeSpent = activityStateObjectMap.ContainsKey(TIME_SPENT) ? activityStateObjectMap[TIME_SPENT] as TimeSpan? : TimeSpan.Zero,
                LastInterval = activityStateObjectMap.ContainsKey(LAST_INTERVAL) ? activityStateObjectMap[LAST_INTERVAL] as TimeSpan? : null,
                Uuid = activityStateObjectMap.ContainsKey(UUID) ? (Guid) activityStateObjectMap[UUID] : default(Guid),
                Enabled = activityStateObjectMap.ContainsKey(ENABLED) ? (bool) activityStateObjectMap[ENABLED] : false,
                IsGdprForgotten = activityStateObjectMap.ContainsKey(IS_GDPR_FORGOTTEN) ? (bool)activityStateObjectMap[IS_GDPR_FORGOTTEN] : false,
                AskingAttribution = activityStateObjectMap.ContainsKey(ASKING_ATTRIBUTION) ? (bool) activityStateObjectMap[ASKING_ATTRIBUTION] : false,
                UpdatePackages = activityStateObjectMap.ContainsKey(UPDATE_PACKAGES) ? (bool) activityStateObjectMap[UPDATE_PACKAGES] : false,
                PushToken = activityStateObjectMap.ContainsKey(PUSH_TOKEN) ? activityStateObjectMap[PUSH_TOKEN] as string : null,
                Adid = activityStateObjectMap.ContainsKey(ADID) ? activityStateObjectMap[ADID] as string : null
            };

            var lastActivityTicks = (long)activityStateObjectMap[LAST_ACTIVITY];
            var lastActivity = new DateTime(lastActivityTicks);
            if (lastActivity != DateTime.MinValue)
                activityState.LastActivity = lastActivity;

            object purchaseIdsJson;
            if (activityStateObjectMap.TryGetValue(PURCHASE_IDS, out purchaseIdsJson))
                activityState.PurchaseIds =
                    JsonConvert.DeserializeObject<LinkedList<string>>(purchaseIdsJson.ToString());

            return activityState;
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static ActivityState DeserializeFromStreamLegacy(Stream stream)
        {
            ActivityState activity = null;
            var reader = new BinaryReader(stream);

            activity = new ActivityState();
            
            activity.EventCount = reader.ReadInt32();
            activity.SessionCount = reader.ReadInt32();
            activity.SubSessionCount = reader.ReadInt32();
            activity.SessionLength = DeserializeTimeSpanFromLong(reader.ReadInt64());
            activity.TimeSpent = DeserializeTimeSpanFromLong(reader.ReadInt64());
            activity.LastActivity = DeserializeDateTimeFromLong(reader.ReadInt64());
            DeserializeDateTimeFromLong(reader.ReadInt64()); // created at
            activity.LastInterval = DeserializeTimeSpanFromLong(reader.ReadInt64());

            // create Uuid for migrating devices
            activity.Uuid = Util.TryRead(() => Guid.Parse(reader.ReadString()), () => Guid.NewGuid());
            // default value of IsEnabled for migrating devices
            activity.Enabled = Util.TryRead(() => reader.ReadBoolean(), () => true);
            // default value for AskingAttribution for migrating devices
            activity.AskingAttribution = Util.TryRead(() => reader.ReadBoolean(), () => false);

            return activity;
        }

        private static TimeSpan? DeserializeTimeSpanFromLong(long ticks)
        {
            if (ticks == -1)
                return null;
            return new TimeSpan(ticks);
        }

        private static DateTime? DeserializeDateTimeFromLong(long ticks)
        {
            if (ticks == -1)
                return null;
            return new DateTime(ticks);
        }
    }
}