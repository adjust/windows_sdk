using System;

namespace AdjustSdk.Pcl
{
    public interface IActivityHandler
    {
        AdjustApi.Environment Environment { get; }

        void FinishTrackingWithResponse(AdjustSdk.ResponseData responseData);

        bool IsBufferedEventsEnabled { get; }

        void SetBufferedEvents(bool enabledEventBuffering);

        void SetEnvironment(AdjustApi.Environment enviornment);

        void SetResponseDelegate(Action<AdjustSdk.ResponseData> responseDelegate);

        void TrackEvent(string eventToken, System.Collections.Generic.Dictionary<string, string> callbackParameters);

        void TrackRevenue(double amountInCents, string eventToken, System.Collections.Generic.Dictionary<string, string> callbackParameters);

        void TrackSubsessionEnd();

        void TrackSubsessionStart();

        void SetEnabled(bool enabled);

        bool IsEnabled();

        void ReadOpenUrl(Uri url);

        void SetSdkPrefix(string sdkPrefix);
    }
}