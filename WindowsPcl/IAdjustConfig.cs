using System;
using System.Collections.Generic;
using AdjustSdk.Pcl;

namespace AdjustSdk
{
    public interface IAdjustConfig
    {
        string AppToken { get; }
        string Environment { get; }

        string SdkPrefix { get; set; }
        bool EventBufferingEnabled { get; set; }
        string DefaultTracker { get; set; }
        bool SendInBackground { get; set; }
        TimeSpan? DelayStart { get; set; }
        Action<AdjustAttribution> AttributionChanged { get; set; }
        Action<AdjustEventSuccess> EventTrackingSucceeded { get; set; }
        Action<AdjustEventFailure> EventTrackingFailed { get; set; }
        Action<AdjustSessionSuccess> SesssionTrackingSucceeded { get; set; }
        Action<AdjustSessionFailure> SesssionTrackingFailed { get; set; }
        bool IsValid();

        List<Action<ActivityHandler>> SessionParametersActions { get; set;  }
        string PushToken { get; set; }
    }
}