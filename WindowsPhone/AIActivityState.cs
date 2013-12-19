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
        public double SessionLenght { get; set; } // all duration in seconds
        public double TimeSpent { get; set; }
        public double LastActivity { get; set; } // all times in seconds sinze 1970

        public double CreatedAt { get; set; }
        public double LastInterval { get; set; }

        public AIActivityState()
        {
            EventCount      = 0;
            SessionCount    = 0;
            SubSessionCount = -1; //-1 means unknown
            SessionLenght   = -1;
            TimeSpent       = -1;
            LastActivity    = -1;
            CreatedAt       = -1;
            LastInterval    = -1;
        }

        public void ResetSessionAttributes(double nowInSeconds)
        {
            SubSessionCount = 1;
            SessionLenght   = 0;
            TimeSpent       = 0;
            LastActivity    = nowInSeconds;
            CreatedAt       = -1;
            LastInterval    = 1;
        }

        public void InjectSessionAttributes(AIPackageBuilder packageBuilder)
        {
            InjectGeneralAttributes(packageBuilder);
            packageBuilder.LastInterval = LastInterval;
        }

        public static void SerializeToStream(Stream stream, AIActivityState activity)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(activity.EventCount);
                writer.Write(activity.SessionCount);
                writer.Write(activity.SubSessionCount);
                writer.Write(activity.SessionLenght);
                writer.Write(activity.TimeSpent);
                writer.Write(activity.LastActivity);
                writer.Write(activity.CreatedAt );
                writer.Write(activity.LastInterval);
            }
        }

        public static AIActivityState DeserializeFromStream(Stream stream)
        {
            AIActivityState activity = null;
            using (var reader = new BinaryReader(stream))
            {
                activity = new AIActivityState();
                activity.EventCount = reader.ReadInt32();
                activity.SessionCount = reader.ReadInt32();
                activity.SubSessionCount = reader.ReadInt32();
                activity.SessionLenght = reader.ReadDouble();
                activity.TimeSpent = reader.ReadDouble();
                activity.LastActivity = reader.ReadDouble();
                activity.CreatedAt = reader.ReadDouble();
                activity.LastInterval = reader.ReadDouble();
            }
            return activity;
        }

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
