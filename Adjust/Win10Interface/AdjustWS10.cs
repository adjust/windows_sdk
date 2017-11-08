using System;
using System.Collections.Generic;
using System.Linq;
using AdjustSdk;

namespace Win10Interface
{
    public class AdjustWS10
    {
        public static void ApplicationLaunching(AdjustConfigDto adjustConfigDto)
        {
            LogLevel logLevel;
            Enum.TryParse(adjustConfigDto.LogLevelString, out logLevel);

            var config = new AdjustConfig(adjustConfigDto.AppToken, adjustConfigDto.Environment, 
                adjustConfigDto.LogDelegate, logLevel)
            {
                DefaultTracker = adjustConfigDto.DefaultTracker,
                SdkPrefix = adjustConfigDto.SdkPrefix,
                SendInBackground = adjustConfigDto.SendInBackground,
                DelayStart = TimeSpan.FromSeconds(adjustConfigDto.DelayStart)
            };
 
            // TODO: launchDeferredDeeplink

            // config.SetAppSecret(0, 0, 0, 0, 0);

            config.SetUserAgent(adjustConfigDto.UserAgent);

            if (adjustConfigDto.EventBufferingEnabled.HasValue)
            {
                config.EventBufferingEnabled = adjustConfigDto.EventBufferingEnabled.Value;
            }

            if (adjustConfigDto.ActionAttributionChangedData != null)
            {
                config.AttributionChanged = attribution =>
                {
                    var attributionMap = AdjustAttribution
                        .ToDictionary(attribution)
                        // convert from <string, object> to <string, string>
                        .ToDictionary(x => x.Key, x => x.Value.ToString());
                    adjustConfigDto.ActionAttributionChangedData(attributionMap);
                };
            }

            if (adjustConfigDto.ActionSessionSuccessData != null)
            {
                config.SesssionTrackingSucceeded = session =>
                    adjustConfigDto.ActionSessionSuccessData(AdjustSessionSuccess.ToDictionary(session));
            }

            if (adjustConfigDto.ActionSessionFailureData != null)
            {
                config.SesssionTrackingFailed = session =>
                    adjustConfigDto.ActionSessionFailureData(AdjustSessionFailure.ToDictionary(session));
            }

            if (adjustConfigDto.ActionEventSuccessData != null)
            {
                config.EventTrackingSucceeded = adjustEvent =>
                    adjustConfigDto.ActionEventSuccessData(AdjustEventSuccess.ToDictionary(adjustEvent));
            }

            if (adjustConfigDto.ActionEventFailureData != null)
            {
                config.EventTrackingFailed = adjustEvent =>
                    adjustConfigDto.ActionEventFailureData(AdjustEventFailure.ToDictionary(adjustEvent));
            }

            Adjust.ApplicationLaunching(config);
        }

        public static void TrackEvent(string eventToken, double? revenue, string currency,
            string purchaseId, List<string> callbackList, List<string> partnerList)
        {
            var adjustEvent = new AdjustEvent(eventToken)
                {PurchaseId = purchaseId};

            if (revenue.HasValue)
            {
                adjustEvent.SetRevenue(revenue.Value, currency);
            }

            if (callbackList != null)
            {
                for (int i = 0; i < callbackList.Count; i += 2)
                {
                    var key = callbackList[i];
                    var value = callbackList[i + 1];

                    adjustEvent.AddCallbackParameter(key, value);
                }
            }

            if (partnerList != null)
            {
                for (int i = 0; i < partnerList.Count; i += 2)
                {
                    var key = partnerList[i];
                    var value = partnerList[i + 1];

                    adjustEvent.AddPartnerParameter(key, value);
                }
            }

            Adjust.TrackEvent(adjustEvent);
        }

        public static void ApplicationActivated()
        {
            Adjust.ApplicationActivated();
        }

        public static void ApplicationDeactivated()
        {
            Adjust.ApplicationDeactivated();
        }

        public static bool IsEnabled()
        {
            return Adjust.IsEnabled();
        }

        public static void SetEnabled(bool enabled)
        {
            Adjust.SetEnabled(enabled);
        }

        public static void SetOfflineMode(bool offlineMode)
        {
            Adjust.SetOfflineMode(offlineMode);
        }

        public static void SendFirstPackages()
        {
            Adjust.SendFirstPackages();
        }

        public static void SetDeviceToken(string deviceToken)
        {
            Adjust.SetPushToken(deviceToken);
        }

        public static Dictionary<string, string> GetAttribution()
        {
            var attribution = Adjust.GetAttributon();
            return AdjustAttribution
                .ToDictionary(attribution)
                // convert from <string, object> to <string, string>
                .ToDictionary(x => x.Key, x => x.Value.ToString());
        }

        public static string GetWindowsAdId()
        {
            return Adjust.GetWindowsAdId();
        }

        public static string GetAdid()
        {
            return Adjust.GetAdid();
        }

        public static void AddSessionCallbackParameter(string key, string value)
        {
            Adjust.AddSessionCallbackParameter(key, value);
        }

        public static void AddSessionPartnerParameter(string key, string value)
        {
            Adjust.AddSessionPartnerParameter(key, value);
        }

        public static void RemoveSessionCallbackParameter(string key)
        {
            Adjust.RemoveSessionCallbackParameter(key);
        }

        public static void RemoveSessionPartnerParameter(string key)
        {
            Adjust.RemoveSessionPartnerParameter(key);
        }

        public static void ResetSessionCallbackParameters()
        {
            Adjust.ResetSessionCallbackParameters();
        }

        public static void ResetSessionPartnerParameters()
        {
            Adjust.ResetSessionPartnerParameters();
        }
    }
}
