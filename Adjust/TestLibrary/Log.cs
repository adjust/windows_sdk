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
                System.Diagnostics.Debug.WriteLine("[{0}]" + LOGTAG + "[{1}]: " + string.Format(message, parameters), logLevel, location);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error formating log message: {0}, with params: {1}", 
                    message, string.Join(",", parameters.Select(p => p.ToString())));
                System.Diagnostics.Debug.WriteLine(e);
            }
        }
    }
}
