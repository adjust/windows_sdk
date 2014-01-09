using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsLibrary
{
    public delegate void PCLTimerCallback(object state);

    //wrapper for Timer Class
    public class PCLTimer : IDisposable
    {
        private Timer _timer;
        public PCLTimer(PCLTimerCallback callback, object state, int dueTime, int period)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTime, period);
        }
        public PCLTimer(PCLTimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
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
