using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace adeven.AdjustIo
{
    class AITimer
    {
        private Timer TimeKeeper;
        private TimeSpan TimeInterval;
        private bool IsSuspended;

        private object State;

        internal AITimer(TimerCallback timerCallback, object state, TimeSpan timeInterval)
        {
            TimeInterval = timeInterval;
            
            IsSuspended = true;
            TimeKeeper = new Timer(timerCallback, state
                , Timeout.Infinite, Timeout.Infinite);
        }

        internal void Resume()
        {
            if (!IsSuspended) return;

            //todo ask welle if timer should fire after Resume or wait until interval 
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
