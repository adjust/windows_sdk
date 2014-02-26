using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Test
{
    internal class MockPackageHandler : IPackageHandler
    {
        private MockLogger MockLogger;
        private const string prefix = "PackageHandler";
        private IList<ActivityPackage> PackageQueue;

        public ActivityPackage LastFinishedPackage { get; private set; }

        public ResponseData LastResponseData { get; private set; }

        internal MockPackageHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
            PackageQueue = new List<ActivityPackage>();
        }

        public void AddPackage(ActivityPackage activityPackage)
        {
            MockLogger.Test("{0} AddPackage", prefix);
            PackageQueue.Add(activityPackage);
        }

        public void CloseFirstPackage()
        {
            MockLogger.Test("{0} CloseFirstPackage", prefix);
        }

        public void FinishedTrackingActivity(ActivityPackage activityPackage, ResponseData responseData)
        {
            MockLogger.Test("{0} FinishedTrackingActivity", prefix);

            LastFinishedPackage = activityPackage;
            LastResponseData = LastResponseData;
        }

        public void PauseSending()
        {
            MockLogger.Test("{0} PauseSending", prefix);
        }

        public void ResumeSending()
        {
            MockLogger.Test("{0} ResumeSending", prefix);
        }

        public void SendFirstPackage()
        {
            MockLogger.Test("{0} SendFirstPackage", prefix);
        }

        public void SendNextPackage()
        {
            MockLogger.Test("{0} SendNextPackage", prefix);
        }
    }
}