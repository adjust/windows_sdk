using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class SdkClickHandler : ISdkClickHandler
    {
        private ILogger _logger = AdjustFactory.Logger;
        private ActionQueue _actionQueue = new ActionQueue("adjust.SdkClickHandler");
        private BackoffStrategy _backoffStrategy = AdjustFactory.GetSdkClickHandlerBackoffStrategy();
        private Queue<ActivityPackage> _packageQueue = new Queue<ActivityPackage>();
        private IRequestHandler _requestHandler;
        private WeakReference<IActivityHandler> _activityHandlerWeakReference;

        private bool _isPaused;

        public SdkClickHandler(IActivityHandler activityHandler, bool startPaused)
        {
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
            _actionQueue.Enqueue(() => SendNextSdkClickI());
        }

        private void SendNextSdkClickI()
        {
            if (_isPaused) { return; }
            if (_packageQueue.Count == 0) { return; }

            var sdkClickPackage = _packageQueue.Dequeue();
            int retries = sdkClickPackage.Retries;

            Action action = () =>
            {
                _requestHandler.SendPackageSync(sdkClickPackage);
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
