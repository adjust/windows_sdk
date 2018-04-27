using System;
using System.Linq;
using static TestLibrary.Constants;

namespace TestLibrary
{
    public class Log
    {
        private static Action<string> _logDelegate;

        public static void InjectLogDelegate(Action<string> logDelegate)
        {
            _logDelegate = logDelegate;
        }

        public static void Debug(string location, string message, params object[] parameters)
        {
            WriteToOutput(location, "Debug", message, parameters);
        }
        
        public static void Error(string location, string message, params object[] parameters)
        {
            WriteToOutput(location, "!Error", message, parameters);
        }
        
        private static void WriteToOutput(string location, string logLevel, string message, params object[] parameters)
        {
            try
            {
                string logInfo = string.Format("[{0}][{1}]" + LOGTAG + "[{2}]: ", GetTimeNow(), logLevel, location);
                string logOutput = logInfo + string.Format(message, parameters);
                if (_logDelegate == null)
                {
                    System.Diagnostics.Debug.WriteLine(logOutput);
                }
                else
                {
                    _logDelegate(logOutput);
                }
            }
            catch (Exception e)
            {
                string errorOutput = string.Format("[{0}] Error formating log message: {1}, with params: {2}",
                    GetTimeNow(), message, string.Join(",", parameters.Select(p => p.ToString())));
                System.Diagnostics.Debug.WriteLine(errorOutput);
                System.Diagnostics.Debug.WriteLine(e);
                if (_logDelegate == null)
                {
                    _logDelegate(errorOutput);
                }
            }
        }

        private static string GetTimeNow()
        {
            var n = DateTime.Now;
            return $"{n.Hour}:{n.Minute}:{n.Second}::{n.Millisecond}";
        }
    }
}
