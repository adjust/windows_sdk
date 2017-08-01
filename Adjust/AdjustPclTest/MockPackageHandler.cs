using AdjustSdk;
using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class MockPackageHandler : IPackageHandler
    {
        private readonly MockLogger _mockLogger;
        private const string Prefix = "PackageHandler";

        public IList<ActivityPackage> PackageQueue { get; set; }

        public MockPackageHandler(MockLogger mockLogger)
        {
            _mockLogger = mockLogger;
            PackageQueue = new List<ActivityPackage>();
        }
        
        public void Init(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused)
        {
            _mockLogger.Test("{0} Init, startPaused: {1}, deviceUtil: {2}", Prefix, startPaused, deviceUtil);
        }

        public void AddPackage(ActivityPackage activityPackage)
        {
            _mockLogger.Test("{0} AddPackage", Prefix);
            PackageQueue.Add(activityPackage);
        }

        public void SendFirstPackage()
        {
            _mockLogger.Test("{0} SendFirstPackage", Prefix);
        }
        
        public void CloseFirstPackage(ResponseData responseData, ActivityPackage activityPackage)
        {
            _mockLogger.Test("{0} CloseFirstPackage", Prefix);
        }

        public void SendNextPackage(ResponseData responseData)
        {
            _mockLogger.Test("{0} SendNextPackage, responseData: {1}", Prefix, responseData);
        }
        
        public void PauseSending()
        {
            _mockLogger.Test("{0} PauseSending", Prefix);
        }

        public void ResumeSending()
        {
            _mockLogger.Test("{0} ResumeSending", Prefix);
        }

        public void UpdatePackages(SessionParameters sessionParameters)
        {
            _mockLogger.Test("{0} UpdatePackages, sessionParameters: ", Prefix, sessionParameters);
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            _mockLogger.Test("{0} FinishedTrackingActivity, {1}", Prefix, string.Join(";", jsonDict));
        }
    }
}