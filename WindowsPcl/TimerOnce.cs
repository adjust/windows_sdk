using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerOnce
    {
        private ActionQueue _ActionQueue;
        private Action _Action;
        private DateTime? _FireDate;
        private CancellationTokenSource _CancelDelayTokenSource = new CancellationTokenSource();

        internal TimerOnce(ActionQueue actionQueue, Action action)
        {
            _ActionQueue = actionQueue;
            _Action = action;
        }

        internal void StartIn(int milliSecondsDelay)
        {
            // reset current timer if active 
            if (_FireDate.HasValue)
            {
                _CancelDelayTokenSource.Cancel();
                _CancelDelayTokenSource = new CancellationTokenSource();
            }
            // save the next fire date
            _FireDate = DateTime.Now.AddMilliseconds(milliSecondsDelay);
            
            // start/reset timer
            Task.Delay(milliSecondsDelay, _CancelDelayTokenSource.Token).ContinueWith((t) => {
                if (t.IsCanceled) { 
                    return; 
                }
                TimerCallback(); 
            });
        }

        internal TimeSpan FireIn
        {
            get
            {
                if (_FireDate == null)
                {
                    return new TimeSpan(0);
                }
                else
                {
                    return _FireDate.Value - DateTime.Now;
                }
            }
        }

        private void TimerCallback()
        {
            _FireDate = null;
            _ActionQueue.Enqueue(_Action);
        }
    }
}