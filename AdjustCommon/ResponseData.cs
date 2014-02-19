namespace adeven.Adjust.Common
{
    public class ResponseData
    {
        public string Error;
        public string TrackerToken;
        public string TrackerName;

        public bool WillRetry { get; set; }

        public bool Success { get; set; }
    }
}