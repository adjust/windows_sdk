using adeven.AdjustIo.PCL;
using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Windows.System.Threading;

namespace adeven.AdjustIo
{
    public class AdjustWP
    {
        public string AIEnvironmentSandbox { get { return Util.AIEnvironmentSandbox; } }
        public string AIEnvironmentProduction { get { return Util.AIEnvironmentProduction; } }

        public string ClientSdk { get { return Util.ClientSdk; } }

        public enum AILogLevel
        {
            AILogLevelVerbose = 1,
            AILogLevelDebug,
            AILogLevelInfo,
            AILogLevelWarn,
            AILogLevelError,
            AILogLevelAssert,
        };

        private static DeviceUtil Util = new UtilWP();

        #region AdjustApi
        
        public static void AppDidLaunch(string appToken)
        {
            AdjustApi.AppDidLaunch(appToken, Util);
        }

        public static void AppDidActivate()
        {
            AdjustApi.AppDidActivate();
        }

        public static void AppDidDeactivate()
        {
            AdjustApi.AppDidDeactivate();
        }

        public static void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters = null)
        {
            AdjustApi.TrackEvent(eventToken, callbackParameters);
        }

        public static void TrackRevenue(double amountInCents,
            string eventToken = null,
            Dictionary<string, string> callbackParameters = null)
        {
            AdjustApi.TrackRevenue(amountInCents, eventToken, callbackParameters);
        }

        public static void SetLogLevel(AILogLevel logLevel)
        {
            AdjustApi.SetLogLevel((PCL.AILogLevel)logLevel);
        }

        public static void SetEnvironment(string environment)
        {
            AdjustApi.SetEnvironment(environment);
        }

        public static void SetEventBufferingEnabled(bool enabledEventBuffering)
        {
            AdjustApi.SetEventBufferingEnabled(enabledEventBuffering);
        }
        #endregion

    }
}
