using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler : IActivityHandler
    {
        private const string ActivityStateFileName = "AdjustIOActivityState";
        private const string ActivityStateName = "Activity state";
        private const string AttributionFileName = "AdjustAttribution";
        private const string AttributionName = "Attribution";

        private const string AdjustPrefix = "adjust_";

        private DeviceUtil DeviceUtil { get; set; }

        private AdjustConfig AdjustConfig { get; set; }

        private DeviceInfo DeviceInfo { get; set; }

        private ActivityState ActivityState { get; set; }

        private AdjustAttribution Attribution { get; set; }

        private bool Enabled { get; set; }

        private bool Offline { get; set; }

        private TimeSpan SessionInterval { get; set; }

        private TimeSpan SubsessionInterval { get; set; }

        private TimeSpan TimerInterval { get; set; }

        private TimeSpan TimerStart { get; set; }

        private IPackageHandler PackageHandler { get; set; }

        private IAttributionHandler AttributionHandler { get; set; }

        private TimerCycle Timer { get; set; }

        private ActionQueue InternalQueue { get; set; }

        private ILogger Logger { get; set; }

        private ActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            // default values
            Enabled = true;

            Logger = AdjustFactory.Logger;

            InternalQueue = new ActionQueue("adjust.ActivityQueue");
            InternalQueue.Enqueue(() => InitInternal(adjustConfig, deviceUtil));
        }

        public void Init(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            AdjustConfig = adjustConfig;
            DeviceUtil = deviceUtil;
        }

        public static ActivityHandler GetInstance(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            if (adjustConfig == null)
            {
                AdjustFactory.Logger.Error("AdjustConfig missing");
                return null;
            }

            if (!adjustConfig.IsValid())
            {
                AdjustFactory.Logger.Error("AdjustConfig not initialized correctly");
                return null;
            }

            ActivityHandler activityHandler = new ActivityHandler(adjustConfig, deviceUtil);
            return activityHandler;
        }

        public void TrackEvent(AdjustEvent adjustEvent)
        {
            InternalQueue.Enqueue(() => TrackEventInternal(adjustEvent));
        }

        public void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(StartInternal);
        }

        public void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(EndInternal);
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            LaunchDeepLink(jsonDict);
            AttributionHandler.CheckAttribution(jsonDict);
        }

        public void SetEnabled(bool enabled)
        {
            if (!HasChangedState(
                previousState: IsEnabled(),
                newState: enabled,
                trueMessage: "Adjust already enabled",
                falseMessage: "Adjust already disabled"))
            {
                return;
            }

            Enabled = enabled;

            if (ActivityState != null)
            {
                ActivityState.Enabled = enabled;
                WriteActivityState();
            }

            UpdateStatusCondition(
                pausingState: !enabled,
                pausingMessage: "Pausing package and attribution handler to disable the SDK",
                remainsPausedMessage: "Package and attribution handler remain paused due to the SDK is offline",
                unPausingMessage: "Resuming package and attribution handler to enabled the SDK");
        }

        public bool IsEnabled()
        {
            if (ActivityState != null)
            {
                return ActivityState.Enabled;
            }
            else
            {
                return Enabled;
            }
        }

        public void SetOfflineMode(bool offline)
        {
            if (!HasChangedState(
                previousState: Offline,
                newState: offline, 
                trueMessage: "Adjust already in offline mode",
                falseMessage: "Adjust already in online mode"))
            {
                return;
            }

            Offline = offline;

            UpdateStatusCondition(
                pausingState: offline,
                pausingMessage: "Pausing package and attribution handler to put in offline mode",
                remainsPausedMessage: "Package and attribution handler remain paused because the SDK is disabled",
                unPausingMessage: "Resuming package and attribution handler to put in online mode");
        }
        
        private bool HasChangedState(bool previousState, bool newState,
            string trueMessage, string falseMessage)
        {
            if (previousState != newState)
            {
                return true;
            }

            if (previousState)
            {
                Logger.Debug(trueMessage);
            }
            else
            {
                Logger.Debug(falseMessage);
            }

            return false;
        }

        private void UpdateStatusCondition(bool pausingState, string pausingMessage,
            string remainsPausedMessage, string unPausingMessage)
        {
            if (pausingState)
            {
                Logger.Info(pausingMessage);
                TrackSubsessionEnd();
                return;
            }

            if (Paused())
            {
                Logger.Info(remainsPausedMessage);
            }
            else
            {
                Logger.Info(unPausingMessage);
                TrackSubsessionStart();
            }
        }

        public void OpenUrl(Uri uri)
        {
            InternalQueue.Enqueue(() => OpenUrlInternal(uri));
        }

        public bool UpdateAttribution(AdjustAttribution attribution)
        {
            if (attribution == null) { return false; }

            if (attribution.Equals(Attribution)) { return false; }

            Attribution = attribution;
            WriteAttribution();

            RunDelegate(attribution);
            return true;
        }
        
        public void SetAskingAttribution(bool askingAttribution)
        {
            ActivityState.AskingAttribution = askingAttribution;
            WriteActivityState();
        }

        public ActivityPackage GetAttributionPackage()
        {
            var now = DateTime.Now;
            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, now);
            return packageBuilder.BuildAttributionPackage();
        }

        private void UpdateStatus()
        {
            InternalQueue.Enqueue(UpdateStatusInternal);
        }

        private void WriteActivityState()
        {
            InternalQueue.Enqueue(WriteActivityStateInternal);
        }

        private void WriteAttribution()
        {
            InternalQueue.Enqueue(WriteAttributionInternal);
        }

        private void InitInternal(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            Init(adjustConfig, deviceUtil);
            DeviceInfo = DeviceUtil.GetDeviceInfo();

            TimerInterval = AdjustFactory.GetTimerInterval();
            TimerStart = AdjustFactory.GetTimerStart();
            SessionInterval = AdjustFactory.GetSessionInterval();
            SubsessionInterval = AdjustFactory.GetSubsessionInterval();

            if (AdjustConfig.Environment.Equals(AdjustConfig.EnvironmentProduction))
            {
                Logger.LogLevel = LogLevel.Assert;
            }
            
            if (AdjustConfig.EventBufferingEnabled)
            {
                Logger.Info("Event buffering is enabled");
            }

            if (AdjustConfig.DefaultTracker != null)
            {
                Logger.Info("Default tracker: '{0}'", AdjustConfig.DefaultTracker);
            }

            ReadAttribution();
            ReadActivityState();

            PackageHandler = AdjustFactory.GetPackageHandler(this, Paused());

            var attributionPackage = GetAttributionPackage();

            AttributionHandler = AdjustFactory.GetAttributionHandler(this,
                attributionPackage,
                Paused(),
                AdjustConfig.HasDelegate);

            Timer = new TimerCycle(InternalQueue, TimerFiredInternal, timeInterval: TimerInterval, timeStart: TimerStart);

            // TODO remove or keep?
            StartInternal();
        }

        private void StartInternal()
        {
            if (ActivityState != null
                && !ActivityState.Enabled)
            {
                return;
            }

            UpdateStatusInternal();

            ProcessSession();

            CheckAttributionState();

            StartTimer();
        }

        private void ProcessSession()
        {
            var now = DateTime.Now;

            // very firsts Session
            if (ActivityState == null)
            {
                // create fresh activity state
                ActivityState = new ActivityState();
                ActivityState.SessionCount = 1; // first session

                TransferSessionPackage();

                ActivityState.ResetSessionAttributes(now);
                ActivityState.Enabled = Enabled;
                WriteActivityStateInternal();

                return;
            }

            var lastInterval = now - ActivityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                Logger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                WriteActivityStateInternal();
                return;
            }

            // new session
            if (lastInterval > SessionInterval)
            {
                ActivityState.SessionCount++;
                ActivityState.LastInterval = lastInterval;

                TransferSessionPackage();

                ActivityState.ResetSessionAttributes(now);
                WriteActivityStateInternal();

                return;
            }

            // new subsession
            if (lastInterval > SubsessionInterval)
            {
                ActivityState.SubSessionCount++;
                ActivityState.SessionLenght += lastInterval;
                ActivityState.LastActivity = now;

                WriteActivityStateInternal();
                Logger.Info("Started subsession {0} of session {1}",
                    ActivityState.SubSessionCount, ActivityState.SessionCount);
                return;
            }
        }

        private void CheckAttributionState()
        {
            // if it's a new session
            if (ActivityState.SubSessionCount <= 1) { return; }

            // if there is already an attribution saved and there was no attribution being asked
            if (Attribution != null && !ActivityState.AskingAttribution) { return; }

            AttributionHandler.AskAttribution();
        }

        private void EndInternal()
        {
            PackageHandler.PauseSending();
            AttributionHandler.PauseSending();
            StopTimer();
            if (UpdateActivityState(DateTime.Now))
            {
                WriteActivityStateInternal();
            }
        }

        private void TrackEventInternal(AdjustEvent adjustEvent)
        {
            if (!IsEnabled()) { return; }
            if (!CheckEvent(adjustEvent)) { return; }

            var now = DateTime.Now;

            ActivityState.EventCount++;
            UpdateActivityState(now);

            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, ActivityState, now);
            ActivityPackage eventPackage = packageBuilder.BuildEventPackage(adjustEvent);
            PackageHandler.AddPackage(eventPackage);

            if (AdjustConfig.EventBufferingEnabled)
            {
                Logger.Info("Buffered event {0}", eventPackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityStateInternal();
        }

        private void OpenUrlInternal(Uri uri)
        {
            if (uri == null) return;

            var sUri = Uri.UnescapeDataString(uri.ToString());

            var queryStringIdx = sUri.IndexOf("?");
            // check if '?' exists and it's not the last char
            if (queryStringIdx == -1 || queryStringIdx + 1 == sUri.Length) return;

            var queryString = sUri.Substring(queryStringIdx + 1);

            // remove any possible fragments
            var fragmentIdx = queryString.LastIndexOf("#");
            if (fragmentIdx != -1)
            {
                queryString = queryString.Substring(0, fragmentIdx);
            }

            var queryPairs = queryString.Split('&');
            var extraParameters = new Dictionary<string, string>(queryPairs.Length);
            var attribution = new AdjustAttribution();
            bool hasAdjustTags = false;

            foreach (var pair in queryPairs)
            {
                if (ReadQueryString(pair, extraParameters, attribution))
                {
                    hasAdjustTags = true;
                }
            }

            if (!hasAdjustTags) { return; }

            var clickPackage = GetDeeplinkClickPackage(extraParameters, attribution);
            PackageHandler.AddPackage(clickPackage);
            PackageHandler.SendFirstPackage();
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters, AdjustAttribution attribution)
        {
            var now = DateTime.Now;

            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, ActivityState, now);
            packageBuilder.ExtraParameters = extraParameters;

            return packageBuilder.BuildClickPackage("deeplink", now, attribution);
        }

        private bool ReadQueryString(string queryString,
            Dictionary<string, string> extraParameters,
            AdjustAttribution attribution)
        {
            var pairComponents = queryString.Split('=');
            if (pairComponents.Length != 2) return false;

            var key = pairComponents[0];
            if (!key.StartsWith(AdjustPrefix)) return false;

            var value = pairComponents[1];
            if (value.Length == 0) return false;

            var keyWOutPrefix = key.Substring(AdjustPrefix.Length);
            if (keyWOutPrefix.Length == 0) return false;

            if (!ReadAttributionQueryString(attribution, keyWOutPrefix, value))
            {
                extraParameters.Add(keyWOutPrefix, value);
            }

            return true;
        }

        private bool ReadAttributionQueryString(AdjustAttribution attribution,
            string key,
            string value)
        {
            if (key.Equals("tracker"))
            {
                attribution.TrackerName = value;
                return true;
            }

            if (key.Equals("campaign"))
            {
                attribution.Campaign = value;
                return true;
            }

            if (key.Equals("adgroup"))
            {
                attribution.Adgroup = value;
                return true;
            }

            if (key.Equals("creative"))
            {
                attribution.Creative = value;
                return true;
            }

            return false;
        }

        private void WriteActivityStateInternal()
        {
            Util.SerializeToFileAsync(
                fileName: ActivityStateFileName,
                objectWriter: ActivityState.SerializeToStream, 
                input: ActivityState,
                objectName: ActivityStateName)
                .Wait();
        }

        private void WriteAttributionInternal()
        {
            Util.SerializeToFileAsync(
                fileName: AttributionFileName, 
                objectWriter: AdjustAttribution.SerializeToStream,
                input: Attribution,
                objectName: AttributionName)
                .Wait();
        }

        private void ReadActivityState()
        {
            ActivityState = Util.DeserializeFromFileAsync(ActivityStateFileName,
                ActivityState.DeserializeFromStream, //deserialize function from Stream to ActivityState
                () => null, //default value in case of error
                ActivityStateName) // activity state name
                .Result;
        }

        private void ReadAttribution()
        {
            Attribution = Util.DeserializeFromFileAsync(AttributionFileName,
                AdjustAttribution.DeserializeFromStream, //deserialize function from Stream to Attribution
                () => null, //default value in case of error
                AttributionName) // attribution name
                .Result;
        }

        // return whether or not activity state should be written
        private bool UpdateActivityState(DateTime now)
        {
            var lastInterval = now - ActivityState.LastActivity.Value;

            // ignore past updates
            if (lastInterval > SessionInterval) { return false; }

            ActivityState.LastActivity = now;

            if (lastInterval.Ticks < 0)
            {
                Logger.Error("Time Travel!");
            }
            else
            {
                ActivityState.SessionLenght += lastInterval;
                ActivityState.TimeSpent += lastInterval;
            }

            return true;
        }

        private void TransferSessionPackage()
        {
            // build Session Package
            var sessionBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, ActivityState, DateTime.Now);
            var sessionPackage = sessionBuilder.BuildSessionPackage();

            // send Session Package
            PackageHandler.AddPackage(sessionPackage);
            PackageHandler.SendFirstPackage();
        }

        private void RunDelegate(AdjustAttribution adjustAttribution)
        {
            if (AdjustConfig.AttributionChanged == null) return;
            if (adjustAttribution == null) return;

            DeviceUtil.RunAttributionChanged(AdjustConfig.AttributionChanged, adjustAttribution);
        }

        private void LaunchDeepLink(Dictionary<string, string> jsonDict)
        {
            if (jsonDict == null) { return; }

            string deeplink;
            if (!jsonDict.TryGetValue("deeplink", out deeplink)) { return; }

            if (!Uri.IsWellFormedUriString(deeplink, UriKind.Absolute))
            {
                Logger.Error("Malformed deeplink '{0}'", deeplink);
                return;
            }

            Logger.Error("Wellformed deeplink '{0}'", deeplink);

            var deeplinkUri = new Uri(deeplink);
            DeviceUtil.LauchDeeplink(deeplinkUri);
        }

        private void UpdateStatusInternal()
        {
            UpdateAttributionHandlerStatus();
            UpdatePackageHandlerStatus();
        }

        private void UpdateAttributionHandlerStatus()
        {
            if (Paused())
            {
                AttributionHandler.PauseSending();
            }
            else
            {
                AttributionHandler.ResumeSending();
            }
        }

        private void UpdatePackageHandlerStatus()
        {
            if (Paused())
            {
                PackageHandler.PauseSending();
            }
            else
            {
                PackageHandler.ResumeSending();
            }
        }

        private bool Paused()
        {
            return Offline || !IsEnabled();
        }

        #region Timer

        private void StartTimer()
        {
            if (Paused())
            {
                return;
            }

            Timer.Resume();
        }

        private void StopTimer()
        {
            Timer.Suspend();
        }

        private void TimerFiredInternal()
        {
            if (Paused())
            {
                StopTimer();
                return;
            }

            Logger.Debug("Session timer fired");
            PackageHandler.SendFirstPackage();

            if (UpdateActivityState(DateTime.Now))
            {
                WriteActivityStateInternal();
            }
        }

        #endregion Timer

        private bool CheckEvent(AdjustEvent adjustEvent)
        {
            if (adjustEvent == null)
            {
                Logger.Error("Event missing");
                return false;
            }

            if (!adjustEvent.IsValid())
            {
                Logger.Error("Event not initialized correctly");
                return false;
            }

            return true;
        }
    }
}