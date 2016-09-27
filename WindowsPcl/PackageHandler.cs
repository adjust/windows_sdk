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

        private ILogger _Logger = AdjustFactory.Logger;
        private ActionQueue _ActionQueue = new ActionQueue("adjust.PackageHandler");
        private BackoffStrategy _backoffStrategy = AdjustFactory.GetPackageHandlerBackoffStrategy();

        private List<ActivityPackage> _PackageQueue;
        private IRequestHandler _RequestHandler;
        private IActivityHandler _ActivityHandler;

        private ManualResetEvent InternalWaitHandle;

        private bool IsPaused;

        public PackageHandler(IActivityHandler activityHandler, bool startPaused)
        {
            _ActionQueue.Enqueue(() => InitI(activityHandler, startPaused));
        }

        public void Init(IActivityHandler activityHandler, bool startPaused)
        {
            _ActivityHandler = activityHandler;
            IsPaused = startPaused;
        }

        public void AddPackage(ActivityPackage activityPackage)
        {
            _ActionQueue.Enqueue(() => AddI(activityPackage));
        }

        public void SendFirstPackage()
        {
            _ActionQueue.Enqueue(SendFirstI);
        }

        public void SendNextPackage(ResponseData responseData)
        {
            _ActionQueue.Enqueue(SendNextI);

            _ActivityHandler.FinishedTrackingActivity(responseData.JsonResponse);
        }

        public void CloseFirstPackage(ResponseData responseData, ActivityPackage activityPackage)
        {
            _ActivityHandler.FinishedTrackingActivity(responseData.JsonResponse);

            Action action = () =>
            {
                _Logger.Verbose("Package handler can send");
                InternalWaitHandle.Set(); // open the door (signals the wait handle)

                SendFirstPackage();
            };

            int retries = activityPackage.IncreaseRetries();

            var waitTime = Util.WaitingTime(retries, _backoffStrategy);

            _Logger.Verbose("Waiting for {0} seconds before retrying for the {1} time", Util.SecondDisplayFormat(waitTime), retries);

            _ActionQueue.Delay(waitTime, action);
        }

        public void PauseSending()
        {
            IsPaused = true;
        }

        public void ResumeSending()
        {
            IsPaused = false;
        }

        private void InitI(IActivityHandler activityHandler, bool startPaused)
        {
            Init(activityHandler, startPaused);

            ReadPackageQueue();

            InternalWaitHandle = new ManualResetEvent(true); // door starts open (signaled)

            _RequestHandler = AdjustFactory.GetRequestHandler(SendNextPackage, CloseFirstPackage);
        }

        private void AddI(ActivityPackage activityPackage)
        {
            _PackageQueue.Add(activityPackage);

            _Logger.Debug("Added package {0} ({1})", _PackageQueue.Count, activityPackage);
            _Logger.Verbose("{0}", activityPackage.GetExtendedString());

            WritePackageQueue();
        }

        private void SendFirstI()
        {
            if (_PackageQueue.Count == 0) return;

            if (IsPaused)
            {
                _Logger.Debug("Package handler is paused");
                return;
            }

            // no need to lock InternalWaitHandle between WaitOne(0) call and Reset()
            // because all Internal methods of PackageHandler can be only executed by 1 thread at a time

            if (InternalWaitHandle.WaitOne(0)) // check if the door is open without waiting (waiting 0 seconds)
            {
                InternalWaitHandle.Reset(); // close the door (non-signals the wait handle)
                _RequestHandler.SendPackage(_PackageQueue.First());
            }
            else
            {
                _Logger.Verbose("Package handler is already sending");
            }
        }

        private void SendNextI()
        {
            try
            {
                _PackageQueue.RemoveAt(0);
                WritePackageQueue();
            }
            finally
            // preventing an exception not signaling the WaitHandle
            {
                InternalWaitHandle.Set(); // open the door (signals the wait handle)
            }
            SendFirstI();
        }

        private void WritePackageQueue()
        {
            Func<string> sucessMessage = () => Util.f("Package handler wrote {0} packages", _PackageQueue.Count);
            Util.SerializeToFileAsync(
                fileName: PackageQueueFilename,
                objectWriter: ActivityPackage.SerializeListToStream,
                input: _PackageQueue,
                sucessMessage: sucessMessage)
                .Wait();
        }

        private void ReadPackageQueue()
        {
            _PackageQueue = Util.DeserializeFromFileAsync(PackageQueueFilename,
                ActivityPackage.DeserializeListFromStream, // deserialize function from Stream to List of ActivityPackage
                () => null, // default value in case of error
                PackageQueueName) // package queue name
                .Result; // wait to finish

            if (_PackageQueue != null)
            {
                _Logger.Debug("Package handler read {0} packages", _PackageQueue.Count);
            } 
            else
            {
                _PackageQueue = new List<ActivityPackage>();
            }
        }
    }
}