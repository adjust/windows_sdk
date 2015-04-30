using System;
namespace AdjustSdk.Pcl
{
    public class AdjustConfig
    {
        public const string EnvironmentSandbox = "sandbox";
        public const string EnvironmentProduction = "production";

        internal string AppToken { get; private set; }
        internal string Environment { get; private set; }

        public LogLevel LogLevel { get; set; }
        public string SdkPrefix { get; set; }
        public bool EventBufferingEnabled { get; set; }
        public string DefaultTracker { get; set; }
        public Action<AdjustAttribution> AttributionChanged { get; set; }
        public Action<String> LogDelegate { get; set; }
        public bool HasDelegate { get { return AttributionChanged != null; } }

        private static ILogger Logger = AdjustFactory.Logger;

        public AdjustConfig(string appToken, string environment)
        {
            if (!isValid(appToken, environment)){ return; }

            AppToken = appToken;
            Environment = environment;

            // default values
            LogLevel = LogLevel.Info;
            EventBufferingEnabled = false;
        }

        public bool isValid()
        {
            return AppToken != null;
        }

        private bool isValid(string appToken, string environment)
        {
            if (!checkAppToken(appToken)) { return false; }
            if (!checkEnvironment(environment)) { return false; }

            return true;
        }

        private bool checkAppToken(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                Logger.Error("Missing App Token");
                return false;
            }

            if (appToken.Length != 12)
            {
                Logger.Error("Malformed App Token '{0}'", appToken);
                return false;
            }

            return true;
        }

        private bool checkEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                Logger.Error("Missing environment");
                return false;
            }

            if (environment.Equals(EnvironmentSandbox))
            {
                Logger.Assert("SANDBOX: Adjust is running in Sandbox mode. " +
                   "Use this setting for testing. " +
                   "Don't forget to set the environment to `production` before publishing!");
                return true;
            }
            else if (environment.Equals(EnvironmentProduction))
            {
                Logger.Assert("PRODUCTION: Adjust is running in Production mode. " +
                           "Use this setting only for the build that you want to publish. " +
                           "Set the environment to `sandbox` if you want to test your app!");
                return true;
            }

            Logger.Error("Unknown environment '{0}'", environment);
            return false;
        }
    }
}