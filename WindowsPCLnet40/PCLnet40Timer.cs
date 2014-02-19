using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.PCLnet40
{
    public delegate void PCLnet40TimerCallback(object state);

    //wrapper for System.Threading.Timer Class
    public class PCLnet40Timer : IDisposable
    {
        private Timer _timer;

        public PCLnet40Timer(PCLnet40TimerCallback callback, object state, int dueTime, int period)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTime, period);
        }

        public PCLnet40Timer(PCLnet40TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
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