using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl.Test
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