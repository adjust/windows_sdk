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
        private static IPackageHandler InjectedPackageHandler;
        private static IRequestHandler InjectedRequestHandler;
        private static HttpMessageHandler InjectedHttpMessageHandler;
        private static TimeSpan? InjectedSessionInterval;
        private static TimeSpan? InjectedSubsessionInterval;
        private static TimeSpan? InjectedTimerInterval;

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

        public static IPackageHandler GetPackageHandler(IActivityHandler activityHandler)
        {
            if (InjectedPackageHandler == null)
                return new PackageHandler(activityHandler);
            else
                return InjectedPackageHandler;
        }

        public static IRequestHandler GetRequestHandler(IPackageHandler packageHandler)
        {
            if (InjectedRequestHandler == null)
                return new RequestHandler(packageHandler);
            else
                return InjectedRequestHandler;
        }

        public static HttpMessageHandler GetHttpMessageHandler()
        {
            if (InjectedHttpMessageHandler == null)
                return new HttpClientHandler();
            else
                return InjectedHttpMessageHandler;
        }

        public static TimeSpan GetSessionInterval()
        {
            if (!InjectedSessionInterval.HasValue)
                return new TimeSpan(0, 30, 0); // 30 minutes
            else
                return InjectedSessionInterval.Value;
        }

        public static TimeSpan GetSubsessionInterval()
        {
            if (!InjectedSubsessionInterval.HasValue)
                return new TimeSpan(0, 0, 1); // 1 second
            else
                return InjectedSubsessionInterval.Value;
        }

        public static TimeSpan GetTimerInterval()
        {
            if (!InjectedTimerInterval.HasValue)
                return new TimeSpan(0, 1, 0); // 1 minute
            else
                return InjectedTimerInterval.Value;
        }

        public static void SetPackageHandler(IPackageHandler packageHandler)
        {
            InjectedPackageHandler = packageHandler;
        }

        public static void SetRequestHandler(IRequestHandler requestHandler)
        {
            InjectedRequestHandler = requestHandler;
        }

        public static void SetHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {
            InjectedHttpMessageHandler = httpMessageHandler;
        }

        public static void SetSessionInterval(TimeSpan? sessionInterval)
        {
            InjectedSessionInterval = sessionInterval;
        }

        public static void SetSubsessionInterval(TimeSpan? subsessionInterval)
        {
            InjectedSubsessionInterval = subsessionInterval;
        }

        public static void SetTimerInterval(TimeSpan? timerInterval)
        {
            InjectedTimerInterval = timerInterval;
        }
    }
}