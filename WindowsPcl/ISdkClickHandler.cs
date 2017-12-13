namespace AdjustSdk.Pcl
{
    public interface ISdkClickHandler
    {
        void Init(IActivityHandler activityHandler, bool startPaused);

        void PauseSending();

        void ResumeSending();

        void SendSdkClick(ActivityPackage sdkClickPackage);
    }
}
