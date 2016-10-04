namespace AdjustSdk.Pcl
{
    public class SessionResponseData : ResponseData
    {
        public AdjustSessionSuccess GetSessionSuccess()
        {
            if (!Success) { return null; }

            AdjustSessionSuccess successResponseData = new AdjustSessionSuccess();
            successResponseData.Message = Message;
            successResponseData.Timestamp = Timestamp;
            successResponseData.Adid = Adid;
            successResponseData.JsonResponse = JsonResponse;

            return successResponseData;
        }

        public AdjustSessionFailure GetFailureResponseData()
        {
            if (Success) { return null; }

            AdjustSessionFailure failureResponseData = new AdjustSessionFailure();
            failureResponseData.Message = Message;
            failureResponseData.Timestamp = Timestamp;
            failureResponseData.Adid = Adid;
            failureResponseData.WillRetry = WillRetry;
            failureResponseData.JsonResponse = JsonResponse;

            return failureResponseData;
        }
    }
}
