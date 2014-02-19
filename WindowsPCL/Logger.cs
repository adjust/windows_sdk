using AdjustSdk;
using System;

namespace AdjustSdk.PCL
{
    public static class Logger
    {
        public static string LogTag { get; set; }

        public static LogLevel LogLevel { get; set; }

        static Logger()
        {
            LogTag = "Adjust";
            LogLevel = LogLevel.Info;
        }

        public static void Verbose(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Verbose, "v", message, parameters);
        }

        public static void Debug(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Debug, "d", message, parameters);
        }

        public static void Info(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Info, "i", message, parameters);
        }

        public static void Warn(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Warn, "w", message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Error, "e", message, parameters);
        }

        public static void Assert(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Assert, "a", message, parameters);
        }

        private static void LoggingLevel(LogLevel logLevel, string logLevelString, string message, object[] parameters)
        {
            if (Logger.LogLevel > logLevel)
                return;
            LogMessage(message, logLevelString, parameters);
        }

        private static void LogMessage(string message, string logLevelString, object[] parameters)
        {
            string formattedMessage = String.Format(message, parameters);
            // write to Debug by new line '\n'
            foreach (string formattedLine in formattedMessage.Split(new char[] { '\n' }))
            {
                System.Diagnostics.Debug.WriteLine("\t[{0}]{1} {2}", LogTag, logLevelString, formattedLine);
            }
        }
    }
}