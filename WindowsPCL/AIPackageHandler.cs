using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    internal class AIPackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOActivityState";

        private AIActionQueue InternalQueue;
        private List<AIActivityPackage> PackageQueue;
        private AIRequestHandler RequestHandler;

        internal bool IsPaused;

        internal AIPackageHandler()
        {
            InternalQueue = new AIActionQueue("io.adjust.PackageQueue");
            PackageQueue = new List<AIActivityPackage>();
            IsPaused = true;

            InternalQueue.Enqueue(InitInternal);
        }

        internal void ResumeSending()
        {
            IsPaused = false;
        }

        internal void PauseSending()
        {
            IsPaused = true;
        }

        internal void AddPackage(AIActivityPackage activityPackage)
        {
            InternalQueue.Enqueue(() => AddInternal(activityPackage));
        }

        internal void SendFirstPackage()
        {
            InternalQueue.Enqueue(SendFirstInternal);
        }

        internal void SendNextPackage()
        {
            InternalQueue.Enqueue(SendNextInternal);
        }

        internal void CloseFirstPackage()
        {
            //Necessary in iOS because there is a semaphore controlling the Request Handler
            //  but not here because the Background Worker only accepts one task at a time
            AILogger.Debug("Close Package");
        }

        internal AIActivityPackage FirstPackage()
        {
            return PackageQueue.First();
        }

        private void InitInternal()
        {
            RequestHandler = new AIRequestHandler(this);

            //todo test file not exists
            Util.DeleteFile(PackageQueueFilename);

            ReadPackageQueue();
        }

        private void AddInternal(AIActivityPackage activityPackage)
        {
            PackageQueue.Add(activityPackage);
            AILogger.Debug("Added package {0} ({1})", PackageQueue.Count, activityPackage);
            AILogger.Verbose("{0}", activityPackage.ExtendedString());

            WritePacakgeQueue();
        }

        private void SendFirstInternal()
        {
            if (PackageQueue.Count == 0) return;

            if (IsPaused)
            {
                AILogger.Debug("Package handler is paused");
                return;
            }

            if (!RequestHandler.TrySendFirstPackage())
            {
                AILogger.Verbose("Package handler is already sending");
            }
        }

        private void SendNextInternal()
        {
            PackageQueue.RemoveAt(0);
            WritePacakgeQueue();

            SendFirstInternal();
        }

        private void WritePacakgeQueue()
        {
            Util.SerializeToFile(PackageQueueFilename, AIActivityPackage.SerializeListToStream, PackageQueue);
        }

        private void ReadPackageQueue()
        {
            if (!Util.TryDeserializeFromFile(PackageQueueFilename, 
                AIActivityPackage.DeserializeListFromStream
                , out PackageQueue))
            {
                //error read, start with fresh
                PackageQueue = new List<AIActivityPackage>(); 
            }
        }
    }
}
