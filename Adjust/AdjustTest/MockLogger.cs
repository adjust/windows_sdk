using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl.Test
{
    internal class MockLogger : ILogger
    {
        private const int LogLevelTest = 7;
        private const string LogTag = "Adjust";

        private StringBuilder LogBuffer;
        private Dictionary<int, List<string>> LogMap;

        internal MockLogger()
        {
            LogBuffer = new StringBuilder();
            LogMap = new Dictionary<int, List<string>>(7)
            {
                { (int)LogLevel.Verbose, new List<string>() },
                { (int)LogLevel.Debug, new List<string>() },
                { (int)LogLevel.Info, new List<string>() },
                { (int)LogLevel.Warn, new List<string>() },
                { (int)LogLevel.Error, new List<string>() },
                { (int)LogLevel.Assert, new List<string>() },
                { LogLevelTest, new List<string>() },
            };
        }

        public LogLevel LogLevel
        {
            set { Test("Logger setLogLevel: {0}", value); }
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

        public void Test(string message, params object[] parameters)
        {
            LogMessage(message, LogLevelTest, "t", parameters);
        }

        public bool DeleteLogUntil(LogLevel loglevel, string beginsWith)
        {
            return DeleteLevelUntil((int)loglevel, beginsWith);
        }

        public bool DeleteTestUntil(string beginsWith)
        {
            return DeleteLevelUntil(LogLevelTest, beginsWith);
        }

        private bool DeleteLevelUntil(int logLevel, string beginsWith)
        {
            var logList = LogMap[logLevel];
            for (int i = 0; i < logList.Count; i++)
            {
                var logMessage = logList[i];
                if (logMessage.StartsWith(beginsWith))
                {
                    Test("{0} found", logMessage);
                    logList.RemoveRange(0, i + 1);
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return LogBuffer.ToString();
        }

        private void LoggingLevel(LogLevel logLevel, string message, object[] parameters)
        {
            LogMessage(message, (int)logLevel, logLevel.ToString().Substring(0, 1).ToLower(), parameters);
        }

        private void LogMessage(string message, int logLevelInt, string logLevelString, object[] parameters)
        {
            var formattedMessage = String.Format(message, parameters);

            // write to Debug by new line '\n'
            foreach (string line in formattedMessage.Split(new char[] { '\n' }))
            {
                var formattedLine = String.Format("\t[{0}]{1} {2}", LogTag, logLevelString, line);

                System.Diagnostics.Debug.WriteLine(formattedLine);
                LogBuffer.AppendLine(formattedLine);
            }

            var logList = LogMap[logLevelInt];
            logList.Add(formattedMessage);
        }
    }
}