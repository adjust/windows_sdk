namespace AdjustSdk.Pcl
{
    public class EventResponseData : ResponseData
    {
        internal string EventToken { get; set; }

        public EventResponseData(ActivityPackage activityPackage)
        {
            string eventToken;
            if (activityPackage.Parameters.TryGetValue("event_token", out eventToken))
            {
                EventToken = eventToken;
            }
        }

    }
}
