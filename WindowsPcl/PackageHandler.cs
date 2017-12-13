using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace AdjustSdk.Pcl
{
    public class PackageHandler : IPackageHandler
    {
        private const string PackageQueueLegacyFilename = "AdjustIOPackageQueue";
        private const string PackageQueueLegacyName = "Package queue";
        private const string PackageQueueStorageName = "adjust_package_queue";

        private readonly ILogger _logger = AdjustFactory.Logger;
        private readonly ActionQueue _actionQueue = new ActionQueue("adjust.PackageHandler");
        private readonly BackoffStrategy _backoffStrategy = AdjustFactory.GetPackageHandlerBackoffStrategy();

        private List<ActivityPackage> _packageQueue;
        private IRequestHandler _requestHandler;
        private IActivityHandler _activityHandler;

        private ManualResetEvent _internalWaitHandle;

        private IDeviceUtil _deviceUtil;

        private bool _isPaused;

        public PackageHandler(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused)
        {
            Init(activityHandler, deviceUtil, startPaused);

            _actionQueue.Enqueue(() => InitI(activityHandler, deviceUtil, startPaused));
        }

        public void Init(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused)
        {
            _activityHandler = activityHandler;
            _deviceUtil = deviceUtil;
            _isPaused = startPaused;
        }

        public void AddPackage(ActivityPackage activityPackage)
        {
            _actionQueue.Enqueue(() => AddI(activityPackage));
        }

        public void SendFirstPackage()
        {
            _actionQueue.Enqueue(SendFirstI);
        }

        public void SendNextPackage(ResponseData responseData)
        {
            _actionQueue.Enqueue(SendNextI);

            _activityHandler.FinishedTrackingActivity(responseData);
        }

        public void CloseFirstPackage(ResponseData responseData, ActivityPackage activityPackage)
        {
            _activityHandler.FinishedTrackingActivity(responseData);

            Action action = () =>
            {
                _logger.Verbose("Package handler can send");
                _internalWaitHandle.Set(); // open the door (signals the wait handle)

                SendFirstPackage();
            };

            int retries = activityPackage.IncreaseRetries();

            var waitTime = Util.WaitingTime(retries, _backoffStrategy);

            _logger.Verbose("Waiting for {0} seconds before retrying for the {1} time", Util.SecondDisplayFormat(waitTime), retries);

            _actionQueue.Delay(waitTime, action);
        }

        public void PauseSending()
        {
            _isPaused = true;
        }

        public void ResumeSending()
        {
            _isPaused = false;
        }

        public void UpdatePackages(SessionParameters sessionParameters)
        {
            var sessionParametersCopy = sessionParameters.Clone();

            _actionQueue.Enqueue(() => UpdatePackagesI(sessionParametersCopy));
        }

        private void InitI(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused)
        {
            ReadPackageQueueI();

            _internalWaitHandle = new ManualResetEvent(true); // door starts open (signaled)

            _requestHandler = AdjustFactory.GetRequestHandler(SendNextPackage, CloseFirstPackage);
        }

        private void AddI(ActivityPackage activityPackage)
        {
            _packageQueue.Add(activityPackage);

            _logger.Debug("Added package {0} ({1})", _packageQueue.Count, activityPackage);
            _logger.Verbose("{0}", activityPackage.GetExtendedString());

            WritePackageQueueI();
        }

        private void SendFirstI()
        {
            if (_packageQueue.Count == 0) {  return; }

            if (_isPaused)
            {
                _logger.Debug("Package handler is paused");
                return;
            }

            // no need to lock InternalWaitHandle between WaitOne(0) call and Reset()
            // because all Internal methods of PackageHandler can be only executed by 1 thread at a time

            if (_internalWaitHandle.WaitOne(0)) // check if the door is open without waiting (waiting 0 seconds)
            {
                _internalWaitHandle.Reset(); // close the door (non-signals the wait handle)
                _requestHandler.SendPackage(_packageQueue.First(), _packageQueue.Count - 1);
            }
            else
            {
                _logger.Verbose("Package handler is already sending");
            }
        }

        private void SendNextI()
        {
            try
            {
                _packageQueue.RemoveAt(0);
                WritePackageQueueI();
                _logger.Verbose("Package handler can send");
            }
            finally
            // preventing an exception not signaling the WaitHandle
            {
                _internalWaitHandle.Set(); // open the door (signals the wait handle)
            }
            SendFirstI();
        }

        private void UpdatePackagesI(SessionParameters sessionParameters)
        {
            _logger.Debug("Updating package handler queue");
            _logger.Verbose("Session Callback parameters: {0}", sessionParameters.CallbackParameters);
            _logger.Verbose("Session Partner parameters: {0}", sessionParameters.PartnerParameters);

            foreach (var activityPackage in _packageQueue)
            {
                var parameters = activityPackage.Parameters;

                // callback parameters
                var mergedCallbackParameters = Util.MergeParameters(
                    target: sessionParameters.CallbackParameters,
                    source: activityPackage.CallbackParameters,
                    parametersName: "Callback");
                PackageBuilder.AddDictionaryJson(parameters, "callback_params", mergedCallbackParameters);

                // partner parameters
                var mergedPartnerParameters = Util.MergeParameters(
                    target: sessionParameters.PartnerParameters,
                    source: activityPackage.PartnerParameters,
                    parametersName: "Partner");
                PackageBuilder.AddDictionaryJson(parameters, "partner_params", mergedPartnerParameters);
            }

            WritePackageQueueI();
        }

        private void WritePackageQueueI()
        {
            List<string> packageQueueStringList = new List<string>(_packageQueue.Count);
            foreach (var activityPackage in _packageQueue)
            {
                var activityPackageMap = ActivityPackage.ToDictionary(activityPackage);
                packageQueueStringList.Add(JsonConvert.SerializeObject(activityPackageMap));
            }

            string packageQueueString = JsonConvert.SerializeObject(packageQueueStringList);

            bool packageQueuePersisted = _deviceUtil.PersistValue(PackageQueueStorageName, packageQueueString);
            if (!packageQueuePersisted)
                _logger.Verbose("Error. Package queue not persisted on device within specific time frame (60 seconds default).");
        }

        private void ReadPackageQueueI()
        {
            string packageQueueString;
            if (_deviceUtil.TryTakeValue(PackageQueueStorageName, out packageQueueString))
            {
                _packageQueue = new List<ActivityPackage>();

                List<string> packageQueueStringList =
                    JsonConvert.DeserializeObject<List<string>>(packageQueueString);
                foreach (var activityPackageMapString in packageQueueStringList)
                {
                    var activityPackageMap =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(activityPackageMapString);
                    var activityPackage = ActivityPackage.FromDictionary(activityPackageMap);
                    _packageQueue.Add(activityPackage);
                }
            }
            else
            {
                var packageQueueLegacyFile = _deviceUtil.GetLegacyStorageFile(PackageQueueLegacyFilename).Result;

                // if package queue is not found, try to read it from the legacy file
                _packageQueue = Util.DeserializeFromFileAsync(
                        file: packageQueueLegacyFile,
                        objectReader: ActivityPackage.DeserializeListFromStreamLegacy, // deserialize function from Stream to List of ActivityPackage
                        defaultReturn: () => null, // default value in case of error
                        objectName: PackageQueueLegacyName) // package queue name
                    .Result;

                // if it's successfully read from legacy source, store it using new persistance
                // and then delete the old file
                if (_packageQueue != null)
                {
                    _logger.Info("Legacy PackageQueue File found and successfully read.");

                    WritePackageQueueI();

                    if (_deviceUtil.TryTakeValue(PackageQueueStorageName, out packageQueueString))
                    {
                        packageQueueLegacyFile.DeleteAsync();
                        _logger.Info("Legacy PackageQueue File deleted.");
                    }
                }
            }
            
            if (_packageQueue != null)
            {
                _logger.Debug("Package handler read {0} packages", _packageQueue.Count);
            } 
            else
            {
                _packageQueue = new List<ActivityPackage>();
            }
        }
    }
}