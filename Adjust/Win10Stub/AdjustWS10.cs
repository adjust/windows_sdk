using System;
using System.Collections.Generic;

namespace Win10Interface
{
    public class AdjustWS10
    {
        public static void ApplicationLaunching(
            string appToken, string environment, string sdkPrefix,
            bool sendInBackground, double delayStart, string userAgent,
            string defaultTracker, bool? eventBufferingEnabled, bool launchDeferredDeeplink,
            string logLevelString, Action<string> logDelegate,
            Action<Dictionary<string, string>> actionAttributionChangedData,
            Action<Dictionary<string, string>> actionSessionSuccessData,
            Action<Dictionary<string, string>> actionSessionFailureData,
            Action<Dictionary<string, string>> actionEventSuccessData,
            Action<Dictionary<string, string>> actionEventFailureData)
        {
        }

        public static void TrackEvent(string eventToken, double? revenue, string currency,
            string purchaseId, List<string> callbackList, List<string> partnerList)
        {
        }

        public static void ApplicationActivated()
        {
        }

        public static void ApplicationDeactivated()
        {
        }

        public static bool IsEnabled()
        {
            return false;
        }

        public static void SetEnabled(bool enabled)
        {
        }

        public static void SetOfflineMode(bool offlineMode)
        {
        }

        public static void SendFirstPackages()
        {
        }

        public static void SetDeviceToken(string deviceToken)
        {
        }

        public static Dictionary<string, string> GetAttribution()
        {
            return null;
        }

        public static string GetWindowsAdId()
        {
            return string.Empty;
        }

        public static string GetAdid()
        {
            return string.Empty;
        }

        public static void AddSessionCallbackParameter(string key, string value)
        {
        }

        public static void AddSessionPartnerParameter(string key, string value)
        {
        }

        public static void RemoveSessionCallbackParameter(string key)
        {
        }

        public static void RemoveSessionPartnerParameter(string key)
        {
        }

        public static void ResetSessionCallbackParameters()
        {
        }

        public static void ResetSessionPartnerParameters()
        {
        }
    }
}
