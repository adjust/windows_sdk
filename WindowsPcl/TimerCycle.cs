using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerCycle
    {
        private TimeSpan _TimeInterval;
        private TimeSpan _TimeStart;
        private bool _IsPaused;
        private DateTime? _FireDate;
        private ActionQueue _ActionQueue;
        private Action _Action;

        private CancellationTokenSource _CancelDelayTokenSource;

        internal TimerCycle(ActionQueue actionQueue, Action action, TimeSpan timeInterval, TimeSpan timeStart)
        {
            _ActionQueue = actionQueue;
            _Action = action;
            _TimeInterval = timeInterval;
            _TimeStart = timeStart;
            _CancelDelayTokenSource = new CancellationTokenSource();

            // timer initially set as paused
            _IsPaused = true;

            //AdjustFactory.Logger.Verbose("TimerCycle Create dueTime:{0}, period:{1}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds);
        }

        // timer triggers 1st event at Resume(), not after the time interval
        internal void Resume()
        {
            if (!_IsPaused) return;

            _IsPaused = false;
                        
            // start the new timer
            var now = StartTimer(_TimeStart);

            //AdjustFactory.Logger.Verbose("TimerCycle Resume dueTime:{0}, period:{1}, fireDate:{2}, now:{3}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds, 
            //    FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));
        }

        internal void Suspend()
        {
            if (_IsPaused) return;

            // cancel previous timer
            _CancelDelayTokenSource.Cancel();
            _CancelDelayTokenSource = new CancellationTokenSource();

            // save the delay of the next fire when restarting
            var now = DateTime.Now;
            _TimeStart = _FireDate.Value - now;

            //AdjustFactory.Logger.Verbose("TimerCycle Suspend timeStart:{0}, fireDate:{1}, now:{2}", 
            //    TimeStart.TotalMilliseconds, FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));

            _IsPaused = true;
        }

        private void TimerCallback()
        {
            _ActionQueue.Enqueue(_Action);

            // start the new timer
            var now = StartTimer(_TimeInterval);

            //AdjustFactory.Logger.Verbose("TimerCycle TimerCallback fireDate:{0}, timeInterval:{1}, now:{2}",
            //    FireDate.Value.ToString("HH:mm:ss.fff"), TimeInterval.TotalMilliseconds, now.ToString("HH:mm:ss.fff"));
        }

        private DateTime StartTimer(TimeSpan fireIn)
        {
            var now = DateTime.Now;
            _FireDate = now.Add(fireIn);

            Task.Delay((int)fireIn.TotalMilliseconds, _CancelDelayTokenSource.Token).ContinueWith((t) =>
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