using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    static class AILogger
    {
        private static enum AILogLevel
        {
            AILogLevelVerbose = 1,
            AILogLevelDebug,
            AILogLevelInfo,
            AILogLevelWarn,
            AILogLevelError,
            AILogLevelAssert,
        };

        internal static string LogTag { get; set; }
        internal static AILogLevel LogLevel{ get; set; }

        internal static AILogger()
        {
            LogTag = "AdjustIO";
            LogLevel = AILogLevel.AILogLevelInfo;
        }

        internal static void Verbose(string message, params object[] parameters)
        {
            if (AILogger.LogLevel > AILogLevel.AILogLevelVerbose) 
                return;
            LogMessage(message, parameters);
        }

        private static void LogMessage(string message, object[] parameters)
        {
            string formattedMessage = String.Format(message, parameters);
            //write to Debug by new line '\n'
            foreach (string formattedLine in formattedMessage.Split(new char[] {'\n'}))
            {
                Debug.WriteLine("\t[{0}]{1} {2}", LogTag, LogLevel, formattedLine);
            }
        }
    }
}
