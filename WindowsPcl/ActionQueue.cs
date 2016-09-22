using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class ActionQueue
    {
        private ILogger _Logger = AdjustFactory.Logger;
        private Queue<Action> _ActionQueue = new Queue<Action>();

        // more info about Wait handles in http://www.yoda.arachsys.com/csharp/threads/waithandles.shtml
        private ManualResetEvent _ManualHandle = new ManualResetEvent(false); // door starts closed (non-signaled)

        internal string Name { get; private set; }

        internal ActionQueue(string name)
        {
            Name = name;
            Task.Factory.StartNew(() => ProcessTaskQueue(), TaskCreationOptions.LongRunning);
        }

        internal void Enqueue(Action task)
        {
            lock (_ActionQueue)
            {
                if (_ActionQueue.Count == 0)
                    _ManualHandle.Set(); // open the door (signals the wait handle)
                _ActionQueue.Enqueue(task);
                // Logger.Verbose("ActionQueue {0} enqueued", Name);
            }
        }

        private void ProcessTaskQueue()
        {
            while (true)
            {
                // Logger.Verbose("ActionQueue {0} waiting", Name);
                _ManualHandle.WaitOne(); // waits until the door is open
                while (true)
                {
                    Action action;
                    lock (_ActionQueue)
                    {
                        // Logger.Verbose("ActionQueue {0} got {1} action to process", Name, InternalQueue.Count);
                        if (_ActionQueue.Count == 0)
                        {
                            _ManualHandle.Reset(); // closes the door (non-signals the wait handle)
                            break;
                        }
                        action = _ActionQueue.Dequeue();
                        // Logger.Verbose("ActionQueue  {0} dequeued", Name);
                    }
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error("ActionQueue {0} with exception ({1})", Name, ex);
                    }
                }
            }
        }
    }
}