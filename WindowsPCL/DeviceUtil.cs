using System;
using System.IO;
using System.Threading.Tasks;

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

        // TODO delete if PCL Storage works in WP & WS
        //Task<T> DeserializeFromFileAsync<T>(string fileName, Func<Stream, T> ObjectReader, Func<T> defaultReturn)
        //    where T : class;

        //Task SerializeToFileAsync<T>(string fileName, Action<Stream, T> ObjectWriter, T input)
        //    where T : class;
    }
}