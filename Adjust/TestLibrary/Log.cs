using System;
using System.Linq;
using static TestLibrary.Constants;

namespace TestLibrary
{
    public class Log
    {
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
                System.Diagnostics.Debug.WriteLine("[{0}][{1}]" + LOGTAG + "[{2}]: " + string.Format(message, parameters),
                    GetTimeNow(), logLevel, location);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[{0}] Error formating log message: {1}, with params: {2}",
                    GetTimeNow(), message, string.Join(",", parameters.Select(p => p.ToString())));
                System.Diagnostics.Debug.WriteLine(e);
            }
        }

        private static string GetTimeNow()
        {
            var n = DateTime.Now;
            return $"{n.Hour}:{n.Minute}:{n.Second}::{n.Millisecond}";
        }
    }
}
