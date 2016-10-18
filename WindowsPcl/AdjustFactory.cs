using System;
using System.Collections.Generic;
using System.Net.Http;

namespace AdjustSdk.Pcl
{
    public static class AdjustFactory
    {
        private static ILogger _Logger;
        private static IActivityHandler _IActivityHandler;
        private static IPackageHandler _IPackageHandler;
        private static IAttributionHandler _IAttributionHandler;
        private static IRequestHandler _IRequestHandler;
        private static ISdkClickHandler _ISdkClickHandler;
        private static HttpMessageHandler _HttpMessageHandler;
        private static TimeSpan? _SessionInterval;
        private static TimeSpan? _SubsessionInterval;
        private static TimeSpan? _TimerInterval;
        private static TimeSpan? _TimerStart;
        private static BackoffStrategy _PackageHandlerBackoffStrategy;
        private static BackoffStrategy _SdkClickHandlerBackoffStrategy;

        public static ILogger Logger
        {
            get
            {
                // same instance of logger for all calls
                if (_Logger == null)
                    _Logger = new Logger();
                return _Logger;
            }

            set { _Logger = value; }
        }

        public static IActivityHandler GetActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            if (_IActivityHandler == null)
                return ActivityHandler.GetInstance(adjustConfig, deviceUtil);

            _IActivityHandler.Init(adjustConfig, deviceUtil);
            return _IActivityHandler;
        }

        public static IPackageHandler GetPackageHandler(IActivityHandler activityHandler, bool startPaused)
        {
            if (_IPackageHandler == null)
                return new PackageHandler(activityHandler, startPaused);

            _IPackageHandler.Init(activityHandler, startPaused);
            return _IPackageHandler;
        }

        public static IAttributionHandler GetAttributionHandler(IActivityHandler activityHandler,
            ActivityPackage attributionPacakage,
            bool startPaused,
            bool hasDelegate)
        {
            if (_IAttributionHandler == null)
            {
                return new AttributionHandler(activityHandler, attributionPacakage, startPaused, hasDelegate);
            }

            _IAttributionHandler.Init(activityHandler, attributionPacakage, startPaused, hasDelegate);
            return _IAttributionHandler;
        }

        public static IRequestHandler GetRequestHandler(Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback)
        {
            if (_IRequestHandler == null)
                return new RequestHandler(sendNextCallback, retryCallback);

            _IRequestHandler.Init(sendNextCallback, retryCallback);
            return _IRequestHandler;
        }

        public static ISdkClickHandler GetSdkClickHandler(bool startPaused)
        {
            if (_ISdkClickHandler == null)
            {
                return new SdkClickHandler(startPaused);
            }
            _ISdkClickHandler.Init(startPaused);
            return _ISdkClickHandler;
        }

        public static HttpMessageHandler GetHttpMessageHandler()
        {
            if (_HttpMessageHandler == null)
                return new HttpClientHandler();
            else
                return _HttpMessageHandler;
        }

        public static TimeSpan GetSessionInterval()
        {
            if (!_SessionInterval.HasValue)
                return new TimeSpan(0, 30, 0); // 30 minutes
            else
                return _SessionInterval.Value;
        }

        public static TimeSpan GetSubsessionInterval()
        {
            if (!_SubsessionInterval.HasValue)
                return new TimeSpan(0, 0, 1); // 1 second
            else
                return _SubsessionInterval.Value;
        }

        public static TimeSpan GetTimerInterval()
        {
            if (!_TimerInterval.HasValue)
                return new TimeSpan(0, 1, 0); // 1 minute
            else
                return _TimerInterval.Value;
        }

        public static TimeSpan GetTimerStart()
        {
            if (!_TimerStart.HasValue)
                return new TimeSpan(0, 0, 0); // 0 seconds
            else
                return _TimerStart.Value;
        }

        public static BackoffStrategy GetPackageHandlerBackoffStrategy()
        {
            if (_PackageHandlerBackoffStrategy == null)
            {
                return BackoffStrategy.LongWait;
            }
            return _PackageHandlerBackoffStrategy;
        }

        public static BackoffStrategy GetSdkClickHandlerBackoffStrategy()
        {
            if (_SdkClickHandlerBackoffStrategy == null)
            {
                return BackoffStrategy.ShortWait;
            }
            return _SdkClickHandlerBackoffStrategy;
        }

        public static void SetActivityHandler(IActivityHandler activityHandler)
        {
            _IActivityHandler = activityHandler;
        }

        public static void SetPackageHandler(IPackageHandler packageHandler)
        {
            _IPackageHandler = packageHandler;
        }

        public static void SetAttributionHandler(IAttributionHandler attributionHandler)
        {
            _IAttributionHandler = attributionHandler;
        }

        public static void SetRequestHandler(IRequestHandler requestHandler)
        {
            _IRequestHandler = requestHandler;
        }

        public static void SetHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            _HttpMessageHandler = httpMessageHandler;
        }

        public static void SetSessionInterval(TimeSpan? sessionInterval)
        {
            _SessionInterval = sessionInterval;
        }

        public static void SetSubsessionInterval(TimeSpan? subsessionInterval)
        {
            _SubsessionInterval = subsessionInterval;
        }

        public static void SetTimerInterval(TimeSpan? timerInterval)
        {
            _TimerInterval = timerInterval;
        }

        public static void SetTimerStart(TimeSpan? timerStart)
        {
            _TimerStart = timerStart;
        }

        public static void SetPackageHandlerBackoffStrategy(BackoffStrategy backoffStrategy)
        {
            _PackageHandlerBackoffStrategy = backoffStrategy;
        }

        public static void SetSdkClickHandlerBackoffStrategy(BackoffStrategy backoffStrategy)
        {
            _SdkClickHandlerBackoffStrategy = backoffStrategy;
        }
    }
}