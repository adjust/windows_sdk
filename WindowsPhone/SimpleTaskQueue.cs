using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    //from http://stackoverflow.com/questions/16645171/task-queue-for-wp8
    class SimpleTaskQueue
    {
        private readonly Queue<Func<Task>> queue = new Queue<Func<Task>>();
        //private Mutex Mutex;
        //private SemaphoreSlim semaphoreSlim;
        //private ManualResetEventSlim;
        private Semaphore semaphore;
        internal string Name { get; private set; }

        internal SimpleTaskQueue(string name)
        {
            Name = name;
            //Mutex = new Mutex(false, Name);
            //semaphoreSlim = new SemaphoreSlim(0, 1);
            semaphore = new Semaphore(0, 1);
            Task.Factory.StartNew(() => ProcessQueue(), TaskCreationOptions.LongRunning);
        }

        private void ProcessQueue()
        {
            while (true)
            {
                AILogger.Debug("Waiting on TaskQueue {0} with {1} tasks", Name, queue.Count);
                semaphore.WaitOne();
                AILogger.Debug("Free at TaskQueue {0} with {1} tasks", Name, queue.Count);
                while(queue.Count != 0)
                {
                    AILogger.Debug("Dequeuing at TaskQueue {0} with {1} tasks", Name, queue.Count);
                    Func<Task> command = queue.Dequeue();
                    try
                    {
                        command().Wait();
                    }
                    catch (Exception ex)
                    {
                        // Exceptions from your queued tasks will end up here.
                        //throw;
                    }
                }
            }
        }


        internal void Enqueue(Func<Task> command)
        {
            AILogger.Debug("Enqueuing  at TaskQueue {0} with {1} tasks", Name, queue.Count);
            queue.Enqueue(command);
            //Mutex.ReleaseMutex();
            //semaphoreSlim.Release()
            semaphore.Release();
        }

    }
}
