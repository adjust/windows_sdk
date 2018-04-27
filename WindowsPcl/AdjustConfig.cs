using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustConfig
    {
        public const string EnvironmentSandbox = "sandbox";
        public const string EnvironmentProduction = "production";

        private readonly ILogger _logger = AdjustFactory.Logger;

        internal string SecretId { get; private set; }
        internal string AppSecret { get; private set; }
        internal string AppToken { get; }
        internal string Environment { get; }
        internal string BasePath { get; set; }
        internal string GdprPath { get; set; }

        public string SdkPrefix { get; set; }
        public bool EventBufferingEnabled { get; set; }
        public string DefaultTracker { get; set; }
        public bool SendInBackground { get; set; }
        public TimeSpan? DelayStart { get; set; }
        internal string UserAgent { get; set; }
        internal bool DeviceKnown { get; set; }
        internal bool? StartEnabled { get; set; }
        internal bool StartOffline { get; set; }

        public Action<AdjustAttribution> AttributionChanged { get; set; }
        public Action<AdjustEventSuccess> EventTrackingSucceeded { get; set; }
        public Action<AdjustEventFailure> EventTrackingFailed { get; set; }
        public Action<AdjustSessionSuccess> SesssionTrackingSucceeded { get; set; }
        public Action<AdjustSessionFailure> SesssionTrackingFailed { get; set; }
        public Func<Uri, bool> DeeplinkResponse { get; set; }

        internal List<Action<ActivityHandler>> PreLaunchActions { get; set; }

        internal static Func<string, string> String2Sha256Func { get; set; }
        internal static Func<string, string> String2Sha512Func { get; set; }
        internal static Func<string, string> String2Md5Func { get; set; }

        public AdjustConfig(string appToken, string environment, Action<string> logDelegate = null, LogLevel? logLevel = null)
        {
            ConfigureLogger(environment, logDelegate, logLevel);

            if (!IsValid(appToken, environment)) { return; }

            AppToken = appToken;
            Environment = environment;

            // default values
            EventBufferingEnabled = false;
        }
        
        private void ConfigureLogger(string environment, Action<string> logDelegate, LogLevel? logLevel)
        {
            if (logDelegate != null)
                _logger.LogDelegate = logDelegate;

            _logger.LogLevel = logLevel ?? LogLevel.Info;
            _logger.IsProductionEnvironment = EnvironmentProduction.Equals(environment);
            _logger.IsLocked = logDelegate != null;
        }

        public void SetUserAgent(string userAgent)
        {
            UserAgent = userAgent;
        }

        public void SetDeviceKnown(bool deviceKnown)
        {
            DeviceKnown = deviceKnown;
        }

        public bool IsValid()
        {
            return AppToken != null;
        }

        private bool IsValid(string appToken, string environment)
        {
            if (!CheckAppToken(appToken)) { return false; }
            if (!CheckEnvironment(environment)) { return false; }

            return true;
        }

        private bool CheckAppToken(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                _logger.Error("Missing App Token");
                return false;
            }

            if (appToken.Length != 12)
            {
                _logger.Error("Malformed App Token '{0}'", appToken);
                return false;
            }

            return true;
        }

        private bool CheckEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                _logger.Error("Missing environment");
                return false;
            }

            if (environment.Equals(EnvironmentSandbox))
            {
                _logger.WarnInProduction("SANDBOX: Adjust is running in Sandbox mode. " +
                   "Use this setting for testing. " +
                   "Don't forget to set the environment to `production` before publishing!");

                return true;
            }

            if (environment.Equals(EnvironmentProduction))
            {
                _logger.WarnInProduction("PRODUCTION: Adjust is running in Production mode. " +
                           "Use this setting only for the build that you want to publish. " +
                           "Set the environment to `sandbox` if you want to test your app!");
                return true;
            }

            _logger.Error("Unknown environment '{0}'", environment);
            return false;
        }

        public void SetAppSecret(long secretId, long info1, long info2, long info3, long info4)
        {
            SecretId = secretId.ToString();
            AppSecret = $"{info1}{info2}{info3}{info4}";
        }
    }
}
