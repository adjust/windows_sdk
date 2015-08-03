using System;

namespace AdjustSdk.Pcl
{
    public static class LogConfig
    {
        public static void SetupLogging(Action<String> logDelegate, LogLevel? logLevel = null)
        {
            AdjustFactory.Logger.LogDelegate = logDelegate;
            if (logLevel.HasValue)
            {
                AdjustFactory.Logger.LogLevel = logLevel.Value;
            }
        }
    }
}
