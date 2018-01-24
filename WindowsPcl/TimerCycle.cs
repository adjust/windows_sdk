using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerCycle
    {
        private readonly TimeSpan _timeInterval;
        private TimeSpan _timeStart;
        private bool _isPaused;
        private DateTime? _fireDate;
        private ActionQueue _actionQueue;
        private Action _action;

        private CancellationTokenSource _cancelDelayTokenSource;

        internal TimerCycle(ActionQueue actionQueue, Action action, TimeSpan timeInterval, TimeSpan timeStart)
        {
            _actionQueue = actionQueue;
            _action = action;
            _timeInterval = timeInterval;
            _timeStart = timeStart;
            _cancelDelayTokenSource = new CancellationTokenSource();

            // timer initially set as paused
            _isPaused = true;

            //AdjustFactory.Logger.Verbose("TimerCycle Create dueTime:{0}, period:{1}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds);
        }

        // timer triggers 1st event at Resume(), not after the time interval
        internal void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;
                        
            // start the new timer
            var now = StartTimer(_timeStart);

            //AdjustFactory.Logger.Verbose("TimerCycle Resume dueTime:{0}, period:{1}, fireDate:{2}, now:{3}",
            //    TimeStart.TotalMilliseconds, TimeInterval.TotalMilliseconds, 
            //    FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));
        }

        internal void Suspend()
        {
            if (_isPaused) return;

            // cancel previous timer
            _cancelDelayTokenSource.Cancel();
            _cancelDelayTokenSource = new CancellationTokenSource();

            // save the delay of the next fire when restarting
            var now = DateTime.Now;
            if (_fireDate != null)
            {
                _timeStart = _fireDate.Value - now;
            }

            //AdjustFactory.Logger.Verbose("TimerCycle Suspend timeStart:{0}, fireDate:{1}, now:{2}", 
            //    TimeStart.TotalMilliseconds, FireDate.Value.ToString("HH:mm:ss.fff"), now.ToString("HH:mm:ss.fff"));

            _isPaused = true;
        }

        private void TimerCallback()
        {
            _actionQueue.Enqueue(_action);

            // start the new timer
            var now = StartTimer(_timeInterval);

            //AdjustFactory.Logger.Verbose("TimerCycle TimerCallback fireDate:{0}, timeInterval:{1}, now:{2}",
            //    FireDate.Value.ToString("HH:mm:ss.fff"), TimeInterval.TotalMilliseconds, now.ToString("HH:mm:ss.fff"));
        }

        private DateTime StartTimer(TimeSpan fireIn)
        {
            var now = DateTime.Now;
            _fireDate = now.Add(fireIn);
            
            Task.Delay((int)fireIn.TotalMilliseconds, _cancelDelayTokenSource.Token).ContinueWith(t =>
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

        public void Teardown()
        {
            _cancelDelayTokenSource.Cancel();

            _action = null;
            _actionQueue.Teardown();
            _actionQueue = null;
        }
    }
}