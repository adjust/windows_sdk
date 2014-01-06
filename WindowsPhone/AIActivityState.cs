using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    public class AIActivityState
    {
        //global counters
        public int EventCount { get; set; }
        public int SessionCount { get; set; }

        //session atributes
        public int SubSessionCount { get; set; }
        public TimeSpan? SessionLenght { get; set; } // all duration in seconds
        public TimeSpan? TimeSpent { get; set; }
        public DateTime? LastActivity { get; set; } // all times in seconds sinze 1970

        public DateTime? CreatedAt { get; set; }
        public TimeSpan? LastInterval { get; set; }

        internal AIActivityState()
        {
            EventCount      = 0;
            SessionCount    = 0;
            SubSessionCount = -1; //-1 means unknown
            SessionLenght   = null;
            TimeSpent       = null;
            LastActivity    = null;
            AILogger.Verbose("LastActivity initialized");
            CreatedAt       = null;
            LastInterval    = null;
        }

        internal void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLenght   = new TimeSpan();
            TimeSpent       = new TimeSpan();
            LastActivity    = now;
            AILogger.Verbose("LastActivity updated: {0}", LastActivity);
            CreatedAt       = null;
            LastInterval    = null;
        }

        internal void InjectSessionAttributes(AIPackageBuilder packageBuilder)
        {
            InjectGeneralAttributes(packageBuilder);
            packageBuilder.LastInterval = LastInterval;
        }

        internal void InjectEventAttributes(AIPackageBuilder packageBuilder)
        {
            InjectGeneralAttributes(packageBuilder);
            packageBuilder.EventCount = EventCount;
        }

        public override string ToString()
        {
            return String.Format("ec:{0} sc:{1} ssc:{2} sl:{3:.0} ts:{4:.0} la:{5:.0}",
                EventCount,
                SessionCount,
                SubSessionCount,
                SessionLenght.SecondsFormat(),
                TimeSpent.SecondsFormat(),
                LastActivity.SecondsFormat()
            );
        }

        #region Serialization
        internal static void SerializeToStream(Stream stream, AIActivityState activity)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(activity.EventCount);
                writer.Write(activity.SessionCount);
                writer.Write(activity.SubSessionCount);
                writer.Write(SerializeTimeSpan(activity.SessionLenght));
                writer.Write(SerializeTimeSpan(activity.TimeSpent));
                writer.Write(SerializeDatetime(activity.LastActivity));
                AILogger.Verbose("LastActivity write: {0}", activity.LastActivity);
                writer.Write(SerializeDatetime(activity.CreatedAt ));
                writer.Write(SerializeTimeSpan(activity.LastInterval));
            }
        }

        internal static AIActivityState DeserializeFromStream(Stream stream)
        {
            AIActivityState activity = null;
            using (var reader = new BinaryReader(stream))
            {
                activity                    = new AIActivityState();
                activity.EventCount         = reader.ReadInt32();
                activity.SessionCount       = reader.ReadInt32();
                activity.SubSessionCount    = reader.ReadInt32();
                activity.SessionLenght      = DeserializeTimeSpan(reader.ReadInt64());
                activity.TimeSpent          = DeserializeTimeSpan(reader.ReadInt64());
                activity.LastActivity       = DeserializeDateTime(reader.ReadInt64());
                AILogger.Verbose("LastActivity read: {0}", activity.LastActivity);
                activity.CreatedAt          = DeserializeDateTime(reader.ReadInt64());
                activity.LastInterval       = DeserializeTimeSpan(reader.ReadInt64());
            }
            return activity;
        }

        private static Int64 SerializeTimeSpan(TimeSpan? timeSpan)
        {
            if (timeSpan.HasValue)
                return timeSpan.Value.Ticks;
            else
                return -1;
        }

        private static Int64 SerializeDatetime(DateTime? dateTime)
        {
            if (dateTime.HasValue)
                return dateTime.Value.Ticks;
            else
                return -1;
        }

        private static TimeSpan? DeserializeTimeSpan(Int64 ticks)
        {
            if (ticks == -1)
                return null;
            else
                return new TimeSpan(ticks);
        }

        private static DateTime? DeserializeDateTime(Int64 ticks)
        {
            if (ticks == -1)
                return null;
            else
                return new DateTime(ticks);
        }

        #endregion

        private void InjectGeneralAttributes(AIPackageBuilder packageBuilder)
        {
            packageBuilder.SessionCount = SessionCount;
            packageBuilder.SubSessionCount = SubSessionCount;
            packageBuilder.SessionLength = SessionLenght;
            packageBuilder.TimeSpent = TimeSpent;
            packageBuilder.CreatedAt = CreatedAt;
        }
    }
}
