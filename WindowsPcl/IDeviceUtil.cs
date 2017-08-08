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

        bool ClearSimpleValue(string key);

        void ClearAllPeristedValues();

        void ClearAllPersistedObjects();

        void PersistObject(string key, Dictionary<string, object> objectValuesMap);

        bool PersistValue(string key, string value);

        void PersistSimpleValue(string key, string value);

        bool TryTakeObject(string key, out Dictionary<string, object> objectValuesMap);

        bool TryTakeValue(string key, out string value);

        bool TryTakeSimpleValue(string key, out string value);

        Task<IFile> GetLegacyStorageFile(string fileName);

        string HashStringUsingSha256(string stringValue);

        string HashStringUsingSha512(string stringValue);

        string HashStringUsingShaMd5(string stringValue);

        void SetInstallTracked();

        bool IsInstallTracked();
    }
}