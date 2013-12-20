using System;
using System.Collections.Generic;
using Windows.System.Threading;

namespace adeven.AdjustIo
{
    public static class AdjustIo
    {
        static AIActivityHandler activityHandler;

        public static void AppDidLaunch(string appToken)
        {
            activityHandler = new AIActivityHandler(appToken);
        }

        //public static void AppDidActivate(string appToken)
        //{
        //    AdjustIo.appToken = appToken;
        //    trackSessionStart();
        //}

        #region oldAPI
        //public static void TrackEvent(string eventId,
        //          Dictionary<string, string> callbackParameters = null)
        //{
        //    string paramString = Util.GetBase64EncodedParameters(callbackParameters);

        //    var parameters = new Dictionary<string, string> {
        //        { "app_id", appId },
        //        { "mac", deviceId },
        //        { "id", eventId },
        //        { "params", paramString}
        //    };

        //    PostRequest request = new PostRequest("/event");
        //    request.SuccessMessage = "Tracked event " + eventId + ".";
        //    request.FailureMessage = "Failed to track event " + eventId + ".";
        //    request.UserAgent = userAgent;
        //    request.Start(parameters);
        //}

        //public static void TrackRevenue(float amountInCents,
        //                               string eventId = null,
        //           Dictionary<string, string> callbackParameters = null)
        //{
        //    int amountInMillis = (int)Math.Round(10 * amountInCents);
        //    string amount = amountInMillis.ToString();
        //    string paramString = Util.GetBase64EncodedParameters(callbackParameters);

        //    var parameters = new Dictionary<string, string> {
        //        { "app_id", appId },
        //        { "mac", deviceId },
        //        { "amount", amount },
        //        { "event_id", eventId },
        //        { "params", paramString }
        //    };

        //    PostRequest request = new PostRequest("/revenue");
        //    request.SuccessMessage = "Tracked revenue.";
        //    request.FailureMessage = "Failed to track revenue.";
        //    request.UserAgent = userAgent;
        //    request.Start(parameters);
        //}


        // This line marks the end of the public interface

        //internal static string appId;
        //internal static string deviceId;
        //internal static string userAgent;

        //private static void trackSessionStart()
        //{
        //    var parameters = new Dictionary<string, string> {
        //        { "app_id", appId },
        //        { "mac", deviceId }
        //    };

        //    PostRequest request = new PostRequest("/startup");
        //    request.SuccessMessage = "Tracked session start.";
        //    request.FailureMessage = "Failed to track session start.";
        //    request.UserAgent = userAgent;
        //    request.Start(parameters);
        //}
        #endregion

        public static void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters = null)
        {
            activityHandler.TrackEvent(eventToken, callbackParameters);
        }

        public static void SetLogLevel(AILogLevel logLevel)
        {
            AILogger.LogLevel = logLevel;
        }

        //public static void TrackEventAsync(string eventToken,
        //          Dictionary<string, string> callbackParameters = null)
        //{ 
        //    var activityHandler = new AIActivityHandler 
        //    {
        //        AppToken = appToken,
        //        CallbackParameters = callbackParameters,
        //        EventToken = eventToken,
        //    };
        //    var asyncEvent = ThreadPool.RunAsync(
        //        activityHandler.TrackEvent,
        //        WorkItemPriority.Normal,
        //        WorkItemOptions.None
        //    );
        //}

        //public static void TrackRevenueAsync(float amountInCents,
        //                               string eventId = null,
        //           Dictionary<string, string> callbackParameters = null)
        //{
        //    var activityHandler = new AIActivityHandler
        //    {
        //        CallbackParameters = callbackParameters,
        //        EventToken = eventId,
        //        AmountInCents = amountInCents,
        //    };
            
        //    var asyncAction = ThreadPool.RunAsync(
        //        activityHandler.TrackRevenue,
        //        WorkItemPriority.Normal,   
        //        WorkItemOptions.None
        //    );
        //}

    }
}
