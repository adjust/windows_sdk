namespace AdjustSdk.Pcl
{
    public interface IAttributionHandler
    {
        void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused);

        void CheckSessionResponse(SessionResponseData sessionResponseData);

        void CheckSdkClickResponse(SdkClickResponseData sdkClickResponseData);

        void GetAttribution();

        void PauseSending();

        void ResumeSending();

        void Teardown();
    }
}