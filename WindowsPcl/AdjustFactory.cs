using System;
using System.Collections.Generic;
using System.Net.Http;

namespace AdjustSdk.Pcl
{
    public static class AdjustFactory
    {
        private static readonly TimeSpan DefaultSessionInterval = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultSubsessionInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultTimerInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DefaultTimerStart = TimeSpan.FromSeconds(0);
        private static readonly TimeSpan DefaultMaxDelayStart = TimeSpan.FromSeconds(10);

        private static ILogger _logger;
        private static IActivityHandler _iActivityHandler;
        private static IPackageHandler _iPackageHandler;
        private static IAttributionHandler _iAttributionHandler;
        private static IRequestHandler _iRequestHandler;
        private static ISdkClickHandler _iSdkClickHandler;
        private static HttpMessageHandler _httpMessageHandler;
        private static TimeSpan? _sessionInterval;
        private static TimeSpan? _subsessionInterval;
        private static TimeSpan? _timerInterval;
        private static TimeSpan? _timerStart;
        private static BackoffStrategy _packageHandlerBackoffStrategy;
        private static BackoffStrategy _sdkClickHandlerBackoffStrategy;
        private static TimeSpan? _maxDelayStart;

        private static string _baseUrl = null;
        public static string BaseUrl
        {
            get
            {
                if (_baseUrl == null)
                {
                    return Constants.BASE_URL;
                }
                return _baseUrl;
            }

            set
            {
                _baseUrl = value;
            }
        }

        private static string _gdprUrl = null;
        public static string GdprUrl
        {
            get
            {
                if (_gdprUrl == null)
                {
                    return Constants.GDPR_URL;
                }
                return _gdprUrl;
            }

            set
            {
                _gdprUrl = value;
            }
        }

        public static ILogger Logger
        {
            get
            {
                // same instance of logger for all calls
                if (_logger == null)
                    _logger = new Logger();
                return _logger;
            }

            set { _logger = value; }
        }
        
        public static IActivityHandler GetActivityHandler(AdjustConfig adjustConfig, IDeviceUtil deviceUtil)
        {
            if (_iActivityHandler == null)
                return ActivityHandler.GetInstance(adjustConfig, deviceUtil);

            _iActivityHandler.Init(adjustConfig, deviceUtil);
            return _iActivityHandler;
        }

        public static IPackageHandler GetPackageHandler(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused)
        {
            if (_iPackageHandler == null)
                return new PackageHandler(activityHandler, deviceUtil, startPaused);

            _iPackageHandler.Init(activityHandler, deviceUtil, startPaused);
            return _iPackageHandler;
        }

        public static IAttributionHandler GetAttributionHandler(IActivityHandler activityHandler,
            ActivityPackage attributionPacakage,
            bool startPaused)
        {
            if (_iAttributionHandler == null)
            {
                return new AttributionHandler(activityHandler, attributionPacakage, startPaused);
            }

            _iAttributionHandler.Init(activityHandler, attributionPacakage, startPaused);
            return _iAttributionHandler;
        }

        public static IRequestHandler GetRequestHandler(IActivityHandler activityHandler, Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback)
        {
            if (_iRequestHandler == null)
                return new RequestHandler(activityHandler, sendNextCallback, retryCallback);

            _iRequestHandler.Init(activityHandler, sendNextCallback, retryCallback);
            return _iRequestHandler;
        }

        public static ISdkClickHandler GetSdkClickHandler(IActivityHandler activityHandler, bool startPaused)
        {
            if (_iSdkClickHandler == null)
            {
                return new SdkClickHandler(activityHandler, startPaused);
            }
            _iSdkClickHandler.Init(activityHandler, startPaused);
            return _iSdkClickHandler;
        }

        public static TimeSpan GetSessionInterval()
        {
            if (!_sessionInterval.HasValue)
                return DefaultSessionInterval; // 30 minutes
            else
                return _sessionInterval.Value;
        }

        public static TimeSpan GetSubsessionInterval()
        {
            if (!_subsessionInterval.HasValue)
                return DefaultSubsessionInterval; // 1 second
            else
                return _subsessionInterval.Value;
        }

        public static TimeSpan GetTimerInterval()
        {
            if (!_timerInterval.HasValue)
                return DefaultTimerInterval; // 1 minute
            else
                return _timerInterval.Value;
        }

        public static TimeSpan GetTimerStart()
        {
            if (!_timerStart.HasValue)
                return DefaultTimerStart; // 0 seconds
            else
                return _timerStart.Value;
        }

        public static BackoffStrategy GetPackageHandlerBackoffStrategy()
        {
            if (_packageHandlerBackoffStrategy == null)
            {
                return BackoffStrategy.LongWait;
            }
            return _packageHandlerBackoffStrategy;
        }

        public static BackoffStrategy GetSdkClickHandlerBackoffStrategy()
        {
            if (_sdkClickHandlerBackoffStrategy == null)
            {
                return BackoffStrategy.ShortWait;
            }
            return _sdkClickHandlerBackoffStrategy;
        }
        
        public static TimeSpan GetMaxDelayStart()
        {
            if (_maxDelayStart == null)
            {
                return DefaultMaxDelayStart;
            }
            return _maxDelayStart.Value;
        }

        public static void SetActivityHandler(IActivityHandler activityHandler)
        {
            _iActivityHandler = activityHandler;
        }

        public static void SetPackageHandler(IPackageHandler packageHandler)
        {
            _iPackageHandler = packageHandler;
        }

        public static void SetAttributionHandler(IAttributionHandler attributionHandler)
        {
            _iAttributionHandler = attributionHandler;
        }

        public static void SetSdkClickHandler(ISdkClickHandler sdkClickHandler)
        {
            _iSdkClickHandler = sdkClickHandler;
        }

        public static void SetRequestHandler(IRequestHandler requestHandler)
        {
            _iRequestHandler = requestHandler;
        }

        public static void SetHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            _httpMessageHandler = httpMessageHandler;
        }

        public static void SetSessionInterval(TimeSpan? sessionInterval)
        {
            _sessionInterval = sessionInterval;
        }

        public static void SetSubsessionInterval(TimeSpan? subsessionInterval)
        {
            _subsessionInterval = subsessionInterval;
        }

        public static void SetTimerInterval(TimeSpan? timerInterval)
        {
            _timerInterval = timerInterval;
        }

        public static void SetTimerStart(TimeSpan? timerStart)
        {
            _timerStart = timerStart;
        }

        public static void SetPackageHandlerBackoffStrategy(BackoffStrategy backoffStrategy)
        {
            _packageHandlerBackoffStrategy = backoffStrategy;
        }

        public static void SetSdkClickHandlerBackoffStrategy(BackoffStrategy backoffStrategy)
        {
            _sdkClickHandlerBackoffStrategy = backoffStrategy;
        }

        public static void SetMaxDelayStart(TimeSpan maxDelayStart)
        {
            _maxDelayStart = maxDelayStart;
        }

        public static void Teardown()
        {
            _iPackageHandler = null;
            _iRequestHandler = null;
            _iAttributionHandler = null;
            _logger = null;
            _httpMessageHandler?.Dispose();
            _httpMessageHandler = null;
            _packageHandlerBackoffStrategy = null;
            _sdkClickHandlerBackoffStrategy = null;

            _timerInterval = DefaultTimerInterval;
            _timerStart = DefaultTimerStart;
            _sessionInterval = DefaultSessionInterval;
            _subsessionInterval = DefaultSubsessionInterval;
            _maxDelayStart = DefaultMaxDelayStart;
            _baseUrl = Constants.BASE_URL;
        }
    }
}