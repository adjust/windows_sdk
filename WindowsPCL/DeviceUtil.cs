using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    public abstract class DeviceUtil
    {
        public abstract string AIEnvironmentSandbox { get; }
        public abstract string AIEnvironmentProduction { get; }

        public abstract string ClientSdk { get; }

        public abstract string GetDeviceId();
        public abstract string GetUserAgent();
        public abstract string GetMd5Hash(string input);
    }
}
