﻿namespace AdjustSdk
{
    public enum ActivityKind
    {
        Unkown = 0,
        Session,
        Event,
        Revenue,
        Reattribution,
    }

    public static class ActivityKindUtil
    {
        public static ActivityKind FromString(string activityKindString)
        {
            if (activityKindString == "session")
                return ActivityKind.Session;
            else if (activityKindString == "event")
                return ActivityKind.Event;
            else if (activityKindString == "revenue")
                return ActivityKind.Revenue;
            else if (activityKindString == "reattribution")
                return ActivityKind.Reattribution;
            else
                return ActivityKind.Unkown;
        }

        public static string ToString(ActivityKind activityKind)
        {
            return activityKind.ToString().ToLower();
        }
    }
}