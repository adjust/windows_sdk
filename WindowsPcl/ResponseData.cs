using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public abstract class ResponseData
    {
        internal ActivityKind ActivityKind { get; set; }
        internal TrackingState? TrackingState { get; set; } = null;
        internal bool WillRetry { get; set; }
        internal Dictionary<string, string> JsonResponse { get; set; }
        internal string Message { get; set; }
        internal string Timestamp { get; set; }
        internal string Adid { get; set; }
        internal bool Success { get; set; }
        internal int? StatusCode { get; set; }
        internal Exception Exception { get; set; }
        internal AdjustAttribution Attribution { get; set; }

        public static ResponseData BuildResponseData(ActivityPackage activityPackage)
        {
            ActivityKind activityKind = activityPackage.ActivityKind;
            ResponseData responseData;
            switch(activityKind)
            {
                case ActivityKind.Session:
                    responseData = new SessionResponseData();
                    break;
                case ActivityKind.Attribution:
                    responseData = new AttributionResponseData();
                    break;
                case ActivityKind.Event:
                    responseData = new EventResponseData(activityPackage);
                    break;
                case ActivityKind.Click:
                    responseData = new SdkClickResponseData();
                    break;
                default:
                    responseData = new UnknowResponseData();
                    break;
            }

            responseData.ActivityKind = activityKind;

            return responseData;
        }

        public override string ToString()
        {
            return Util.F("message:{0} timestamp:{1} adid:{2} success:{3} willRetry:{4} attribution:{5} trackingState:{6} json:{7}",
                Message, Timestamp, Adid, Success, WillRetry, Attribution, TrackingState, JsonResponse);
        }
    }
}
