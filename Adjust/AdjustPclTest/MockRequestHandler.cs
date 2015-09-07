using AdjustSdk;
using AdjustSdk.Pcl;
using System;

namespace AdjustTest.Pcl
{
    public class MockRequestHandler : IRequestHandler
    {
        private MockLogger MockLogger;

        private const string prefix = "RequestHandler";

        public MockRequestHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public void Init(IPackageHandler packageHandler)
        {
            MockLogger.Test("{0} Init", prefix);
        }

        public void SendPackage(ActivityPackage package)
        {
            MockLogger.Test("{0} SendPackage, {1}", prefix, package);
        }
    }
}