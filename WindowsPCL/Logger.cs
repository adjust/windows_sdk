using System;

namespace adeven.AdjustIo.PCL
{
    public enum LogLevel
    {
        Verbose = 1,
        Debug,
        Info,
        Warn,
        Error,
        Assert,
    };

    public static class Logger
    {
        public static string LogTag { get; set; }

        public static LogLevel LogLevel { get; set; }

        static Logger()
        {
            LogTag = "AdjustIO";
            LogLevel = LogLevel.Info;
        }

        public static void Verbose(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Verbose, message, parameters);
        }

        public static void Debug(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Debug, message, parameters);
        }

        public static void Info(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Info, message, parameters);
        }

        public static void Warn(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Warn, message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Error, message, parameters);
        }

        public static void Assert(string message, params object[] parameters)
        {
            LoggingLevel(LogLevel.Assert, message, parameters);
        }

        private static void LoggingLevel(LogLevel logLevel, string message, object[] parameters)
        {
            if (Logger.LogLevel > logLevel)
                return;
            LogMessage(message, parameters);
        }

        private static void LogMessage(string message, object[] parameters)
        {
            string formattedMessage = String.Format(message, parameters);
            // write to Debug by new line '\n'
            foreach (string formattedLine in formattedMessage.Split(new char[] { '\n' }))
            {
                System.Diagnostics.Debug.WriteLine("\t[{0}]{1} {2}", LogTag, LogLevel, formattedLine);
            }
        }
    }
}