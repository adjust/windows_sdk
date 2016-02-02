using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerCycle
    {
        private TimeSpan TimeInterval { get; set; }

        private TimeSpan TimeStart { get; set; }

        private bool IsPaused { get; set; }

        private DateTime? FireDate { get; set; }

        private ActionQueue ActionQueue { get; set; }

        private Action Action { get; set; }

        private CancellationTokenSource CancelDelayTokenSource { get; set; }

        internal TimerCycle(ActionQueue actionQueue, Action action, TimeSpan timeInterval, TimeSpan timeStart)
        {
            ActionQueue = actionQueue;
            Action = action;
            TimeInterval = timeInterval;
            TimeStart = timeStart;
            CancelDelayTokenSource = new CancellationTokenSource();

            // timer initially set as paused
            IsPaused = true;

            //AdjustFactory.Logger.Verbose("TimerCycle Create dueTime:{0}, period:{1}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds);
        }

        // timer triggers 1st event at Resume(), not after the time interval
        internal void Resume()
        {
            if (!IsPaused) return;

            IsPaused = false;
                        
            // start the new timer
            var now = StartTimer(TimeStart);

            //AdjustFactory.Logger.Verbose("TimerCycle Resume dueTime:{0}, period:{1}, fireDate:{2}, now:{3}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds, 
            //    FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));
        }

        internal void Suspend()
        {
            if (IsPaused) return;

            // cancel previous timer
            CancelDelayTokenSource.Cancel();
            CancelDelayTokenSource = new CancellationTokenSource();

            // save the delay of the next fire when restarting
            var now = DateTime.Now;
            TimeStart = FireDate.Value - now;

            //AdjustFactory.Logger.Verbose("TimerCycle Suspend timeStart:{0}, fireDate:{1}, now:{2}", 
            //    TimeStart.TotalMilliseconds, FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));

            IsPaused = true;
        }

        private void TimerCallback()
        {
            ActionQueue.Enqueue(Action);

            // start the new timer
            var now = StartTimer(TimeInterval);

            //AdjustFactory.Logger.Verbose("TimerCycle TimerCallback fireDate:{0}, timeInterval:{1}, now:{2}",
            //    FireDate.Value.ToString("HH:mm:ss.fff"), TimeInterval.TotalMilliseconds, now.ToString("HH:mm:ss.fff"));
        }

        private DateTime StartTimer(TimeSpan fireIn)
        {
            var now = DateTime.Now;
            FireDate = now.Add(fireIn);

            Task.Delay((int)fireIn.TotalMilliseconds, CancelDelayTokenSource.Token).ContinueWith((t) =>
            {
                //AdjustFactory.Logger.Verbose("TimerCycle StartTimer, IsCanceled {0}, IsCompleted{1}, IsFaulted {2}, Status {3} ", t.IsCanceled, t.IsCompleted, t.IsFaulted, t.Status);
                if (t.IsCanceled)
                {
                    return;
                }
                TimerCallback();
            });

            return now;
        }
    }
}