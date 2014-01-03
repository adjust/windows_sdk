using System;
using System.Collections.Generic;
using Windows.System.Threading;

namespace adeven.AdjustIo
{
    public static class AdjustIo
    {
        public const string AIEnvironmentSandbox = "sandbox";
        public const string AIEnvironmentProduction = "production";
        
        private static AIActivityHandler activityHandler;

        public static void AppDidLaunch(string appToken)
        {
            activityHandler = new AIActivityHandler(appToken);
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

        public static void SetLogLevel(AILogLevel logLevel)
        {
            AILogger.LogLevel = logLevel;
        }

        public static void SetEnvironment(string environment)
        {
            if (activityHandler == null) 
            {
                AILogger.Error("Please call 'SetEnvironment' after 'AppDidLaunch'!");
            } 
            else if (environment == AdjustIo.AIEnvironmentSandbox) 
            {
                activityHandler.SetEnvironment(environment);
                AILogger.Assert("SANDBOX: AdjustIo is running in Sandbox mode. Use this setting for testing."
                + " Don't forget to set the environment to AIEnvironmentProduction before publishing!");
            }
            else if (environment == AdjustIo.AIEnvironmentProduction)
            {
                activityHandler.SetEnvironment(environment);
                AILogger.Assert("PRODUCTION: AdjustIo is running in Production mode."
                + " Use this setting only for the build that you want to publish."
                + " Set the environment to AIEnvironmentSandbox if you want to test your app!");
            }
            else
            {
                activityHandler.SetEnvironment("malformed");
                AILogger.Error("Malformerd environment: '{0}'", environment);
            }
        }

        public static void SetEventBufferingEnabled(bool enabledEventBuffering)
        {
            if (activityHandler == null)
            {
                AILogger.Error("Please call 'SetEventBufferingEnabled' after 'AppDidLaunch'!");
                return;
            }

            activityHandler.SetBufferedEvents(enabledEventBuffering);

            if (AIActivityHandler.IsBufferedEventsEnabled)
                AILogger.Info("Event buffering is enabled");
        }
    }
}
