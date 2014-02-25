﻿using AdjustSdk;
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

        private static ActivityHandler activityHandler;
        private static DeviceUtil DeviceSpecific;
        private static ILogger Logger = AdjustFactory.Logger;

        public static void AppDidLaunch(string appToken, DeviceUtil deviceSpecific)
        {
            DeviceSpecific = deviceSpecific;
            activityHandler = new ActivityHandler(appToken, deviceSpecific);
        }

        public static void AppDidActivate()
        {
            activityHandler.TrackSubsessionStart();
        }

        public static void AppDidDeactivate()
        {
            activityHandler.TrackSubsessionEnd();
        }

        public static void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters = null)
        {
            activityHandler.TrackEvent(eventToken, callbackParameters);
        }

        public static void TrackRevenue(double amountInCents,
            string eventToken = null,
            Dictionary<string, string> callbackParameters = null)
        {
            activityHandler.TrackRevenue(amountInCents, eventToken, callbackParameters);
        }

        public static void SetLogLevel(LogLevel logLevel)
        {
            Logger.LogLevel = logLevel;
        }

        public static void SetEnvironment(AdjustEnvironment environment)
        {
            if (activityHandler == null)
            {
                Logger.Error("Please call 'SetEnvironment' after 'AppDidLaunch'!");
            }
            else if ((AdjustApi.Environment)environment == AdjustApi.Environment.Sandbox)
            {
                activityHandler.SetEnvironment((AdjustApi.Environment)environment);
                Logger.Assert("SANDBOX: Adjust is running in Sandbox mode. Use this setting for testing."
                    + " Don't forget to set the environment to AIEnvironmentProduction before publishing!");
            }
            else if ((AdjustApi.Environment)environment == AdjustApi.Environment.Production)
            {
                activityHandler.SetEnvironment((AdjustApi.Environment)environment);
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
            if (activityHandler == null)
            {
                Logger.Error("Please call 'SetEventBufferingEnabled' after 'AppDidLaunch'!");
                return;
            }

            activityHandler.SetBufferedEvents(enabledEventBuffering);

            if (activityHandler.IsBufferedEventsEnabled)
                Logger.Info("Event buffering is enabled");
        }

        public static void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            if (activityHandler == null)
            {
                Logger.Error("Please call 'SetResponseDelegate' after 'AppDidLaunch'!");
                return;
            }

            activityHandler.SetResponseDelegate(responseDelegate);
        }
    }
}