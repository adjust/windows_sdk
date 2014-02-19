using AdjustSdk.PCLnet40;
using System;
using System.Threading;

namespace AdjustSdk.PCL
{
    internal class PCLnet45Timer
    {
        // using wrapper for Timer class, reason: http://stackoverflow.com/questions/12555049/timer-in-portable-library, solution used:
        //  3) Create a new project targeting .NET 4.0 and Windows Store apps, and put the code that requires timer in that.
        //  Then reference that from the .NET 4.5 and Windows Store apps project.

        private PCLnet40Timer TimeKeeper;

        private TimeSpan TimeInterval;
        private bool IsPaused;

        internal PCLnet45Timer(PCLnet40TimerCallback timerCallback, object state, TimeSpan timeInterval)
        {
            TimeInterval = timeInterval;

            // timer initially set as paused
            IsPaused = true;
            TimeKeeper = new PCLnet40Timer(timerCallback, state
                , Timeout.Infinite, Timeout.Infinite);
        }

        // timer triggers 1st event at Resume(), not after the time interval
        internal void Resume()
        {
            if (!IsPaused) return;

            TimeKeeper.Change(0, (int)TimeInterval.TotalMilliseconds);

            IsPaused = false;
        }

        internal void Pause()
        {
            if (IsPaused) return;

            TimeKeeper.Change(Timeout.Infinite, Timeout.Infinite);

            IsPaused = true;
        }
    }
}