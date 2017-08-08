using System;
using System.Linq;

namespace TestApp
{
    internal class Log
    {
        public static void Debug(string message, params object[] parameters)
        {
            WriteToOutput("Debug", message, parameters);
        }

        public static void Error(string message, params object[] parameters)
        {
            WriteToOutput("!Error", message, parameters);
        }

        private static void WriteToOutput(string logLevel, string message, params object[] parameters)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[{0}] TEST-APP" + ": " + string.Format(message, parameters),
                    logLevel);
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