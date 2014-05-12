using AdjustSdk;
using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class AdjustApi
    {
        // keep this enum in sync with WS and WP Adjust class
        public enum Environment
        {
            Sandbox,
            Production,
            Unknown
        }

        private static IActivityHandler ActivityHandler;
        private static DeviceUtil DeviceSpecific;
        private static ILogger Logger = AdjustFactory.Logger;

        public static void AppDidLaunch(string appToken, DeviceUtil deviceSpecific)
        {
            DeviceSpecific = deviceSpecific;
            ActivityHandler = new ActivityHandler(appToken, deviceSpecific);
        }

        public static void AppDidActivate()
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'AppDidActivate' after 'AppDidLaunch'!");
                return;
            }
            ActivityHandler.TrackSubsessionStart();
        }

        public static void AppDidDeactivate()
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'AppDidDeactivate' after 'AppDidLaunch'!");
                return;
            }
            ActivityHandler.TrackSubsessionEnd();
        }

        public static void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters = null)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'TrackEvent' after 'AppDidLaunch'!");
                return;
            }
            ActivityHandler.TrackEvent(eventToken, callbackParameters);
        }

        public static void TrackRevenue(double amountInCents,
            string eventToken = null,
            Dictionary<string, string> callbackParameters = null)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'TrackRevenue' after 'AppDidLaunch'!");
                return;
            }
            ActivityHandler.TrackRevenue(amountInCents, eventToken, callbackParameters);
        }

        public static void SetLogLevel(LogLevel logLevel)
        {
            Logger.LogLevel = logLevel;
        }

        public static void SetEnvironment(AdjustEnvironment environment)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'SetEnvironment' after 'AppDidLaunch'!");
            }
            else if ((AdjustApi.Environment)environment == AdjustApi.Environment.Sandbox)
            {
                ActivityHandler.SetEnvironment((AdjustApi.Environment)environment);
                Logger.Assert("SANDBOX: Adjust is running in Sandbox mode. Use this setting for testing."
                    + " Don't forget to set the environment to AIEnvironmentProduction before publishing!");
            }
            else if ((AdjustApi.Environment)environment == AdjustApi.Environment.Production)
            {
                ActivityHandler.SetEnvironment((AdjustApi.Environment)environment);
                Logger.Assert("PRODUCTION: Adjust is running in Production mode."
                    + " Use this setting only for the build that you want to publish."
                    + " Set the environment to AIEnvironmentSandbox if you want to test your app!");
            }
            else
            {
                Logger.Error("Malformerd environment: '{0}'", environment.ToString());
            }
        }

        public static void SetEventBufferingEnabled(bool enabledEventBuffering)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'SetEventBufferingEnabled' after 'AppDidLaunch'!");
                return;
            }

            ActivityHandler.SetBufferedEvents(enabledEventBuffering);

            if (ActivityHandler.IsBufferedEventsEnabled)
                Logger.Info("Event buffering is enabled");
        }

        public static void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'SetResponseDelegate' after 'AppDidLaunch'!");
                return;
            }

            ActivityHandler.SetResponseDelegate(responseDelegate);
        }

        public static void SetEnabled(bool enabled)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'SetEnabled' after 'AppDidLaunch'!");
                return;
            }

            ActivityHandler.SetEnabled(enabled);
        }

        public static bool IsEnabled()
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Please call 'IsEnabled' after 'AppDidLaunch'!");
                return false;
            }

            return ActivityHandler.IsEnabled();
        }

        public static void AppWillOpenUrl(Uri url)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Can only open url after 'AppDidLaunch'!");
                return;
            }

            ActivityHandler.ReadOpenUrl(url);
        }

        public static void SetSdkPrefix(string sdkPrefix)
        {
            if (ActivityHandler == null)
            {
                Logger.Error("Can only set SDK prefix after 'AppDidLaunch'!");
                return;
            }

            ActivityHandler.SetSdkPrefix(sdkPrefix);
        }
    }
}
