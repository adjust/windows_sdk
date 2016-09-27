using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class SdkClickHandler : ISdkClickHandler
    {
        private ILogger _Logger = AdjustFactory.Logger;
        private ActionQueue _ActionQueue = new ActionQueue("adjust.SdkClickHandler");
        private BackoffStrategy _backoffStrategy = AdjustFactory.GetSdkClickHandlerBackoffStrategy();
        private Queue<ActivityPackage> _PackageQueue = new Queue<ActivityPackage>();
        private IRequestHandler _RequestHandler;

        private bool _IsPaused;

        public SdkClickHandler(bool startPaused)
        {
            Init(startPaused);
            _RequestHandler = new RequestHandler(sendNextCallback: null,
                retryCallback: (_, sdkClickPackage) => RetrySendingI(sdkClickPackage));
        }

        public void Init(bool startPaused)
        {
            _IsPaused = startPaused;
        }

        public void PauseSending()
        {
            _IsPaused = true;
        }

        public void ResumeSending()
        {
            _IsPaused = false;

            SendNextSdkClick();
        }

        public void SendSdkClick(ActivityPackage sdkClickPackage)
        {
            _ActionQueue.Enqueue(() =>
            {
                _PackageQueue.Enqueue(sdkClickPackage);

                _Logger.Debug("Added sdk_click {0}", _PackageQueue.Count);
                _Logger.Verbose("{0}", sdkClickPackage.GetExtendedString());

                SendNextSdkClick();
            });
        }

        private void SendNextSdkClick()
        {
            _ActionQueue.Enqueue(() => SendNextSdkClickI());
        }

        private void SendNextSdkClickI()
        {
            if (_IsPaused) { return; }
            if (_PackageQueue.Count == 0) { return; }

            var sdkClickPackage = _PackageQueue.Dequeue();
            int retries = sdkClickPackage.Retries;

            Action action = () =>
            {
                _RequestHandler.SendPackageSync(sdkClickPackage);
                SendNextSdkClick();
            };

            if (retries <= 0)
            {
                action.Invoke();
                return;
            }

            var waitTime = Util.WaitingTime(retries, _backoffStrategy);

            _Logger.Verbose("Waiting for {0} seconds before retrying sdk_click for the {1} time", Util.SecondDisplayFormat(waitTime), retries);

            _ActionQueue.Delay(waitTime, action);
        }

        private void RetrySendingI(ActivityPackage sdkClickPackage)
        {
            var retries = sdkClickPackage.IncreaseRetries();

            _Logger.Error("Retrying sdk_click package for the {0} time", retries);
            SendSdkClick(sdkClickPackage);
        }
    }
}
