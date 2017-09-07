using AdjustSdk.Pcl;
using System.Collections.Generic;

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
    }

    public static class ActivityKindUtil
    {
        public static ActivityKind FromString(string activityKindString)
        {
            if ("session".Equals(activityKindString))
                return ActivityKind.Session;
            if ("event".Equals(activityKindString))
                return ActivityKind.Event;
            if ("click".Equals(activityKindString))
                return ActivityKind.Click;
            if ("attribution".Equals(activityKindString))
                return ActivityKind.Attribution;
            if ("info".Equals(activityKindString))
                return ActivityKind.Info;
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
                case ActivityKind.Session: return "/session";
                case ActivityKind.Event: return "/event";
                case ActivityKind.Click: return "/sdk_click";
                case ActivityKind.Attribution: return "/attribution";
                case ActivityKind.Info: return "/sdk_info";
                default: return null;
            }
        }

        public static string GetSuffix(Dictionary<string, string> parameters)
        {
            string eventToken = null;

            parameters?.TryGetValue("event_token", out eventToken);

            if (eventToken == null) { return ""; }

            string sRevenue;

            if (!parameters.TryGetValue("revenue", out sRevenue))
            {
                return Util.F("'{0}'", eventToken);
            }

            return Util.F("({0} {1}, '{2}')", sRevenue, parameters["currency"], eventToken);
        }
    }
}