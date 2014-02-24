namespace AdjustSdk
{
    public enum ActivityKind
    {
        Unkown = 0,
        Session,
        Event,
        Revenue,
    }

    public static class ActivityKindUtil
    {
        internal static ActivityKind FromString(string activityKindString)
        {
            if (activityKindString == "session")
                return ActivityKind.Session;
            else if (activityKindString == "event")
                return ActivityKind.Event;
            else if (activityKindString == "revenue")
                return ActivityKind.Revenue;
            else
                return ActivityKind.Unkown;
        }

        internal static string ToString(ActivityKind activityKind)
        {
            return activityKind.ToString().ToLower();
        }
    }
}