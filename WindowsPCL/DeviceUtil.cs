using System;
using System.IO;

namespace adeven.AdjustIo.PCL
{
    public interface DeviceUtil
    {
        string EnvironmentSandbox { get; }

        string EnvironmentProduction { get; }

        string ClientSdk { get; }

        string GetDeviceId();

        string GetUserAgent();

        string GetMd5Hash(string input);

        T DeserializeFromFile<T>(string fileName, Func<Stream, T> ObjectReader, Func<T> defaultReturn)
            where T : class;

        void SerializeToFile<T>(string fileName, Action<Stream, T> ObjectWriter, T input)
            where T : class;
    }
}