using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    public enum AILogLevel
    {
        AILogLevelVerbose = 1,
        AILogLevelDebug,
        AILogLevelInfo,
        AILogLevelWarn,
        AILogLevelError,
        AILogLevelAssert,
    };

    public static class AILogger
    {
        public static string LogTag { get; set; }
        public static AILogLevel LogLevel{ get; set; }

        static AILogger()
        {
            LogTag = "AdjustIO";
            LogLevel = AILogLevel.AILogLevelInfo;
        }

        public static void Verbose(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelVerbose, message, parameters);
        }

        public static void Debug(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelDebug, message, parameters);
        }

        public static void Info(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelInfo, message, parameters);
        }

        public static void Warn(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelWarn, message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelError, message, parameters);
        }

        public static void Assert(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelAssert, message, parameters);
        }

        private static void LoggingLevel(AILogLevel logLevel, string message, object[] parameters)
        {
            if (AILogger.LogLevel > logLevel)
                return;
            LogMessage(message, parameters);
        }

        private static void LogMessage(string message, object[] parameters)
        {
            string formattedMessage = String.Format(message, parameters);
            //write to Debug by new line '\n'
            foreach (string formattedLine in formattedMessage.Split(new char[] {'\n'}))
            {
                System.Diagnostics.Debug.WriteLine("\t[{0}]{1} {2}", LogTag, LogLevel, formattedLine);
            }
        }
    }
}
