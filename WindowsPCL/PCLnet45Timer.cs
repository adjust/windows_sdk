using AdjustSdk.PclNet40;
using System;
using System.Threading;

namespace AdjustSdk.Pcl
{
    internal class TimerPclNet45
    {
        // using wrapper for Timer class, reason: http://stackoverflow.com/questions/12555049/timer-in-portable-library, solution used:
        //  3) Create a new project targeting .NET 4.0 and Windows Store apps, and put the code that requires timer in that.
        //  Then reference that from the .NET 4.5 and Windows Store apps project.

        private TimerPclNet40 TimeKeeper;

        private TimeSpan TimeInterval;
        private bool IsPaused;

        internal TimerPclNet45(TimerPclNet40Callback timerCallback, object state, TimeSpan timeInterval)
        {
            TimeInterval = timeInterval;

            // timer initially set as paused
            IsPaused = true;
            TimeKeeper = new TimerPclNet40(timerCallback, state
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