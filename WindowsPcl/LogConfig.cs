using System;

namespace AdjustSdk.Pcl
{
    public static class LogConfig
    {
        public static void SetupLogging(Action<String> logDelegate, LogLevel? logLevel = null)
        {
            var logger = AdjustFactory.Logger;
            if (logger.IsLocked()) { return; }

            logger.LogDelegate = logDelegate;
            if (logLevel.HasValue)
            {
                logger.LogLevel = logLevel.Value;
            }
        }
    }
}
