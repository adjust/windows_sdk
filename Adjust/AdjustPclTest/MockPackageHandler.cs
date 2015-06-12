using AdjustSdk;
using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockPackageHandler : IPackageHandler
    {
        private MockLogger MockLogger;
        private const string prefix = "PackageHandler";

        public IList<ActivityPackage> PackageQueue { get; set; }

        public MockPackageHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
            PackageQueue = new List<ActivityPackage>();
        }

        public void Init(IActivityHandler activityHandler, bool startPaused)
        {
            MockLogger.Test("{0} Init, startPaused: {1}", prefix, startPaused);
        }
        
        public void AddPackage(ActivityPackage activityPackage)
        {
            MockLogger.Test("{0} AddPackage", prefix);
            PackageQueue.Add(activityPackage);
        }

        public void SendFirstPackage()
        {
            MockLogger.Test("{0} SendFirstPackage", prefix);
        }

        public void SendNextPackage()
        {
            MockLogger.Test("{0} SendNextPackage", prefix);
        }

        public void CloseFirstPackage()
        {
            MockLogger.Test("{0} CloseFirstPackage", prefix);
        }

        public void PauseSending()
        {
            MockLogger.Test("{0} PauseSending", prefix);
        }

        public void ResumeSending()
        {
            MockLogger.Test("{0} ResumeSending", prefix);
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            MockLogger.Test("{0} FinishedTrackingActivity {1}", prefix, jsonDict);
        }
    }
}