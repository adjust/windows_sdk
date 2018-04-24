using AdjustSdk.Pcl;
using System.Collections.Generic;
using static AdjustSdk.Pcl.Constants;

namespace AdjustSdk
{
    public enum ActivityKind
    {
        Unknown = 0,
        Session,
        Event,
        Click,
        Attribution,
        Info,
        Gdpr
    }

    public static class ActivityKindUtil
    {
        public static ActivityKind FromString(string activityKindString)
        {
            if (activityKindString == SESSION)
                return ActivityKind.Session;
            if (activityKindString == EVENT)
                return ActivityKind.Event;
            if (activityKindString == CLICK)
                return ActivityKind.Click;
            if (activityKindString == ATTRIBUTION)
                return ActivityKind.Attribution;
            if (activityKindString == INFO)
                return ActivityKind.Info;
            if (activityKindString == GDPR)
                return ActivityKind.Gdpr;
            return ActivityKind.Unknown;
        }

        public static string ToString(ActivityKind activityKind)
        {
            return activityKind.ToString().ToLower();
        }

        public static string GetPath(ActivityKind activityKind)
        {
            switch (activityKind)
            {
                case ActivityKind.Session: return SESSION_PATH;
                case ActivityKind.Event: return EVENT_PATH;
                case ActivityKind.Click: return SDK_CLICK_PATH;
                case ActivityKind.Attribution: return ATTRIBUTION_PATH;
                case ActivityKind.Info: return SDK_INFO_PATH;
                case ActivityKind.Gdpr: return GDPR_PATH;
                default: return null;
            }
        }

        public static string GetSuffix(Dictionary<string, string> parameters)
        {
            string eventToken = null;

            parameters?.TryGetValue(EVENT_TOKEN, out eventToken);

            if (eventToken == null) { return ""; }

            string sRevenue;

            if (!parameters.TryGetValue(REVENUE, out sRevenue))
            {
                return Util.F("'{0}'", eventToken);
            }

            return Util.F("({0} {1}, '{2}')", sRevenue, parameters[CURRENCY], eventToken);
        }
    }
}