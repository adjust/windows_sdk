using AdjustSdk.PclNet40;
using System;
using System.Threading;

namespace AdjustSdk.Pcl
{
    internal class TimerOnce
    {
        // using wrapper for Timer class, reason: http://stackoverflow.com/questions/12555049/timer-in-portable-library, solution used:
        //  3) Create a new project targeting .NET 4.0 and Windows Store apps, and put the code that requires timer in that.
        private TimerPclNet40 TimeKeeper { get; set; }

        private ActionQueue ActionQueue { get; set; }

        private Action Action { get; set; }

        private DateTime? FireDate { get; set; }

        internal TimerOnce(ActionQueue actionQueue, Action action)
        {
            ActionQueue = actionQueue;
            Action = action;

            TimeKeeper = new TimerPclNet40(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        internal void StartIn(int milliSecondsDelay)
        {
            // save the next fire date
            FireDate = DateTime.Now.AddMilliseconds(milliSecondsDelay);
            // start/reset timer
            TimeKeeper.Change(dueTime: milliSecondsDelay, period: Timeout.Infinite);
        }

        internal TimeSpan FireIn
        {
            get
            {
                if (FireDate == null)
                {
                    return new TimeSpan(0);
                }
                else
                {
                    return FireDate.Value - DateTime.Now;
                }
            }
        }

        private void TimerCallback(object state)
        {
            ActionQueue.Enqueue(Action);
            FireDate = null;
        }
    }
}