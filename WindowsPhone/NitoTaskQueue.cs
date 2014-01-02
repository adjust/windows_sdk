using Microsoft.Phone.Reactive;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class NitoTaskQueue
    {
        //private readonly Queue<Func<Task>> queue = new Queue<Func<Task>>();
        public string Name { get; set; }
        private AsyncProducerConsumerQueue<Func<Task>> queue = new AsyncProducerConsumerQueue<Func<Task>>();

        internal NitoTaskQueue()
        {
            Task.Factory.StartNew(() => ProcessQueue(), TaskCreationOptions.LongRunning);
        }

        private async Task ProcessQueue()
        {
            while (await queue.OutputAvailableAsync())
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
            return;
        }

        internal void Enqueue(Func<Task> command)
        {
            queue.Enqueue(command);
        }

    }
}
