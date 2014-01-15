using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace adeven.AdjustIo.PCL
{
    internal class PackageHandler
    {
        private const string PackageQueueFilename = "AdjustIOActivityState";

        private ActionQueue InternalQueue;
        private List<ActivityPackage> PackageQueue;
        private RequestHandler InternalRequestHandler;

        private ManualResetEvent InternalWaitHandle;
        private DeviceUtil DeviceSpecific;

        internal bool IsPaused;

        internal PackageHandler(DeviceUtil deviceUtil)
        {
            InternalQueue = new ActionQueue("io.adjust.PackageQueue");
            PackageQueue = new List<ActivityPackage>();
            IsPaused = true;
            DeviceSpecific = deviceUtil;

            InternalWaitHandle = new ManualResetEvent(true);

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
            InternalWaitHandle.Set();
        }

        internal void PauseSending()
        {
            IsPaused = true;
        }

        internal void ResumeSending()
        {
            IsPaused = false;
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
            if (InternalWaitHandle.WaitOne(0))
            {
                InternalWaitHandle.Reset();
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
            {
                // preventing an exception not releasing the WaitHandle
                InternalWaitHandle.Set();
            }
            SendFirstInternal();
        }

        private void WritePackageQueue()
        {
            //Util.SerializeToFile(PackageQueueFilename, ActivityPackage.SerializeListToStream, PackageQueue);
            DeviceSpecific.SerializeToFile(PackageQueueFilename, ActivityPackage.SerializeListToStream, PackageQueue);
        }

        private void ReadPackageQueue()
        {
            //PackageQueue = Util.DeserializeFromFile(PackageQueueFilename,
            //    ActivityPackage.DeserializeListFromStream, //deserialize function from Stream to List of ActivityPackage
            //    () => new List<ActivityPackage>()); //default value in case of error
            PackageQueue = DeviceSpecific.DeserializeFromFile(PackageQueueFilename,
                ActivityPackage.DeserializeListFromStream, //deserialize function from Stream to List of ActivityPackage
                () => new List<ActivityPackage>()); //default value in case of error
        }
    }
}