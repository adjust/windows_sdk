using System;

namespace AdjustSdk.Pcl
{
    public class BackoffStrategy
    {
        internal int MinRetries { get; private set; }
        internal long TicksMultiplier { get; private set; }
        internal long MaxWaitTicks { get; private set; }
        internal double MinRange { get; private set; }
        internal double MaxRange { get; private set; }

        public static readonly BackoffStrategy LongWait = new BackoffStrategy
        {
            MinRetries = 1,
            TicksMultiplier = TimeSpan.TicksPerMinute * 2,
            MaxWaitTicks = TimeSpan.TicksPerDay,
            MinRange = 0.5,
            MaxRange = 1.0
        };
        public static readonly BackoffStrategy ShortWait = new BackoffStrategy
        {
            MinRetries = 1,
            TicksMultiplier = TimeSpan.TicksPerMillisecond* 200,
            MaxWaitTicks = TimeSpan.TicksPerHour,
            MinRange = 0.5,
            MaxRange = 1.0
        };
        //TestWait,
        //NoWait,
        //NoRetry
    }
}
