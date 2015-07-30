using AdjustSdk.PclNet40;
using System;
using System.Threading;

namespace AdjustSdk.Pcl
{
    internal class TimerCycle
    {
        // using wrapper for Timer class, reason: http://stackoverflow.com/questions/12555049/timer-in-portable-library, solution used:
        //  3) Create a new project targeting .NET 4.0 and Windows Store apps, and put the code that requires timer in that.
        //  Then reference that from the .NET 4.5 and Windows Store apps project.

        private TimerPclNet40 TimeKeeper;

        private TimeSpan TimeInterval { get; set; }

        private TimeSpan TimeStart { get; set; }

        private bool IsPaused { get; set; }

        private DateTime? FireDate { get; set; }

        private ActionQueue ActionQueue { get; set; }

        private Action Action { get; set; }

        internal TimerCycle(ActionQueue actionQueue, Action action, TimeSpan timeInterval, TimeSpan timeStart)
        {
            ActionQueue = actionQueue;
            Action = action;
            TimeInterval = timeInterval;
            TimeStart = timeStart;

            // timer initially set as paused
            IsPaused = true;

            TimeKeeper = new TimerPclNet40(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            //AdjustFactory.Logger.Verbose("TimerCycle Create dueTime:{0}, period:{1}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds);
        }

        // timer triggers 1st event at Resume(), not after the time interval
        internal void Resume()
        {
            if (!IsPaused) return;

            TimeKeeper.Change(dueTime: TimeStart, period: TimeInterval);

            // save the date of the first fire
            var now = DateTime.Now;
            FireDate = now.Add(TimeStart);

            //AdjustFactory.Logger.Verbose("TimerCycle Resume dueTime:{0}, period:{1}, fireDate:{2}, now:{3}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds, 
            //    FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));

            IsPaused = false;
        }

        internal void Suspend()
        {
            if (IsPaused) return;

            // save the delay of the next fire when restarting
            var now = DateTime.Now;
            TimeStart = FireDate.Value - now;

            TimeKeeper.Change(dueTime: Timeout.Infinite, period: Timeout.Infinite);

            //AdjustFactory.Logger.Verbose("TimerCycle Suspend timeStart:{0}, fireDate:{1}, now:{2}", 
            //    TimeStart.TotalMilliseconds, FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));

            IsPaused = true;
        }

        private void TimerCallback(object state)
        {
            // save the date of the next fire
            var now = DateTime.Now;
            FireDate = now + TimeInterval;

            //AdjustFactory.Logger.Verbose("TimerCycle TimerCallback fireDate:{0}, timeInterval:{1}, now:{2}",
            //    FireDate.Value.ToString("HH:mm:ss.fff"), TimeInterval.TotalMilliseconds, now.ToString("HH:mm:ss.fff"));

            ActionQueue.Enqueue(Action);
        }
    }
}