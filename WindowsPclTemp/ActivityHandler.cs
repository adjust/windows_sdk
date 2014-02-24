using AdjustSdk;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler
    {
        public AdjustApi.Environment Environment { get; private set; }

        public bool IsBufferedEventsEnabled { get; private set; }

        private const string ActivityStateFileName = "AdjustIOActivityState";
        private static readonly TimeSpan SessionInterval = new TimeSpan(0, 30, 0); // 30 minutes
        private static readonly TimeSpan SubSessionInterval = new TimeSpan(0, 0, 1); // 1 second
        private static readonly TimeSpan TimerInterval = new TimeSpan(0, 1, 0); // 1 minute

        private PackageHandler PackageHandler;
        private ActivityState ActivityState;
        private TimerPclNet45 TimeKeeper;
        private ActionQueue InternalQueue;
        private Action<ResponseData> ResponseDelegate;

        private Action<Action<ResponseData>, ResponseData> ResponseDelegateAction;

        private string DeviceUniqueId;
        private string HardwareId;
        private string NetworkAdapterId;

        private string AppToken;
        private string UserAgent;
        private string ClientSdk;

        internal ActivityHandler(string appToken, DeviceUtil deviceUtil)
        {
            // default values
            Environment = AdjustApi.Environment.Unknown;
            IsBufferedEventsEnabled = false;

            InternalQueue = new ActionQueue("adjust.ActivityQueue");
            InternalQueue.Enqueue(() => InitInternal(appToken, deviceUtil));
        }

        internal void SetEnvironment(AdjustApi.Environment enviornment)
        {
            Environment = enviornment;
        }

        internal void SetBufferedEvents(bool enabledEventBuffering)
        {
            IsBufferedEventsEnabled = enabledEventBuffering;
        }

        internal void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            ResponseDelegate = responseDelegate;
        }

        internal void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(StartInternal);
        }

        internal void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(EndInternal);
        }

        internal void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => EventInternal(eventToken, callbackParameters));
        }

        internal void TrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => RevenueInternal(amountInCents, eventToken, callbackParameters));
        }

        internal void FinishTrackingWithResponse(ResponseData responseData)
        {
            if (ResponseDelegate != null)
                ResponseDelegateAction(ResponseDelegate, responseData);
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

            PackageHandler = new PackageHandler(this);
            ResponseDelegateAction = deviceUtil.RunResponseDelegate;

            ReadActivityState();

            StartInternal();
        }

        private void StartInternal()
        {
            PackageHandler.ResumeSending();
            StartTimer();

            var now = DateTime.Now;

            Logger.Verbose("Now time ({0})", now);

            // if firsts Session
            if (ActivityState == null)
            {
                // create fresh activity state
                ActivityState = new ActivityState();
                ActivityState.SessionCount = 1; // first session
                ActivityState.CreatedAt = now;

                TransferSessionPackage();

                ActivityState.ResetSessionAttributes(now);
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
            if (lastInterval > SubSessionInterval)
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
            UpdateActivityState();
            WriteActivityState();
        }

        private void EventInternal(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            if (!CheckAppToken(AppToken)) return;
            if (!CheckActivityState(ActivityState)) return;
            if (!CheckEventToken(eventToken)) return;
            if (!CheckEventTokenLenght(eventToken)) return;

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.EventToken = eventToken;
            packageBuilder.CallbackParameters = callbackParameters;

            var now = DateTime.Now;

            UpdateActivityState();
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

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.AmountInCents = amountInCents;
            packageBuilder.EventToken = eventToken;
            packageBuilder.CallbackParameters = callbackParameters;

            var now = DateTime.Now;
            UpdateActivityState();

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

        private void WriteActivityState()
        {
            var sucessMessage = String.Format("Wrote activity state: {0}", ActivityState);
            Util.SerializeToFileAsync(ActivityStateFileName, ActivityState.SerializeToStream, ActivityState, sucessMessage).Wait();
        }

        private void ReadActivityState()
        {
            ActivityState = Util.DeserializeFromFileAsync(ActivityStateFileName,
                ActivityState.DeserializeFromStream, //deserialize function from Stream to ActivityState
                () => null) //default value in case of error
                .Result;
        }

        // return whether or not activity state should be written
        private bool UpdateActivityState()
        {
            if (!CheckActivityState(ActivityState))
                return false;

            var now = DateTime.Now;
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

            return lastInterval > SubSessionInterval;
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
            PackageHandler.SendFirstPackage();
            if (UpdateActivityState())
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
                Logger.Error("Invalid amount {0:.0}", amount);
                return false;
            }
            return true;
        }

        #endregion Checks
    }
}