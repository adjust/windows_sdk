using AdjustSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AdjustSdk.Pcl
{
    public class PackageHandler : IPackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOPackageQueue";

        private ActionQueue InternalQueue;
        private List<ActivityPackage> PackageQueue;
        private IRequestHandler RequestHandler;
        private IActivityHandler ActivityHandler;
        private ILogger Logger;

        private ManualResetEvent InternalWaitHandle;

        private bool IsPaused;

        public PackageHandler(IActivityHandler activityHandler)
        {
            InternalQueue = new ActionQueue("adjust.PackageQueue");
            PackageQueue = new List<ActivityPackage>();
            IsPaused = true;
            Logger = AdjustFactory.Logger;

            InternalWaitHandle = new ManualResetEvent(true); // door starts open (signaled)

            InternalQueue.Enqueue(() => InitInternal(activityHandler));
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

        public void FinishedTrackingActivity(ActivityPackage activityPackage, ResponseData responseData)
        {
            responseData.ActivityKind = activityPackage.ActivityKind;
            ActivityHandler.FinishTrackingWithResponse(responseData);
        }

        private void InitInternal(IActivityHandler activityHandler)
        {
            ActivityHandler = activityHandler;
            RequestHandler = AdjustFactory.GetRequestHandler(this);

            ReadPackageQueue();
        }

        private void AddInternal(ActivityPackage activityPackage)
        {
            PackageQueue.Add(activityPackage);
            Logger.Debug("Added package {0} ({1})", PackageQueue.Count, activityPackage);
            Logger.Verbose("{0}", activityPackage.ExtendedString());

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
            var sucessMessage = String.Format("Package handler wrote {0} packages", PackageQueue.Count);
            Util.SerializeToFileAsync(PackageQueueFilename, ActivityPackage.SerializeListToStream, PackageQueue, sucessMessage).Wait();
        }

        private void ReadPackageQueue()
        {
            PackageQueue = Util.DeserializeFromFileAsync(PackageQueueFilename,
                ActivityPackage.DeserializeListFromStream, //deserialize function from Stream to List of ActivityPackage
                () => new List<ActivityPackage>()) //default value in case of error
                .Result;
        }
    }
}