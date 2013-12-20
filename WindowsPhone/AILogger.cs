using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
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

    static class AILogger
    {
        internal static string LogTag { get; set; }
        internal static AILogLevel LogLevel{ get; set; }

        static AILogger()
        {
            LogTag = "AdjustIO";
            LogLevel = AILogLevel.AILogLevelInfo;
        }

        internal static void Verbose(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelVerbose, message, parameters);
        }

        internal static void Debug(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelDebug, message, parameters);
        }

        internal static void Info(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelInfo, message, parameters);
        }

        internal static void Warn(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelWarn, message, parameters);
        }

        internal static void Error(string message, params object[] parameters)
        {
            LoggingLevel(AILogLevel.AILogLevelError, message, parameters);
        }

        internal static void Assert(string message, params object[] parameters)
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
