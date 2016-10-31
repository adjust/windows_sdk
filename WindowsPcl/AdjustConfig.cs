using AdjustSdk.Pcl;
using System;

namespace AdjustSdk
{
    public class AdjustConfig
    {
        public const string EnvironmentSandbox = "sandbox";
        public const string EnvironmentProduction = "production";

        private ILogger _Logger = AdjustFactory.Logger;

        internal string AppToken { get; private set; }
        internal string Environment { get; private set; }

        public string SdkPrefix { get; set; }
        public bool EventBufferingEnabled { get; set; }
        public string DefaultTracker { get; set; }
        public bool SendInBackground { get; set; }
        public TimeSpan? DelayStart { get; set; }

        public Action<AdjustAttribution> AttributionChanged { get; set; }
        public Action<AdjustEventSuccess> EventTrackingSucceeded { get; set; }
        public Action<AdjustEventFailure> EventTrackingFailed { get; set; }
        public Action<AdjustSessionSuccess> SesssionTrackingSucceeded { get; set; }
        public Action<AdjustSessionFailure> SesssionTrackingFailed { get; set; }

        public bool HasAttributionDelegate { get { return AttributionChanged != null; } }
        public bool HasResponseDelegate
        {
            get
            {
                return HasAttributionDelegate ||
                    EventTrackingSucceeded != null ||
                    EventTrackingFailed != null ||
                    SesssionTrackingSucceeded != null ||
                    SesssionTrackingFailed != null;
            }
        }

        internal List<Action<ActivityHandler>> SessionParametersActions;

        public AdjustConfig(string appToken, string environment, bool allowSupressLevel = false)
        {
            ConfigureLogLevel(environment, allowSupressLevel);
            if (!IsValid(appToken, environment)) { return; }

            AppToken = appToken;
            Environment = environment;

            // default values
            EventBufferingEnabled = false;
        }

        private void ConfigureLogLevel(string environment, bool allowSupressLevel)
        {
            if (AdjustConfig.EnvironmentProduction.Equals(environment))
            {
                if (allowSupressLevel)
                {
                    _Logger.LogLevel = LogLevel.Suppress;
                }
                else
                {
                    _Logger.LogLevel = LogLevel.Assert;
                }
            }
            else
            {
                if (!allowSupressLevel &&
                    _Logger.LogLevel == LogLevel.Suppress)
                {
                    _Logger.LogLevel = LogLevel.Assert;
                }
            }
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
                _Logger.Error("Missing App Token");
                return false;
            }

            if (appToken.Length != 12)
            {
                _Logger.Error("Malformed App Token '{0}'", appToken);
                return false;
            }

            return true;
        }

        private bool CheckEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                _Logger.Error("Missing environment");
                return false;
            }

            if (environment.Equals(EnvironmentSandbox))
            {
                _Logger.Assert("SANDBOX: Adjust is running in Sandbox mode. " +
                   "Use this setting for testing. " +
                   "Don't forget to set the environment to `production` before publishing!");

                return true;
            }
            else if (environment.Equals(EnvironmentProduction))
            {
                _Logger.Assert("PRODUCTION: Adjust is running in Production mode. " +
                           "Use this setting only for the build that you want to publish. " +
                           "Set the environment to `sandbox` if you want to test your app!");
                return true;
            }

            _Logger.Error("Unknown environment '{0}'", environment);
            return false;
        }
    }
}
