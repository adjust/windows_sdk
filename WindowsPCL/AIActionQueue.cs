using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    class AIActionQueue
    {
        private Queue<Action> ActionQueue;
        private ManualResetEvent WorkEvent;

        internal string Name { get; private set; }

        internal AIActionQueue(string name)
        {
            Name = name;
            ActionQueue = new Queue<Action>();
            WorkEvent = new ManualResetEvent(false);
            Task.Factory.StartNew(() => ProcessTaskQueue(), TaskCreationOptions.LongRunning);
        }

        internal void Enqueue(Action task)
        {
            lock (ActionQueue)
            {
                if (ActionQueue.Count == 0)
                    WorkEvent.Set();
                ActionQueue.Enqueue(task);
                AILogger.Verbose("ActionQueue {0} enqueued", Name); 
            }
        }

        private void ProcessTaskQueue()
        {
            while (true)
            {
                AILogger.Debug("ActionQueue {0} waiting", Name);
                WorkEvent.WaitOne();
                while (true)
                {
                    Action action;
                    lock (ActionQueue)
                    {
                        AILogger.Debug("ActionQueue {0} got {1} action to process", Name, ActionQueue.Count);
                        if (ActionQueue.Count == 0)
                        {
                            WorkEvent.Reset();
                            break;
                        }
                        action = ActionQueue.Dequeue();
                        AILogger.Debug("ActionQueue  {0} dequeued", Name);
                    }
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        AILogger.Error("ActionQueue {0} with exception ({1})", Name, ex);
                    }
                }
            }
        }
    }
}
