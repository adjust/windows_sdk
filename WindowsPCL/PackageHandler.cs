using AdjustSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AdjustSdk.Pcl
{
    internal class PackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOPackageQueue";

        private ActionQueue InternalQueue;
        private List<ActivityPackage> PackageQueue;
        private RequestHandler RequestHandler;
        private ActivityHandler ActivityHandler;

        private ManualResetEvent InternalWaitHandle;
        private DeviceUtil DeviceSpecific;

        internal bool IsPaused;

        internal PackageHandler(DeviceUtil deviceUtil)
        {
            InternalQueue = new ActionQueue("adjust.PackageQueue");
            PackageQueue = new List<ActivityPackage>();
            IsPaused = true;
            DeviceSpecific = deviceUtil;

            InternalWaitHandle = new ManualResetEvent(true); // door starts open (signaled)

            InternalQueue.Enqueue(InitInternal);
        }

        internal void AddPackage(ActivityPackage activityPackage)
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
            InternalWaitHandle.Set(); // open the door (signals the wait handle)
        }

        internal void PauseSending()
        {
            IsPaused = true;
        }

        internal void ResumeSending()
        {
            IsPaused = false;
        }

        internal void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            InternalRequestHandler.SetResponseDelegate(responseDelegate);
        }

        private void InitInternal()
        {
            InternalRequestHandler = new RequestHandler(this);

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
                InternalRequestHandler.SendPackage(PackageQueue.First());
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