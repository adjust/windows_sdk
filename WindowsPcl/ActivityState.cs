﻿using System;
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
        internal TimeSpan? SessionLenght { get; set; } // all duration in seconds
        internal TimeSpan? TimeSpent { get; set; }
        internal DateTime? LastActivity { get; set; } // all times in seconds sinze 1970
        internal TimeSpan? LastInterval { get; set; }

        // persistent data
        internal Guid Uuid { get; set; }
        internal bool Enabled { get; set; }
        internal bool AskingAttribution { get; set; }
        internal bool UpdatePackages { get; set; }
        internal string PushToken { get; set; }

        internal string Adid { get; set; }

        internal LinkedList<string> PurchaseIds { get; private set; }
        private static int PURCHASE_ID_MAXCOUNT = 10;

        public ActivityState()
        {
            SubSessionCount = -1; // -1 means unknown
            Uuid = Guid.NewGuid();
            Enabled = true;
            AskingAttribution = false;
            UpdatePackages = false;
            Adid = null;
            PurchaseIds = null;
        }

        internal void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLenght = new TimeSpan();
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
            return Util.F("ec:{0} sc:{1} ssc:{2} sl:{3:.0} ts:{4:.0} la:{5:.0} adid:{6}",
                EventCount,
                SessionCount,
                SubSessionCount,
                SessionLenght.SecondsFormat(),
                TimeSpent.SecondsFormat(),
                LastActivity.SecondsFormat(),
                Adid
            );
        }

        public static Dictionary<string, object> ToDictionary(ActivityState activityState)
        {
            string purchaseIdsString = null;
            if (activityState.PurchaseIds != null)
                purchaseIdsString = JsonConvert.SerializeObject(activityState.PurchaseIds);

            return new Dictionary<string, object>
            {
                {"EventCount", activityState.EventCount},
                {"SessionCount", activityState.SessionCount},
                {"SubSessionCount", activityState.SubSessionCount},
                {"SessionLenght", activityState.SessionLenght},
                {"TimeSpent", activityState.TimeSpent},
                {"LastActivity", activityState.LastActivity?.Ticks ?? DateTime.MinValue.Ticks},
                {"LastInterval", activityState.LastInterval},
                {"Uuid", activityState.Uuid},
                {"Enabled", activityState.Enabled},
                {"AskingAttribution", activityState.AskingAttribution},
                {"UpdatePackages", activityState.UpdatePackages},
                {"PushToken", activityState.PushToken},
                {"Adid", activityState.Adid},
                {"PurchaseIds", purchaseIdsString }
            };
        }
        
        public static ActivityState FromDictionary(Dictionary<string, object> activityStateObjectMap)
        {
            var activityState = new ActivityState
            {
                EventCount = activityStateObjectMap.ContainsKey("EventCount") ? (int) activityStateObjectMap["EventCount"] : 0,
                SessionCount = activityStateObjectMap.ContainsKey("SessionCount") ? (int) activityStateObjectMap["SessionCount"] : 0,
                SubSessionCount = activityStateObjectMap.ContainsKey("SubSessionCount") ? (int) activityStateObjectMap["SubSessionCount"] : 0,
                SessionLenght = activityStateObjectMap.ContainsKey("SessionLenght") ? activityStateObjectMap["SessionLenght"] as TimeSpan? : TimeSpan.Zero,
                TimeSpent = activityStateObjectMap.ContainsKey("TimeSpent") ? activityStateObjectMap["TimeSpent"] as TimeSpan? : TimeSpan.Zero,
                LastInterval = activityStateObjectMap.ContainsKey("LastInterval") ? activityStateObjectMap["LastInterval"] as TimeSpan? : null,
                Uuid = activityStateObjectMap.ContainsKey("Uuid") ? (Guid) activityStateObjectMap["Uuid"] : default(Guid),
                Enabled = activityStateObjectMap.ContainsKey("Enabled") ? (bool) activityStateObjectMap["Enabled"] : false,
                AskingAttribution = activityStateObjectMap.ContainsKey("AskingAttribution") ? (bool) activityStateObjectMap["AskingAttribution"] : false,
                UpdatePackages = activityStateObjectMap.ContainsKey("UpdatePackages") ? (bool) activityStateObjectMap["UpdatePackages"] : false,
                PushToken = activityStateObjectMap.ContainsKey("PushToken") ? activityStateObjectMap["PushToken"] as string : null,
                Adid = activityStateObjectMap.ContainsKey("Adid") ? activityStateObjectMap["Adid"] as string : null
            };

            var lastActivityTicks = (long)activityStateObjectMap["LastActivity"];
            var lastActivity = new DateTime(lastActivityTicks);
            if (lastActivity != DateTime.MinValue)
                activityState.LastActivity = lastActivity;

            object purchaseIdsJson;
            if (activityStateObjectMap.TryGetValue("PurchaseIds", out purchaseIdsJson))
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
            activity.SessionLenght = DeserializeTimeSpanFromLong(reader.ReadInt64());
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