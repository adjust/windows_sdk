using AdjustSdk;
using AdjustSdk.Pcl;
using System;

namespace AdjustTest.Pcl
{
    public class MockRequestHandler : IRequestHandler
    {
        private readonly MockLogger _mockLogger;

        private const string Prefix = "RequestHandler";

        public MockRequestHandler(MockLogger mockLogger)
        {
            _mockLogger = mockLogger;
        }
        
        public void Init(Action<ResponseData> sendNextCallback, Action<ResponseData, ActivityPackage> retryCallback)
        {
            _mockLogger.Test("{0} Init", Prefix);
        }

        public void SendPackage(ActivityPackage package)
        {
            _mockLogger.Test("{0} SendPackage, {1}", Prefix, package);
        }

        public void SendPackageSync(ActivityPackage activityPackage)
        {
            _mockLogger.Test("{0} SendPackage, {1}", Prefix, activityPackage);
        }
    }
}