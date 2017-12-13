using System;

namespace AdjustSdk.Pcl
{
    public interface ILogger
    {
        LogLevel LogLevel { get; set; }

        bool IsProductionEnvironment { get; set; }

        Action<string> LogDelegate { set; }

        bool IsLocked { get; set; }

        void Verbose(string message, params object[] parameters);

        void Debug(string message, params object[] parameters);

        void Info(string message, params object[] parameters);

        void Warn(string message, params object[] parameters);

        void WarnInProduction(string message, params object[] parameters);

        void Error(string message, params object[] parameters);

        void Assert(string message, params object[] parameters);        
    }
}