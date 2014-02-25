using AdjustSdk;
using System;

namespace AdjustSdk.Pcl
{
    public class Logger : ILogger
    {
        private const string LogTag = "Adjust";

        public LogLevel LogLevel { private get; set; }

        internal Logger()
        {
            LogLevel = LogLevel.Info;
        }

        public void Verbose(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Verbose, "v", message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Debug, "d", message, parameters);
        }

        public void Info(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Info, "i", message, parameters);
        }

        public void Warn(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Warn, "w", message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Error, "e", message, parameters);
        }

        public void Assert(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Assert, "a", message, parameters);
        }

        private void LoggingLevel(LogLevel logLevel, string logLevelString, string message, object[] parameters)
        {
            if (LogLevel > logLevel)
                return;
            LogMessage(message, logLevelString, parameters);
        }

        private void LogMessage(string message, string logLevelString, object[] parameters)
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