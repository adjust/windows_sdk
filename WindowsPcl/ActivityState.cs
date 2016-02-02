using System;
using System.IO;

namespace AdjustSdk.Pcl
{
    public class ActivityState
    {
        // global counters
        public int EventCount { get; set; }

        public int SessionCount { get; set; }

        // session atributes
        public int SubSessionCount { get; set; }

        public TimeSpan? SessionLenght { get; set; } // all duration in seconds

        public TimeSpan? TimeSpent { get; set; }

        public DateTime? LastActivity { get; set; } // all times in seconds sinze 1970

        public DateTime? CreatedAt { get; set; }

        public TimeSpan? LastInterval { get; set; }

        // persistent data
        public Guid Uuid { get; set; }

        public bool Enabled { get; set; }

        public bool AskingAttribution { get; set; }

        public ActivityState()
        {
            EventCount = 0;
            SessionCount = 0;
            SubSessionCount = -1; // -1 means unknown
            SessionLenght = null;
            TimeSpent = null;
            LastActivity = null;
            CreatedAt = null;
            LastInterval = null;
            Uuid = Guid.NewGuid();
            Enabled = true;
            AskingAttribution = false;
        }

        public void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLenght = new TimeSpan();
            TimeSpent = new TimeSpan();
            LastActivity = now;
            CreatedAt = null;
            LastInterval = null;
        }

        public ActivityState Clone()
        {
            // TODO check if Timespans and Datetimes are altered by the original activity state
            return (ActivityState)this.MemberwiseClone();
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

        // does not close stream received. Caller is responsible to close if it wants it
        public static void SerializeToStream(Stream stream, ActivityState activity)
        {
            var writer = new BinaryWriter(stream);

            writer.Write(activity.EventCount);
            writer.Write(activity.SessionCount);
            writer.Write(activity.SubSessionCount);
            writer.Write(Util.SerializeTimeSpanToLong(activity.SessionLenght));
            writer.Write(Util.SerializeTimeSpanToLong(activity.TimeSpent));
            writer.Write(Util.SerializeDatetimeToLong(activity.LastActivity));
            writer.Write(Util.SerializeDatetimeToLong(activity.CreatedAt));
            writer.Write(Util.SerializeTimeSpanToLong(activity.LastInterval));
            writer.Write(activity.Uuid.ToString());
            writer.Write(activity.Enabled);
            writer.Write(activity.AskingAttribution);
        }

        // does not close stream received. Caller is responsible to close if it wants it
        public static ActivityState DeserializeFromStream(Stream stream)
        {
            ActivityState activity = null;
            var reader = new BinaryReader(stream);

            activity = new ActivityState();
            activity.EventCount = reader.ReadInt32();
            activity.SessionCount = reader.ReadInt32();
            activity.SubSessionCount = reader.ReadInt32();
            activity.SessionLenght = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());
            activity.TimeSpent = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());
            activity.LastActivity = Util.DeserializeDateTimeFromLong(reader.ReadInt64());
            activity.CreatedAt = Util.DeserializeDateTimeFromLong(reader.ReadInt64());
            activity.LastInterval = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());

            // create Uuid for migrating devices
            activity.Uuid = Util.TryRead(() => Guid.Parse(reader.ReadString()), () => Guid.NewGuid());
            // default value of IsEnabled for migrating devices
            activity.Enabled = Util.TryRead(() => reader.ReadBoolean(), () => true);
            // default value for AskingAttribution for migrating devices
            activity.AskingAttribution = Util.TryRead(() => reader.ReadBoolean(), () => false);

            return activity;
        }

        #endregion Serialization
    }
}