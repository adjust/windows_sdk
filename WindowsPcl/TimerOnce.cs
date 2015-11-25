using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class TimerOnce
    {
        private ActionQueue ActionQueue { get; set; }

        private Action Action { get; set; }

        private DateTime? FireDate { get; set; }

        private CancellationTokenSource CancelDelayTokenSource { get; set; }

        internal TimerOnce(ActionQueue actionQueue, Action action)
        {
            ActionQueue = actionQueue;
            Action = action;

            CancelDelayTokenSource = new CancellationTokenSource();
        }

        internal void StartIn(int milliSecondsDelay)
        {
            // reset current timer if active 
            if (FireDate.HasValue)
            {
                CancelDelayTokenSource.Cancel();
                CancelDelayTokenSource = new CancellationTokenSource();
            }
            // save the next fire date
            FireDate = DateTime.Now.AddMilliseconds(milliSecondsDelay);
            
            // start/reset timer
            Task.Delay(milliSecondsDelay, CancelDelayTokenSource.Token).ContinueWith((t) => {
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

        private void TimerCallback()
        {
            FireDate = null;
            ActionQueue.Enqueue(Action);
        }
    }
}