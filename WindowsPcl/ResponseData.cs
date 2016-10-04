using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public abstract class ResponseData
    {
        internal ActivityKind ActivityKind { get; set; }
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
                    responseData = new ClickResponseData();
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
            return Util.f("message:{0} timestamp:{1} json:{2}", Message, Timestamp, JsonResponse);
        }
    }
}
