namespace AdjustSdk.Pcl
{
    public class EventResponseData : ResponseData
    {
        internal string EventToken { get; set; }

        public EventResponseData(ActivityPackage activityPackage)
        {
            string eventToken;
            if (activityPackage.Parameters.TryGetValue(Constants.EVENT_TOKEN, out eventToken))
            {
                EventToken = eventToken;
            }
        }

        public AdjustEventSuccess GetSuccessResponseData()
        {
            if (!Success)
            {
                return null;
            }

            AdjustEventSuccess successResponseData = new AdjustEventSuccess();
            successResponseData.Message = Message;
            successResponseData.Timestamp = Timestamp;
            successResponseData.Adid = Adid;
            successResponseData.JsonResponse = JsonResponse;
            successResponseData.EventToken = EventToken;

            return successResponseData;
        }

        public AdjustEventFailure GetFailureResponseData()
        {
            if (Success)
            {
                return null;
            }

            AdjustEventFailure failureResponseData = new AdjustEventFailure();
            failureResponseData.Message = Message;
            failureResponseData.Timestamp = Timestamp;
            failureResponseData.Adid = Adid;
            failureResponseData.WillRetry = WillRetry;
            failureResponseData.JsonResponse = JsonResponse;
            failureResponseData.EventToken = EventToken;

            return failureResponseData;
        }
    }
}
