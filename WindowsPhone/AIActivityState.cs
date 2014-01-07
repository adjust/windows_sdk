using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class AIActivityState
    {
        //global counters
        internal int EventCount { get; set; }
        internal int SessionCount { get; set; }

        //session atributes
        internal int SubSessionCount { get; set; }
        internal TimeSpan? SessionLenght { get; set; } // all duration in seconds
        internal TimeSpan? TimeSpent { get; set; }
        internal DateTime? LastActivity { get; set; } // all times in seconds sinze 1970

        internal DateTime? CreatedAt { get; set; }
        internal TimeSpan? LastInterval { get; set; }

        internal AIActivityState()
        {
            EventCount      = 0;
            SessionCount    = 0;
            SubSessionCount = -1; //-1 means unknown
            SessionLenght   = null;
            TimeSpent       = null;
            LastActivity    = null;
            CreatedAt       = null;
            LastInterval    = null;
        }

        internal void ResetSessionAttributes(DateTime now)
        {
            SubSessionCount = 1;
            SessionLenght   = new TimeSpan();
            TimeSpent       = new TimeSpan();
            LastActivity    = now;
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
                writer.Write(Util.SerializeTimeSpanToLong(activity.SessionLenght));
                writer.Write(Util.SerializeTimeSpanToLong(activity.TimeSpent));
                writer.Write(Util.SerializeDatetimeToLong(activity.LastActivity));
                writer.Write(Util.SerializeDatetimeToLong(activity.CreatedAt));
                writer.Write(Util.SerializeTimeSpanToLong(activity.LastInterval));
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
                activity.SessionLenght      = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());
                activity.TimeSpent          = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());
                activity.LastActivity       = Util.DeserializeDateTimeFromLong(reader.ReadInt64());
                activity.CreatedAt          = Util.DeserializeDateTimeFromLong(reader.ReadInt64());
                activity.LastInterval       = Util.DeserializeTimeSpanFromLong(reader.ReadInt64());
            }
            return activity;
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
