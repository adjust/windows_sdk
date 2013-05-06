using System;
using System.Collections.Generic;

namespace adeven.AdjustIo
{
    public static class AdjustIo
    {
        public static void AppDidLaunch()
        {
            AdjustIo.appId = Util.GetAppId();
            AdjustIo.deviceId = Util.GetDeviceId();
            AdjustIo.userAgent = Util.GetUserAgent();

            trackSessionStart();
        }

        public static void AppDidActivate()
        {
            trackSessionStart();
        }

        public static void TrackEvent(string eventToken,
                  Dictionary<string, string> callbackParameters = null)
        {
            string paramString = Util.GetBase64EncodedParameters(callbackParameters);

            var parameters = new Dictionary<string, string> {
                { "app_id", appId },
                { "mac", deviceId },
                { "id", eventToken },
                { "params", paramString}
            };

            PostRequest request = new PostRequest("/event");
            request.SuccessMessage = "Tracked event " + eventToken + ".";
            request.FailureMessage = "Failed to track event " + eventToken + ".";
            request.UserAgent = userAgent;
            request.Start(parameters);
        }

        public static void TrackRevenue(float amountInCents,
                                       string eventToken = null,
                   Dictionary<string, string> callbackParameters = null)
        {
            int amountInMillis = (int)Math.Round(10 * amountInCents);
            string amount = amountInMillis.ToString();
            string paramString = Util.GetBase64EncodedParameters(callbackParameters);

            var parameters = new Dictionary<string, string> {
                { "app_id", appId },
                { "mac", deviceId },
                { "amount", amount },
                { "event_id", eventToken },
                { "params", paramString }
            };

            PostRequest request = new PostRequest("/revenue");
            request.SuccessMessage = "Tracked revenue.";
            request.FailureMessage = "Failed to track revenue.";
            request.UserAgent = userAgent;
            request.Start(parameters);
        }

        // This line marks the end of the public interface

        private static string appId;
        private static string deviceId;
        private static string userAgent;

        private static void trackSessionStart()
        {
            var parameters = new Dictionary<string, string> {
                { "app_id", appId },
                { "mac", deviceId }
            };

            PostRequest request = new PostRequest("/startup");
            request.SuccessMessage = "Tracked session start.";
            request.FailureMessage = "Failed to track session start.";
            request.UserAgent = userAgent;
            request.Start(parameters);
        }
    }
}
