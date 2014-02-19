namespace AdjustSdk
{
    public class ResponseData
    {
        #region Set by SDK

        // the kind of activity (ActivityKind.Session etc.)
        public ActivityKind Kind { get; set; }

        // true when the activity was tracked successfully
        // might be true even if response could not be parsed
        public bool Success { get; set; }

        // true if the server was not reachable and the request will be tried again later
        public bool WillRetry { get; set; }

        #endregion Set by SDK

        #region Set by server or SDK

        // nil if activity was tracked successfully and response could be parsed
        // might be not nil even when activity was tracked successfully
        public string Error;

        #endregion Set by server or SDK

        #region Set by server

        // the following attributes are only set when error is nil
        // (when activity was tracked successfully and response could be parsed)

        // tracker token of current device
        public string TrackerToken;

        // tracker name of current device
        public string TrackerName;

        #endregion Set by server

        // returns human readable version of activityKind
        // (session, event, revenue), see above
        public string ActivityKindString { get; set; }
    }
}