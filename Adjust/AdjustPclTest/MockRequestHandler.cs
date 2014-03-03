using AdjustSdk;
using AdjustSdk.Pcl;
using System;

namespace AdjustTest.Pcl
{
    public class MockRequestHandler : IRequestHandler
    {
        private MockLogger MockLogger;
        private IPackageHandler PackageHandler;

        private const string prefix = "RequestHandler";

        public MockRequestHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        public void SendPackage(ActivityPackage package)
        {
            MockLogger.Test("{0} SendPackage", prefix);

            if (PackageHandler != null)
                PackageHandler.SendNextPackage();
        }

        public void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            MockLogger.Test("{0} SetResponseDelegate", prefix);
        }

        public void SetPackageHandler(IPackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
        }
    }
}