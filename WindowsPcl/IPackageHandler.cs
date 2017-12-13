namespace AdjustSdk.Pcl
{
    public interface IPackageHandler
    {
        void Init(IActivityHandler activityHandler, IDeviceUtil deviceUtil, bool startPaused);

        void AddPackage(ActivityPackage activityPackage);

        void SendFirstPackage();

        void SendNextPackage(ResponseData responseData);

        void CloseFirstPackage(ResponseData responseData, ActivityPackage activityPackage);

        void PauseSending();

        void ResumeSending();

        void UpdatePackages(SessionParameters sessionParameters);
    }
}