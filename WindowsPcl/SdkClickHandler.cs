using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class SdkClickHandler : ISdkClickHandler
    {
        private readonly ILogger _logger = AdjustFactory.Logger;
        private readonly ActionQueue _actionQueue = new ActionQueue("adjust.SdkClickHandler");
        private readonly BackoffStrategy _backoffStrategy = AdjustFactory.GetSdkClickHandlerBackoffStrategy();
        private readonly Queue<ActivityPackage> _packageQueue = new Queue<ActivityPackage>();
        private readonly IRequestHandler _requestHandler;
        private WeakReference<IActivityHandler> _activityHandlerWeakReference;
        private readonly string _userAgent;

        private bool _isPaused;

        public SdkClickHandler(IActivityHandler activityHandler, bool startPaused, string userAgent)
        {
            _userAgent = userAgent;

            Init(activityHandler, startPaused);
            _requestHandler = new RequestHandler(
                successCallbac: (responseData) => ProcessSdkClickResponseData(responseData),
                failureCallback: (_, sdkClickPackage) => RetrySendingI(sdkClickPackage));
        }

        public void Init(IActivityHandler activityHandler, bool startPaused)
        {
            _isPaused = startPaused;
            _activityHandlerWeakReference = new WeakReference<IActivityHandler>(activityHandler);
        }

        public void Teardown()
        {
            _actionQueue?.Teardown();
            _packageQueue?.Clear();
            _activityHandlerWeakReference.SetTarget(null);
            _requestHandler.Teardown();

            _actionQueue = null;
            _logger = null;
            _packageQueue = null;
            _backoffStrategy = null;
            _requestHandler = null;
            _activityHandlerWeakReference = null;
        }

        public void PauseSending()
        {
            _isPaused = true;
        }

        public void ResumeSending()
        {
            _isPaused = false;

            SendNextSdkClick();
        }

        public void SendSdkClick(ActivityPackage sdkClickPackage)
        {
            _actionQueue.Enqueue(() =>
            {
                _packageQueue.Enqueue(sdkClickPackage);

                _logger.Debug("Added sdk_click {0}", _packageQueue.Count);
                _logger.Verbose("{0}", sdkClickPackage.GetExtendedString());

                SendNextSdkClick();
            });
        }

        private void SendNextSdkClick()
        {
            _actionQueue.Enqueue(SendNextSdkClickI);
        }

        private void SendNextSdkClickI()
        {
            if (_isPaused) { return; }
            if (_packageQueue.Count == 0) { return; }

            var sdkClickPackage = _packageQueue.Dequeue();
            int retries = sdkClickPackage.Retries;

            Action action = () =>
            {
                _requestHandler.SendPackageSync(sdkClickPackage, _packageQueue.Count - 1);

                SendNextSdkClick();
            };

            if (retries <= 0)
            {
                action.Invoke();
                return;
            }

            var waitTime = Util.WaitingTime(retries, _backoffStrategy);

            _logger.Verbose("Waiting for {0} seconds before retrying sdk_click for the {1} time", Util.SecondDisplayFormat(waitTime), retries);

            _actionQueue.Delay(waitTime, action);
        }

        private void ProcessSdkClickResponseData(ResponseData responseData)
        {
            IActivityHandler activityHandler;
            if (_activityHandlerWeakReference.TryGetTarget(out activityHandler))
            {
                activityHandler.FinishedTrackingActivity(responseData);
            }
        }

        private void RetrySendingI(ActivityPackage sdkClickPackage)
        {
            var retries = sdkClickPackage.IncreaseRetries();

            _logger.Error("Retrying sdk_click package for the {0} time", retries);
            SendSdkClick(sdkClickPackage);
        }
    }
}
