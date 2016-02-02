using AdjustSdk.Pcl;
using System;
using System.IO;

namespace AdjustSdk
{
    public class AdjustConfig
    {
        public const string EnvironmentSandbox = "sandbox";
        public const string EnvironmentProduction = "production";

        internal string AppToken { get; private set; }

        internal string Environment { get; private set; }

        public string SdkPrefix { get; set; }

        public bool EventBufferingEnabled { get; set; }

        public string DefaultTracker { get; set; }

        public Func<string, byte[]> FileReader { get; set; }
        public Action<string, byte[]> FileWriter { get; set; }
        public Action<AdjustAttribution> AttributionChanged { get; set; }

        public bool HasDelegate { get { return AttributionChanged != null; } }

        private ILogger Logger { get; set; }

        public AdjustConfig(string appToken, string environment)
        {
            Logger = AdjustFactory.Logger;

            if (!IsValid(appToken, environment)) { return; }

            AppToken = appToken;
            Environment = environment;

            // default values
            EventBufferingEnabled = false;
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

        private bool CheckEnvironment(string environment)
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
