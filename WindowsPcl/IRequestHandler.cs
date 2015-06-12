namespace AdjustSdk.Pcl
{
    public interface IRequestHandler
    {
        void Init(IPackageHandler packageHandler);

        void SendPackage(ActivityPackage package);
    }
}