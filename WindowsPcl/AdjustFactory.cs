using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public static class AdjustFactory
    {
        private static ILogger InjectedLogger;
        private static IActivityHandler IActivityHandler;
        private static IPackageHandler IPackageHandler;
        private static IRequestHandler IRequestHandler;
        private static HttpMessageHandler HttpMessageHandler;
        private static TimeSpan? SessionInterval;
        private static TimeSpan? SubsessionInterval;
        private static TimeSpan? TimerInterval;
        private static TimeSpan? TimerStart;

        public static ILogger Logger
        {
            get
            {
                // same instance of logger for all calls
                if (InjectedLogger == null)
                    InjectedLogger = new Logger();
                return InjectedLogger;
            }

            set { InjectedLogger = value; }
        }

        public static IActivityHandler GetActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            if (IActivityHandler == null)
                return ActivityHandler.GetInstance(adjustConfig, deviceUtil);

            IActivityHandler.Init(adjustConfig, deviceUtil);
            return IActivityHandler;
        }

        public static IPackageHandler GetPackageHandler(IActivityHandler activityHandler, bool startPaused)
        {
            if (IPackageHandler == null)
                return new PackageHandler(activityHandler, startPaused);
            
            IPackageHandler.Init(activityHandler, startPaused);
            return IPackageHandler;
        }

        public static IRequestHandler GetRequestHandler(IPackageHandler packageHandler)
        {
            if (IRequestHandler == null)
                return new RequestHandler(packageHandler);

            IRequestHandler.Init(packageHandler);
            return IRequestHandler;
        }

        public static HttpMessageHandler GetHttpMessageHandler()
        {
            if (HttpMessageHandler == null)
                return new HttpClientHandler();
            else
                return HttpMessageHandler;
        }

        public static TimeSpan GetSessionInterval()
        {
            if (!SessionInterval.HasValue)
                return new TimeSpan(0, 30, 0); // 30 minutes
            else
                return SessionInterval.Value;
        }

        public static TimeSpan GetSubsessionInterval()
        {
            if (!SubsessionInterval.HasValue)
                return new TimeSpan(0, 0, 1); // 1 second
            else
                return SubsessionInterval.Value;
        }

        public static TimeSpan GetTimerInterval()
        {
            if (!TimerInterval.HasValue)
                return new TimeSpan(0, 1, 0); // 1 minute
            else
                return TimerInterval.Value;
        }

        public static TimeSpan GetTimerStart()
        {
            if (!TimerStart.HasValue)
                return new TimeSpan(0, 0, 0); // 0 seconds
            else
                return TimerStart.Value;
        }

        public static void SetPackageHandler(IPackageHandler packageHandler)
        {
            IPackageHandler = packageHandler;
        }

        public static void SetRequestHandler(IRequestHandler requestHandler)
        {
            IRequestHandler = requestHandler;
        }

        public static void SetHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            HttpMessageHandler = httpMessageHandler;
        }

        public static void SetSessionInterval(TimeSpan? sessionInterval)
        {
            SessionInterval = sessionInterval;
        }

        public static void SetSubsessionInterval(TimeSpan? subsessionInterval)
        {
            SubsessionInterval = subsessionInterval;
        }

        public static void SetTimerInterval(TimeSpan? timerInterval)
        {
            TimerInterval = timerInterval;
        }

        public static void SetTimerStart(TimeSpan? timerStart)
        {
            TimerStart = timerStart;
        }
    }
}