using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class ActionQueue
    {
        private ILogger _Logger = AdjustFactory.Logger;
        private Queue<Action> _ActionQueue = new Queue<Action>();

        private bool _IsTaskWorkerProcessing = false; // protected by lock(InternalQueue)

        internal string Name { get; private set; }

        internal ActionQueue(string name)
        {
            Name = name;
        }

        internal void Delay(TimeSpan timeSpan, Action action)
        {
            Task.Delay(timeSpan).ContinueWith(_ => Enqueue(action));
        }

        internal void Enqueue(Action action)
        {
            lock (_ActionQueue)
            {
                if (!_IsTaskWorkerProcessing)
                {
                    //_Logger.Verbose("TaskScheduler {0} start thread", Name);
                    _IsTaskWorkerProcessing = true;
                    ProcessActionQueue(action);
                }
                else
                {
                    //_Logger.Verbose("TaskScheduler {0} enqued", Name);
                    _ActionQueue.Enqueue(action);
                }
            }
        }

        private void ProcessActionQueue(Action firstAction)
        {
            Task.Run(() =>
            {
                //_Logger.Verbose("ActionQueue {0} run first action", Name);

                // execute the first task
                TryExecuteAction(firstAction);

                // Process all available items in the queue.
                while (true)
                {
                    Action action;
                    lock (_ActionQueue)
                    {
                        //_Logger.Verbose("ActionQueue {0} got {1} action to process", Name, _ActionQueue.Count);
                        if (_ActionQueue.Count == 0)
                        {
                            _IsTaskWorkerProcessing = false;
                            break;
                        }
                        action = _ActionQueue.Dequeue();
                        //_Logger.Verbose("ActionQueue {0} dequeued", Name);
                    }
                    TryExecuteAction(action);
                }
            });
        }

        private void TryExecuteAction(Action action)
        {
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