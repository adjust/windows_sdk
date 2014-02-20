using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.PclNet40
{
    public delegate void TimerPclNet40Callback(object state);

    //wrapper for System.Threading.Timer Class
    public class TimerPclNet40 : IDisposable
    {
        private Timer _timer;

        public TimerPclNet40(TimerPclNet40Callback callback, object state, int dueTime, int period)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTime, period);
        }

        public TimerPclNet40(TimerPclNet40Callback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTime, period);
        }

        public bool Change(int dueTime, int period)
        {
            return _timer.Change(dueTime, period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return _timer.Change(dueTime, period);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}