using System;

namespace AdjustSdk.Pcl
{
    public class Logger : ILogger
    {
        private const string LogTag = "Adjust";
        private LogLevel _logLevel;
        public bool IsProductionEnvironment { get; set; }
        public bool IsLocked { get; set; }

        internal Logger()
        {
            LogLevel = LogLevel.Info;
            LogDelegate = null;
            IsProductionEnvironment = false;
        }

        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set
            {
                if (IsLocked) return;
                _logLevel = value;
            }
        }

        public Action<string> LogDelegate { private get; set; }
        
        public void Verbose(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Verbose, message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Debug, message, parameters);
        }

        public void Info(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Info, message, parameters);
        }

        public void Warn(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Warn, message, parameters);
        }

        public void WarnInProduction(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Warn, message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Error, message, parameters);
        }

        public void Assert(string message, params object[] parameters)
        {
            if (IsProductionEnvironment)
                return;

            LoggingLevel(LogLevel.Assert, message, parameters);
        }

        private void LoggingLevel(LogLevel logLevel, string message, object[] parameters)
        {
            if (LogLevel > logLevel)
                return;

            if (LogDelegate == null)
                return;

            var logLevelString = logLevel.ToString().Substring(0, 1).ToLower();

            LogMessage(message, logLevelString, parameters);
        }

        private void LogMessage(string message, string logLevelString, object[] parameters)
        {
            var formattedMessage = Util.F(message, parameters);
            // write to Debug by new line '\n'
            foreach (var formattedLine in formattedMessage.Split('\n'))
            {
                var logMessage = string.Format("\t[{0}]{1} {2}", LogTag, logLevelString, formattedLine);
                LogDelegate(logMessage);
            }
        }
    }
}