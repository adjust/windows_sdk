using AdjustSdk.Pcl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AdjustSdk
{
    public class ResponseData
    {
        #region Set by SDK

        // the kind of activity (ActivityKind.Session etc.)
        public ActivityKind ActivityKind { get; set; }

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
        public string ActivityKindString { get { return ActivityKindUtil.ToString(ActivityKind); } }

        #region internal

        public void SetResponseData(string responseString)
        {
            Dictionary<string, string> responseDic = null;
            try
            {
                responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (Exception)
            { }

            if (responseDic == null)
            {
                Error = Util.f("Failed to parse json response: {0}", responseString);
                return;
            }

            responseDic.TryGetValue("error", out Error);
            responseDic.TryGetValue("tracker_token", out TrackerToken);
            responseDic.TryGetValue("tracker_name", out TrackerName);
        }

        public void SetResponseError(string errorString)
        {
            Error = errorString;
            Success = false;
        }

        #endregion internal

        public override string ToString()
        {
            return Util.f("[kind: {0} success:{1} willRetry:{2} error:{3} trackerToken:{4} trackerName:{5}]",
                ActivityKindString,
                Success,
                WillRetry,
                Error.Quote(),
                TrackerToken,
                TrackerName.Quote());
        }
    }
}