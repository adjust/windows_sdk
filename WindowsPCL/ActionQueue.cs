using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.Adjust.PCL
{
    internal class ActionQueue
    {
        private Queue<Action> InternalQueue;

        // more info about Wait handles in http://www.yoda.arachsys.com/csharp/threads/waithandles.shtml
        private ManualResetEvent ManualHandle;

        internal string Name { get; private set; }

        internal ActionQueue(string name)
        {
            Name = name;
            InternalQueue = new Queue<Action>();
            ManualHandle = new ManualResetEvent(false); // door starts closed (non-signaled)
            Task.Factory.StartNew(() => ProcessTaskQueue(), TaskCreationOptions.LongRunning);
        }

        internal void Enqueue(Action task)
        {
            lock (InternalQueue)
            {
                if (InternalQueue.Count == 0)
                    ManualHandle.Set(); // open the door (signals the wait handle)
                InternalQueue.Enqueue(task);
                // Logger.Verbose("ActionQueue {0} enqueued", Name);
            }
        }

        private void ProcessTaskQueue()
        {
            while (true)
            {
                // Logger.Verbose("ActionQueue {0} waiting", Name);
                ManualHandle.WaitOne(); // waits until the door is open
                while (true)
                {
                    Action action;
                    lock (InternalQueue)
                    {
                        // Logger.Verbose("ActionQueue {0} got {1} action to process", Name, InternalQueue.Count);
                        if (InternalQueue.Count == 0)
                        {
                            ManualHandle.Reset(); // closes the door (non-signals the wait handle)
                            break;
                        }
                        action = InternalQueue.Dequeue();
                        // Logger.Verbose("ActionQueue  {0} dequeued", Name);
                    }
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ActionQueue {0} with exception ({1})", Name, ex);
                    }
                }
            }
        }
    }
}