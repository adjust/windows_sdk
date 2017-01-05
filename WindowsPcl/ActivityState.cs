using System;
using System.Collections.Generic;
using System.IO;

namespace AdjustSdk.Pcl
{
    public class ActivityState : VersionedSerializable
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
        
        public ActivityState()
        {
            SubSessionCount = -1; // -1 means unknown
            Uuid = Guid.NewGuid();
            Enabled = true;
            AskingAttribution = false;
            UpdatePackages = false;
        }

        internal void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLenght = new TimeSpan();
            TimeSpent = new TimeSpan();
            LastActivity = now;
            LastInterval = null;
        }

        public override string ToString()
        {
            return Util.f("ec:{0} sc:{1} ssc:{2} sl:{3:.0} ts:{4:.0} la:{5:.0}",
                EventCount,
                SessionCount,
                SubSessionCount,
                SessionLenght.SecondsFormat(),
                TimeSpent.SecondsFormat(),
                LastActivity.SecondsFormat()
            );
        }

        #region Serialization
        internal override Dictionary<string, Tuple<SerializableType, object>> GetSerializableFields()
        {
            var serializableFields = new Dictionary<string, Tuple<SerializableType, object>>(7);

            AddField(serializableFields, "EventCount", EventCount);
            AddField(serializableFields, "SessionCount", SessionCount);
            AddField(serializableFields, "SubSessionCount", SubSessionCount);
            AddField(serializableFields, "SessionLenght", SessionLenght);
            AddField(serializableFields, "TimeSpent", TimeSpent);
            AddField(serializableFields, "LastActivity", LastActivity);
            AddField(serializableFields, "LastInterval", LastInterval);
            AddField(serializableFields, "Uuid", Uuid.ToString());
            AddField(serializableFields, "Enabled", Enabled);
            AddField(serializableFields, "AskingAttribution", AskingAttribution);
            AddField(serializableFields, "UpdatePackages", UpdatePackages);
            return serializableFields;
        }

        internal override void InitWithSerializedFields(int version, Dictionary<string, object> serializedFields)
        {
            EventCount = GetFieldValueInt(serializedFields, "EventCount", EventCount);
            SessionCount = GetFieldValueInt(serializedFields, "SessionCount", SessionCount);
            SubSessionCount = GetFieldValueInt(serializedFields, "SubSessionCount", SubSessionCount);
            SessionLenght = GetFieldValueTimeSpan(serializedFields, "SessionLenght");
            TimeSpent = GetFieldValueTimeSpan(serializedFields, "TimeSpent");
            LastActivity = GetFieldValueDateTime(serializedFields, "LastActivity");
            LastInterval = GetFieldValueTimeSpan(serializedFields, "LastInterval");

            var uuidString = GetFieldValueString(serializedFields, "Uuid");
            Uuid = uuidString != null ? Guid.Parse(uuidString) : Guid.NewGuid();

            Enabled = GetFieldValueBool(serializedFields, "Enabled", defaultValue: true);
            AskingAttribution = GetFieldValueBool(serializedFields, "AskingAttribution", defaultValue: false);
            UpdatePackages = GetFieldValueBool(serializedFields, "UpdatePackages", defaultValue: false);
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
            else
                return new TimeSpan(ticks);
        }

        private static DateTime? DeserializeDateTimeFromLong(long ticks)
        {
            if (ticks == -1)
                return null;
            else
                return new DateTime(ticks);
        }
        #endregion Serialization
    }
}