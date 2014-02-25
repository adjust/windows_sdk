using System;

namespace AdjustSdk.Pcl
{
    public interface ILogger
    {
        void Assert(string message, params object[] parameters);

        void Debug(string message, params object[] parameters);

        void Error(string message, params object[] parameters);

        void Info(string message, params object[] parameters);

        AdjustSdk.LogLevel LogLevel { set; }

        void Verbose(string message, params object[] parameters);

        void Warn(string message, params object[] parameters);
    }
}