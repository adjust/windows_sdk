using System;
using System.Collections.Generic;
using Windows.System.Threading;
using PCL = adeven.AdjustIo.PCL;

namespace adeven.AdjustIo
{
    public static class AdjustIo
    {
        public const string AIEnvironmentSandbox = "sandbox";
        public const string AIEnvironmentProduction = "production";
        
        private static PCL.AIActivityHandler activityHandler;

        public enum AILogLevel
        {
            AILogLevelVerbose = 1,
            AILogLevelDebug,
            AILogLevelInfo,
            AILogLevelWarn,
            AILogLevelError,
            AILogLevelAssert,
        };

        public static void AppDidLaunch(string appToken)
        {
            activityHandler = new PCL.AIActivityHandler(appToken, new PCL.AIActivityHandler.DeviceUtil 
                                                                    {   DeviceId = Util.GetDeviceId(),
                                                                        ClientSdk = Util.ClientSdk, 
                                                                        UserAgent = Util.GetUserAgent(),
                                                                        Md5Function = Util.GetMd5Hash});
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
            PCL.AILogger.LogLevel = (PCL.AILogLevel)logLevel;
        }

        public static void SetEnvironment(string environment)
        {
            if (activityHandler == null) 
            {
                PCL.AILogger.Error("Please call 'SetEnvironment' after 'AppDidLaunch'!");
            } 
            else if (environment == AdjustIo.AIEnvironmentSandbox) 
            {
                activityHandler.SetEnvironment(environment);
                PCL.AILogger.Assert("SANDBOX: AdjustIo is running in Sandbox mode. Use this setting for testing."
                + " Don't forget to set the environment to AIEnvironmentProduction before publishing!");
            }
            else if (environment == AdjustIo.AIEnvironmentProduction)
            {
                activityHandler.SetEnvironment(environment);
                PCL.AILogger.Assert("PRODUCTION: AdjustIo is running in Production mode."
                + " Use this setting only for the build that you want to publish."
                + " Set the environment to AIEnvironmentSandbox if you want to test your app!");
            }
            else
            {
                activityHandler.SetEnvironment("malformed");
                PCL.AILogger.Error("Malformerd environment: '{0}'", environment);
            }
        }

        public static void SetEventBufferingEnabled(bool enabledEventBuffering)
        {
            if (activityHandler == null)
            {
                PCL.AILogger.Error("Please call 'SetEventBufferingEnabled' after 'AppDidLaunch'!");
                return;
            }

            activityHandler.SetBufferedEvents(enabledEventBuffering);

            if (PCL.AIActivityHandler.IsBufferedEventsEnabled)
                PCL.AILogger.Info("Event buffering is enabled");
        }
    }
}
