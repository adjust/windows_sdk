using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdjustSdk.Pcl.FileSystem;

namespace AdjustSdk.Pcl
{
    public interface IDeviceUtil
    {
        DeviceInfo GetDeviceInfo();

        Task RunActionInForeground(Action action, Task previousTask = null);

        void Sleep(int milliseconds);

        Task LauchDeeplink(Uri deeplinkUri, Task previousTask = null);

        string ReadWindowsAdvertisingId();

        void PersistObject(string key, Dictionary<string, object> objectValuesMap);

        bool PersistValue(string key, string value);

        bool TryTakeObject(string key, out Dictionary<string, object> objectValuesMap);

        bool TryTakeValue(string key, out string value);

        Task<IFile> GetLegacyStorageFile(string fileName);
    }
}