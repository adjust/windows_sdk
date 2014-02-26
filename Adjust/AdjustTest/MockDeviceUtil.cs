using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Test
{
    internal class MockDeviceUtil : DeviceUtil
    {
        private MockLogger MockLogger;
        private const string prefix = "MockDeviceUtil";

        internal MockDeviceUtil(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public string ClientSdk
        {
            get
            {
                var mockString = String.Format("{0} ClientSdk", prefix);
                MockLogger.Test(mockString);

                return mockString;
            }
        }

        public string GetUserAgent()
        {
            var mockString = String.Format("{0} GetUserAgent", prefix);
            MockLogger.Test(mockString);

            return mockString;
        }

        public string GetMd5Hash(string input)
        {
            var mockString = String.Format("{0} GetMd5Hash", prefix);
            MockLogger.Test(mockString);

            return mockString;
        }

        public string GetDeviceUniqueId()
        {
            var mockString = String.Format("{0} GetDeviceUniqueId", prefix);
            MockLogger.Test(mockString);

            return mockString;
        }

        public string GetHardwareId()
        {
            var mockString = String.Format("{0} GetHardwareId", prefix);
            MockLogger.Test(mockString);

            return mockString;
        }

        public string GetNetworkAdapterId()
        {
            var mockString = String.Format("{0} GetNetworkAdapterId", prefix);
            MockLogger.Test(mockString);

            return mockString;
        }

        public void RunResponseDelegate(Action<ResponseData> responseDelegate, ResponseData responseData)
        {
            MockLogger.Test("{0} RunResponseDelegate, ResponseData: {1}", prefix, responseData);
        }
    }
}