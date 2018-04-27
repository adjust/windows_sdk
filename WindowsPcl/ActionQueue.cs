using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    internal class ActionQueue
    {
        private ILogger _logger = AdjustFactory.Logger;
        private Queue<Action> _actionQueue = new Queue<Action>();
        private CancellationTokenSource _processActionQueueCancelToken;

        private bool _isTaskWorkerProcessing = false; // protected by lock(InternalQueue)

        internal string Name { get; }

        internal ActionQueue(string name)
        {
            _processActionQueueCancelToken = new CancellationTokenSource();
            Name = name;
        }

        internal void Delay(TimeSpan timeSpan, Action action)
        {
            Task.Delay(timeSpan).ContinueWith(_ => Enqueue(action));
        }

        internal void Enqueue(Action action)
        {
            lock (_actionQueue)
            {
                if (!_isTaskWorkerProcessing)
                {
                    //_Logger.Verbose("TaskScheduler {0} start thread", Name);
                    _isTaskWorkerProcessing = true;
                    ProcessActionQueue(action);
                }
                else
                {
                    //_Logger.Verbose("TaskScheduler {0} enqued", Name);
                    _actionQueue.Enqueue(action);
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
                    // possible teardown happened meanwhile
                    if (IsTeardownInitiated())
                        return;

                    Action action;
                    lock (_actionQueue)
                    {
                        //_Logger.Verbose("ActionQueue {0} got {1} action to process", Name, _ActionQueue.Count);
                        if (_actionQueue.Count == 0)
                        {
                            _isTaskWorkerProcessing = false;
                            break;
                        }
                        action = _actionQueue.Dequeue();
                        //_Logger.Verbose("ActionQueue {0} dequeued", Name);
                    }
                    TryExecuteAction(action);
                }
            }, _processActionQueueCancelToken.Token);
        }

        private void TryExecuteAction(Action action)
        {
            try
            {
                if (IsTeardownInitiated())
                    return;

                action();
            }
            catch (Exception ex)
            {
                _logger.Error("ActionQueue {0} with exception ({1})", Name, ex);
            }
        }

        private bool IsTeardownInitiated()
        {
            if (_processActionQueueCancelToken == null || _processActionQueueCancelToken.Token.IsCancellationRequested)
            {
                _processActionQueueCancelToken?.Dispose();
                _processActionQueueCancelToken = null;
                return true;
            }

            return false;
        }

        public void Teardown()
        {
            _processActionQueueCancelToken?.Cancel();
            _actionQueue?.Clear();
            _actionQueue = null;
        }
    }
}