using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    //from http://stackoverflow.com/questions/16645171/task-queue-for-wp8
    class SimpleTaskQueue
    {
        private readonly Queue<Func<Task>> queue = new Queue<Func<Task>>();
        public string Name { get; set; }
        //private Task queueProcessor;

        internal SimpleTaskQueue()
        {
            Task.Factory.StartNew(() => ProcessQueue(), TaskCreationOptions.LongRunning);
        }

        private async Task ProcessQueue()
        {
            //try
            //{
                while (true)
                {
                    if (queue.Count != 0)
                    {
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
            //}
            //finally
            //{
            //    queueProcessor = null;
            //}
        }


        internal void Enqueue(Func<Task> command)
        {
            queue.Enqueue(command);
            //if (queueProcessor == null)
            //    queueProcessor = ProcessQueue();
        }

    }
}
