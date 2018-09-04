namespace AdjustSdk.Pcl
{
    public class EventResponseData : ResponseData
    {
        internal string EventToken { get; set; }
        internal string CallbackId { get; set; }

        public EventResponseData(ActivityPackage activityPackage)
        {
            string eventToken;
            if (activityPackage.Parameters.TryGetValue(Constants.EVENT_TOKEN, out eventToken))
            {
                EventToken = eventToken;
            }

            string eventCallbackId;
            if (activityPackage.Parameters.TryGetValue(Constants.EVENT_CALLBACK_ID, out eventCallbackId))
            {
                CallbackId = eventCallbackId;
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
            successResponseData.EventToken = EventToken;
            successResponseData.CallbackId = CallbackId;
            successResponseData.JsonResponse = JsonResponse;

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
            failureResponseData.EventToken = EventToken;
            failureResponseData.CallbackId = CallbackId;
            failureResponseData.JsonResponse = JsonResponse;

            return failureResponseData;
        }
    }
}
