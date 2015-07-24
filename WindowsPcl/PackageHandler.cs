using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AdjustSdk.Pcl
{
    public class PackageHandler : IPackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOPackageQueue";
        private const string PackageQueueName = "Package queue";

        private ActionQueue InternalQueue { get; set; }
        private List<ActivityPackage> PackageQueue { get; set; }
        private IRequestHandler RequestHandler { get; set; }
        private IActivityHandler ActivityHandler { get; set; }
        private ILogger Logger { get; set; }

        private ManualResetEvent InternalWaitHandle;

        private bool IsPaused;

        public PackageHandler(IActivityHandler activityHandler, bool startPaused)
        {
            Logger = AdjustFactory.Logger;

            InternalQueue = new ActionQueue("adjust.PackageQueue");

            InternalQueue.Enqueue(() => InitInternal(activityHandler, startPaused));
        }

        public void Init(IActivityHandler activityHandler, bool startPaused)
        {
            ActivityHandler = activityHandler;
            IsPaused = startPaused;
        }

        public void AddPackage(ActivityPackage activityPackage)
        {
            InternalQueue.Enqueue(() => AddInternal(activityPackage));
        }

        public void SendFirstPackage()
        {
            InternalQueue.Enqueue(SendFirstInternal);
        }

        public void SendNextPackage()
        {
            InternalQueue.Enqueue(SendNextInternal);
        }

        public void CloseFirstPackage()
        {
            InternalWaitHandle.Set(); // open the door (signals the wait handle)
        }

        public void PauseSending()
        {
            IsPaused = true;
        }

        public void ResumeSending()
        {
            IsPaused = false;
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            ActivityHandler.FinishedTrackingActivity(jsonDict);
        }

        private void InitInternal(IActivityHandler activityHandler, bool startPaused)
        {
            Init(activityHandler, startPaused);

            ReadPackageQueue();

            InternalWaitHandle = new ManualResetEvent(true); // door starts open (signaled)

            RequestHandler = AdjustFactory.GetRequestHandler(this);
        }

        private void AddInternal(ActivityPackage activityPackage)
        {
            if (activityPackage.ActivityKind.Equals(ActivityKind.Click) && PackageQueue.Count > 0)
            {
                PackageQueue.Insert(1, activityPackage);
            }
            else
            {
                PackageQueue.Add(activityPackage);
            }

            Logger.Debug("Added package {0} ({1})", PackageQueue.Count, activityPackage);
            Logger.Verbose("{0}", activityPackage.GetExtendedString());

            WritePackageQueue();
        }

        private void SendFirstInternal()
        {
            if (PackageQueue.Count == 0) return;

            if (IsPaused)
            {
                Logger.Debug("Package handler is paused");
                return;
            }

            // no need to lock InternalWaitHandle between WaitOne(0) call and Reset()
            // because all Internal methods of PackageHandler can be only executed by 1 thread at a time

            if (InternalWaitHandle.WaitOne(0)) // check if the door is open without waiting (waiting 0 seconds)
            {
                InternalWaitHandle.Reset(); // close the door (non-signals the wait handle)
                RequestHandler.SendPackage(PackageQueue.First());
            }
            else
            {
                Logger.Verbose("Package handler is already sending");
            }
        }

        private void SendNextInternal()
        {
            try
            {
                PackageQueue.RemoveAt(0);
                WritePackageQueue();
            }
            finally
            // preventing an exception not signaling the WaitHandle
            {
                InternalWaitHandle.Set(); // open the door (signals the wait handle)
            }
            SendFirstInternal();
        }

        private void WritePackageQueue()
        {
            Func<string> sucessMessage = () => Util.f("Package handler wrote {0} packages", PackageQueue.Count);
            Util.SerializeToFileAsync(
                fileName: PackageQueueFilename,
                objectWriter: ActivityPackage.SerializeListToStream,
                input: PackageQueue,
                sucessMessage: sucessMessage)
                .Wait();
        }

        private void ReadPackageQueue()
        {
            PackageQueue = Util.DeserializeFromFileAsync(PackageQueueFilename,
                ActivityPackage.DeserializeListFromStream, // deserialize function from Stream to List of ActivityPackage
                () => null, // default value in case of error
                PackageQueueName) // package queue name
                .Result; // wait to finish

            if (PackageQueue != null)
            {
                Logger.Debug("Package handler read {0} packages", PackageQueue.Count);
            } 
            else
            {
                PackageQueue = new List<ActivityPackage>();
            }
        }
    }
}