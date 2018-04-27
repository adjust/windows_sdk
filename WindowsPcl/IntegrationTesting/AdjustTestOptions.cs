namespace AdjustSdk.Pcl.IntegrationTesting
{
    public class AdjustTestOptions
    {
        public string BaseUrl { get; set; }
        public string GdprUrl { get; set; }
        public string BasePath { get; set; }
        public string GdprPath { get; set; }
        public bool? Teardown { get; set; }
        public bool? DeleteState { get; set; }

        // default value => Constants.ONE_MINUTE;
        public long? TimerIntervalInMilliseconds { get; set; }
        // default value => Constants.ONE_MINUTE;
        public long? TimerStartInMilliseconds { get; set; }
        // default value => Constants.THIRTY_MINUTES;
        public long? SessionIntervalInMilliseconds { get; set; }
        // default value => Constants.ONE_SECOND;
        public long? SubsessionIntervalInMilliseconds { get; set; }
    }
}
