using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class AITaskQueue
    {
        private Queue<Func<Task>> TaskQueue;
        private ManualResetEvent WorkEvent;

        internal string Name { get; private set; }

        public AITaskQueue(string name)
        {
            Name = name;
            TaskQueue = new Queue<Func<Task>>();
            WorkEvent = new ManualResetEvent(false);
            Task.Factory.StartNew(() => ProcessTaskQueue(), TaskCreationOptions.LongRunning);
        }

        internal void Enqueue(Func<Task> task)
        {
            lock (TaskQueue)
            {
                if (TaskQueue.Count == 0)
                    WorkEvent.Set();
                TaskQueue.Enqueue(task);
                AILogger.Debug("TaskQueue {0} enqueued {1} task", Name, task.Method.Name);
            }
        }

        private void ProcessTaskQueue()
        {
            while (true)
            {
                AILogger.Debug("TaskQueue {0} waiting", Name);
                WorkEvent.WaitOne();
                while (true)
                {
                    Func<Task> task;
                    lock (TaskQueue)
                    {
                        AILogger.Debug("TaskQueue {0} got {1} tasks to process", Name, TaskQueue.Count);
                        if (TaskQueue.Count == 0)
                        {
                            WorkEvent.Reset();
                            break;
                        }
                        task = TaskQueue.Dequeue();
                        AILogger.Debug("TaskQueue {0} dequeued {1} task", Name, task.Method.Name);
                    }
                    try
                    {
                        task().Wait();
                    }
                    catch (Exception ex)
                    {
                        AILogger.Error("TaskQueue {0} with exception ({1})", Name, ex);
                    }
                }
            }
        }
    }
}
