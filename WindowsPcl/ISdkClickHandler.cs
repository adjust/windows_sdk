namespace AdjustSdk.Pcl
{
    public interface ISdkClickHandler
    {
        void Init(bool startPaused);

        void PauseSending();

        void ResumeSending();

        void SendSdkClick(ActivityPackage sdkClickPackage);
    }
}
