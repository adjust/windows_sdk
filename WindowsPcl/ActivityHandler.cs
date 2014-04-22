using AdjustSdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler : IActivityHandler
    {
        public AdjustApi.Environment Environment { get; private set; }

        public bool IsBufferedEventsEnabled { get; private set; }
        private bool Enabled { get; set; }

        private const string ActivityStateFileName = "AdjustIOActivityState";
        private const string AdjustPrefix = "adjust_";
        private TimeSpan SessionInterval;
        private TimeSpan SubsessionInterval;
        private readonly TimeSpan TimerInterval = AdjustFactory.GetTimerInterval();

        private IPackageHandler PackageHandler;
        private ActivityState ActivityState;
        private TimerPclNet45 TimeKeeper;
        private ActionQueue InternalQueue;
        private Action<ResponseData> ResponseDelegate;
        private ILogger Logger;

        private Action<Action<ResponseData>, ResponseData> ResponseDelegateAction;

        private string DeviceUniqueId;
        private string HardwareId;
        private string NetworkAdapterId;

        private string AppToken;
        private string UserAgent;
        private string ClientSdk;

        public ActivityHandler(string appToken, DeviceUtil deviceUtil)
        {
            // default values
            Environment = AdjustApi.Environment.Unknown;
            IsBufferedEventsEnabled = false;
            Enabled = true;
            Logger = AdjustFactory.Logger;

            SessionInterval = AdjustFactory.GetSessionInterval();
            SubsessionInterval = AdjustFactory.GetSubsessionInterval();

            InternalQueue = new ActionQueue("adjust.ActivityQueue");
            InternalQueue.Enqueue(() => InitInternal(appToken, deviceUtil));
        }

        public void SetEnvironment(AdjustApi.Environment enviornment)
        {
            Environment = enviornment;
        }

        public void SetBufferedEvents(bool enabledEventBuffering)
        {
            IsBufferedEventsEnabled = enabledEventBuffering;
        }

        public void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            ResponseDelegate = responseDelegate;
        }

        public void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(StartInternal);
        }

        public void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(EndInternal);
        }

        public void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => EventInternal(eventToken, callbackParameters));
        }

        public void TrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => RevenueInternal(amountInCents, eventToken, callbackParameters));
        }

        public void FinishTrackingWithResponse(ResponseData responseData)
        {
            if (ResponseDelegate != null)
                ResponseDelegateAction(ResponseDelegate, responseData);
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (CheckActivityState(ActivityState))
            {
                ActivityState.IsEnabled = enabled;
            }
            if (enabled)
            {
                TrackSubsessionStart();
            }
            else
            {
                TrackSubsessionEnd();
            }
        }

        public bool IsEnabled()
        {
            if (CheckActivityState(ActivityState))
            {
                return ActivityState.IsEnabled;
            }
            else
            {
                return Enabled;
            }
        }

        public void ReadOpenUrl(Uri url)
        {
            InternalQueue.Enqueue(() => ReadOpenUrlInternal(url));
        }

        private void InitInternal(string appToken, DeviceUtil deviceUtil)
        {
            if (!CheckAppToken(appToken)) return;
            if (!CheckAppTokenLength(appToken)) return;

            AppToken = appToken;
            ClientSdk = deviceUtil.ClientSdk;
            UserAgent = deviceUtil.GetUserAgent();

            DeviceUniqueId = deviceUtil.GetDeviceUniqueId();
            HardwareId = deviceUtil.GetHardwareId();
            NetworkAdapterId = deviceUtil.GetNetworkAdapterId();

            PackageHandler = AdjustFactory.GetPackageHandler(this);
            ResponseDelegateAction = deviceUtil.RunResponseDelegate;

            ReadActivityState();

            StartInternal();
        }

        private void StartInternal()
        {
            if (!CheckAppToken(AppToken)) return;

            if (ActivityState != null
                && !ActivityState.IsEnabled)
            {
                return;
            }

            PackageHandler.ResumeSending();
            StartTimer();

            var now = DateTime.Now;

            // very firsts Session
            if (ActivityState == null)
            {
                // create fresh activity state
                ActivityState = new ActivityState();
                ActivityState.SessionCount = 1; // first session
                ActivityState.CreatedAt = now;

                TransferSessionPackage();

                ActivityState.ResetSessionAttributes(now);
                ActivityState.IsEnabled = Enabled;
                WriteActivityState();

                Logger.Info("First session");
                return;
            }

            var lastInterval = now - ActivityState.LastActivity.Value;

            Logger.Verbose("Last interval ({0})", lastInterval);

            if (lastInterval.Ticks < 0)
            {
                Logger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                WriteActivityState();
                return;
            }

            // new session
            if (lastInterval > SessionInterval)
            {
                ActivityState.SessionCount++;
                ActivityState.CreatedAt = now;
                ActivityState.LastInterval = lastInterval;

                TransferSessionPackage();
                ActivityState.ResetSessionAttributes(now);
                WriteActivityState();

                Logger.Debug("Session {0}", ActivityState.SessionCount);
                return;
            }

            // new subsession
            if (lastInterval > SubsessionInterval)
            {
                ActivityState.SubSessionCount++;
                ActivityState.SessionLenght += lastInterval;
                ActivityState.LastActivity = now;

                WriteActivityState();
                Logger.Info("Processed Subsession {0} of Session {1}",
                    ActivityState.SubSessionCount, ActivityState.SessionCount);
                return;
            }
        }

        private void EndInternal()
        {
            if (!CheckAppToken(AppToken)) return;

            PackageHandler.PauseSending();
            StopTimer();
            UpdateActivityState(DateTime.Now);
            WriteActivityState();
        }

        private void EventInternal(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            if (!CheckAppToken(AppToken)) return;
            if (!CheckActivityState(ActivityState)) return;
            if (!CheckEventToken(eventToken)) return;
            if (!CheckEventTokenLenght(eventToken)) return;
            if (!ActivityState.IsEnabled) return;

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.EventToken = eventToken;
            packageBuilder.CallbackParameters = callbackParameters;

            var now = DateTime.Now;

            UpdateActivityState(now);
            ActivityState.CreatedAt = now;
            ActivityState.EventCount++;

            ActivityState.InjectEventAttributes(packageBuilder);
            var eventPackage = packageBuilder.BuildEventPackage();

            PackageHandler.AddPackage(eventPackage);

            if (IsBufferedEventsEnabled)
            {
                Logger.Info("Buffered event{0}", eventPackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityState();
            Logger.Debug("Event {0}", ActivityState.EventCount);
        }

        private void RevenueInternal(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            if (!CheckAppToken(AppToken)) return;
            if (!CheckActivityState(ActivityState)) return;
            if (!CheckAmount(amountInCents)) return;
            if (!CheckEventTokenLenght(eventToken)) return;
            if (!ActivityState.IsEnabled) return;

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.AmountInCents = amountInCents;
            packageBuilder.EventToken = eventToken;
            packageBuilder.CallbackParameters = callbackParameters;

            var now = DateTime.Now;
            UpdateActivityState(now);

            ActivityState.CreatedAt = now;
            ActivityState.EventCount++;

            ActivityState.InjectEventAttributes(packageBuilder);

            var revenuePackage = packageBuilder.BuildRevenuePackage();

            PackageHandler.AddPackage(revenuePackage);

            if (IsBufferedEventsEnabled)
            {
                Logger.Info("Buffered revenue{0}", revenuePackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityState();
            Logger.Debug("Event {0} (revenue)", ActivityState.EventCount);
        }

        private void ReadOpenUrlInternal(Uri url)
        {
            if (url == null) return;

            var sUrl = Uri.UnescapeDataString(url.ToString());

            var queryStringIdx = sUrl.IndexOf("?");
            // check if '?' exists and it's not the last char
            if (queryStringIdx == -1 || queryStringIdx + 1 == sUrl.Length) return;

            var queryString = sUrl.Substring(queryStringIdx + 1);

            // remove any possible fragments
            var fragmentIdx = queryString.LastIndexOf("#");
            if (fragmentIdx != -1)
            {
                queryString = queryString.Substring(0, fragmentIdx);
            }

            var queryPairs = queryString.Split('&');
            var adjustDeepLinks = new Dictionary<string, string>(queryPairs.Length);
            foreach (var pair in queryPairs)
            {
                var pairComponents = pair.Split('=');
                if (pairComponents.Length != 2) continue;

                var key = pairComponents[0];
                if (!key.StartsWith(AdjustPrefix)) continue;

                var value = pairComponents[1];
                if (value.Length == 0) continue;

                var keyWOutPrefix = key.Substring(AdjustPrefix.Length);
                if (keyWOutPrefix.Length == 0) continue;

                adjustDeepLinks.Add(keyWOutPrefix, value);
            }

            if (adjustDeepLinks.Count == 0) return;

            var packageBuilder = GetDefaultPackageBuilder();
            packageBuilder.DeepLinksParameters = adjustDeepLinks;

            var reattributionPackage = packageBuilder.BuildReattributionPackage();
            PackageHandler.AddPackage(reattributionPackage);
            PackageHandler.SendFirstPackage();

            Logger.Debug("Reattribution {0}", reattributionPackage.Parameters["deeplink_parameters"]);
        }

        private void WriteActivityState()
        {
            var sucessMessage = Util.f("Wrote activity state: {0}", ActivityState);
            Util.SerializeToFileAsync(ActivityStateFileName, ActivityState.SerializeToStream, ActivityState, sucessMessage).Wait();
        }

        private void ReadActivityState()
        {
            Func<ActivityState, string> successMessage = (activityState) =>
                Util.f("Read activity state:{0} uuid {1}", activityState, ActivityState.Uuid);

            ActivityState = Util.DeserializeFromFileAsync(ActivityStateFileName,
                ActivityState.DeserializeFromStream, //deserialize function from Stream to ActivityState
                () => null, //default value in case of error
                successMessage) // message generated for the activity state, if it was succesfully read
                .Result;
        }

        // return whether or not activity state should be written
        private bool UpdateActivityState(DateTime now)
        {
            if (!CheckActivityState(ActivityState))
                return false;

            var lastInterval = now - ActivityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                Logger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                return true;
            }

            // ignore past updates
            if (lastInterval > SessionInterval)
                return false;

            ActivityState.SessionLenght += lastInterval;
            ActivityState.TimeSpent += lastInterval;
            ActivityState.LastActivity = now;

            return lastInterval > SubsessionInterval;
        }

        private void TransferSessionPackage()
        {
            // build Session Package
            var sessionBuilder = GetDefaultPackageBuilder();
            ActivityState.InjectSessionAttributes(sessionBuilder);
            var sessionPackage = sessionBuilder.BuildSessionPackage();

            // send Session Package
            PackageHandler.AddPackage(sessionPackage);
            PackageHandler.SendFirstPackage();
        }

        private PackageBuilder GetDefaultPackageBuilder()
        {
            var packageBuilder = new PackageBuilder
            {
                UserAgent = UserAgent,
                ClientSdk = ClientSdk,
                AppToken = AppToken,
                DeviceUniqueId = DeviceUniqueId,
                HardwareId = HardwareId,
                NetworkAdapterId = NetworkAdapterId,
                Environment = Environment,
            };
            return packageBuilder;
        }

        #region Timer

        private void StartTimer()
        {
            if (TimeKeeper == null)
            {
                TimeKeeper = new TimerPclNet45(SystemThreadingTimer, null, TimerInterval);
            }
            TimeKeeper.Resume();
        }

        private void StopTimer()
        {
            TimeKeeper.Pause();
        }

        private void SystemThreadingTimer(object state)
        {
            InternalQueue.Enqueue(TimerFired);
        }

        private void TimerFired()
        {
            if (ActivityState != null
                && !ActivityState.IsEnabled)
            {
                return;
            }
            PackageHandler.SendFirstPackage();
            if (UpdateActivityState(DateTime.Now))
                WriteActivityState();
        }

        #endregion Timer

        #region Checks

        private bool CheckAppToken(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                Logger.Error("Missing App Token");
                return false;
            }
            return true;
        }

        private bool CheckAppTokenLength(string appToken)
        {
            if (appToken.Length != 12)
            {
                Logger.Error("Malformed App Token '{0}'", appToken);
                return false;
            }
            return true;
        }

        private bool CheckActivityState(ActivityState activityState)
        {
            if (activityState == null)
            {
                Logger.Error("Missing activity state");
                return false;
            }
            return true;
        }

        private bool CheckEventToken(string eventToken)
        {
            if (eventToken == null)
            {
                Logger.Error("Missing Event Token");
                return false;
            }
            return true;
        }

        private bool CheckEventTokenLenght(string eventToken)
        {
            if (eventToken != null && eventToken.Length != 6)
            {
                Logger.Error("Malformed Event Token '{0}'", eventToken);
                return false;
            }
            return true;
        }

        private bool CheckAmount(double amount)
        {
            if (amount < 0.0)
            {
                Logger.Error("Invalid amount {0:0.0}", amount);
                return false;
            }
            return true;
        }

        #endregion Checks
    }
}