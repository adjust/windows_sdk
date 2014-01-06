using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class AIPackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOActivityState";

        private static AITaskQueue InternalQueue;
        private static List<AIActivityPackage> PackageQueue;
        private static AIRequestHandler RequestHandler;

        internal static bool IsPaused;

        internal AIPackageHandler()
        {
            InternalQueue = new AITaskQueue("io.adjust.PackageQueue");
            PackageQueue = new List<AIActivityPackage>();
            IsPaused = true;

            InternalQueue.Enqueue(() => InitInternalAsync());
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
            InternalQueue.Enqueue(() => AddInternalAsync(activityPackage));
        }

        internal void SendFirstPackage()
        {
            InternalQueue.Enqueue(() => SendFirstInternalAsync());
        }

        internal void SendNextPackage()
        {
            InternalQueue.Enqueue(() => SendNextInternalAsync());
        }

        internal void CloseFirstPackage()
        {
            //Necessary in iOS because there is a semaphore controlling the Request Handler
            //  but not here because the Background Worker only accepts one task at a time
            AILogger.Debug("Close Package");
        }

        private async Task InitInternalAsync()
        {
            RequestHandler = new AIRequestHandler(this);

            //test file not exists
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (storage.FileExists(PackageQueueFilename))
                storage.DeleteFile(PackageQueueFilename);

            ReadPackageQueue();
        }

        private async Task AddInternalAsync(AIActivityPackage activityPackage)
        {
            PackageQueue.Add(activityPackage);
            AILogger.Debug("Added package {0} ({1})", PackageQueue.Count, activityPackage);
            AILogger.Verbose("{0}", activityPackage.ExtendedString());

            WritePacakgeQueue();
        }

        private async Task SendFirstInternalAsync()
        {
            if (PackageQueue.Count == 0) return;

            if (IsPaused)
            {
                AILogger.Debug("Package handler is paused");
                return;
            }

            if (RequestHandler.IsBusy)
            {
                AILogger.Verbose("Package handler is already sending");
                return;
            }

            var activityPackage = PackageQueue.First();

            RequestHandler.SendPackage(
                activityPackage
            );
        }

        private async Task SendNextInternalAsync()
        {
            PackageQueue.RemoveAt(0);
            WritePacakgeQueue();
            
            await SendFirstInternalAsync();
        }

        private void WritePacakgeQueue()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                using (var stream = storage.OpenFile(PackageQueueFilename, FileMode.OpenOrCreate))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    AIActivityPackage.SerializeListToStream(stream, PackageQueue);
                }
                AILogger.Debug("Package handler wrote {0} packages", PackageQueue.Count);
            }
            catch (Exception ex)
            {
                AILogger.Error("Failed to write package queue");
            }
        }

        private void ReadPackageQueue()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                using (var stream = storage.OpenFile(PackageQueueFilename, FileMode.Open))
                {
                    PackageQueue = AIActivityPackage.DeserializeListFromStream(stream);
                }
                AILogger.Verbose("Package Handler read {0} packages", PackageQueue.Count);
                return;
            }
            catch (IsolatedStorageException ise)
            {
                AILogger.Verbose("Package queue file not found");
            }
            catch (FileNotFoundException fnfe)
            {
                AILogger.Verbose("Package queue file not found");
            }
            catch (Exception e)
            {
                AILogger.Error("Failed to read package queue ({0})", e);
            }

            //start with a fresh package queue in case of any exception
            PackageQueue = new List<AIActivityPackage>();
        }
    }
}
