using adeven.AdjustIo.PCLnet40;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    class AITimer
    {
        //using wrapper for Timer, reason: http://stackoverflow.com/questions/12555049/timer-in-portable-library
        private PCLTimer TimeKeeper;
        private TimeSpan TimeInterval;
        private bool IsSuspended;

        internal AITimer(PCLTimerCallback timerCallback, object state, TimeSpan timeInterval)
        {
            TimeInterval = timeInterval;
            
            IsSuspended = true;
            TimeKeeper = new PCLTimer(timerCallback, state
                , Timeout.Infinite, Timeout.Infinite);
        }

        internal void Resume()
        {
            if (!IsSuspended) return;

            TimeKeeper.Change(0, (int)TimeInterval.TotalMilliseconds);

            IsSuspended = false;
        }

        internal void Suspend()
        {
            if (IsSuspended) return;

            TimeKeeper.Change(Timeout.Infinite, Timeout.Infinite);

            IsSuspended = true;
        }
    }
}
