using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerOnce
    {
        private ActionQueue _actionQueue;
        private Action _action;
        private DateTime? _fireDate;
        private CancellationTokenSource _cancelDelayTokenSource = new CancellationTokenSource();

        internal TimerOnce(ActionQueue actionQueue, Action action)
        {
            _actionQueue = actionQueue;
            _action = action;
        }

        internal void StartIn(TimeSpan delay)
        {
            // reset current timer if active 
            if (_fireDate.HasValue)
            {
                Cancel();
            }
            // save the next fire date
            _fireDate = DateTime.Now.Add(delay);
            
            // start/reset timer
            Task.Delay(delay, _cancelDelayTokenSource.Token).ContinueWith(t => {
                _fireDate = null;

                if (t.IsCanceled) { 
                    return; 
                }
                _actionQueue.Enqueue(_action);
            });
        }

        internal TimeSpan FireIn
        {
            get
            {
                if (_fireDate == null)
                {
                    return TimeSpan.Zero;
                }

                return _fireDate.Value - DateTime.Now;
            }
        }

        internal void Cancel()
        {
            _cancelDelayTokenSource.Cancel();
            _cancelDelayTokenSource = new CancellationTokenSource();
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