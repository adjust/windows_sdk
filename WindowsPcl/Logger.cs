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
            LoggingLevel(LogLevel.Verbose, message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Debug, message, parameters);
        }

        public void Info(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Info, message, parameters);
        }

        public void Warn(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Warn, message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Error, message, parameters);
        }

        public void Assert(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Assert, message, parameters);
        }

        private void LoggingLevel(LogLevel logLevel, string message, object[] parameters)
        {
            if (LogLevel > logLevel)
                return;

            var logLevelString = logLevel.ToString().Substring(0, 1).ToLower();

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