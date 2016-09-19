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
        private TimeSpan SessionInterval { get; set; }
        private TimeSpan SubsessionInterval { get; set; }
        private IPackageHandler PackageHandler { get; set; }
        private IAttributionHandler AttributionHandler { get; set; }
        private TimerCycle Timer { get; set; }
        private ActionQueue InternalQueue { get; set; }
        private ILogger Logger { get; set; }
        private InternalState State { get; set; }

        public class InternalState
        {
            internal bool enabled;
            internal bool offline;

            public bool IsEnabled { get { return enabled; } }
            public bool IsDisabled { get { return !enabled; } }
            public bool IsOffline { get { return offline; } }
            public bool IsOnline { get { return !offline; } }
        }

        private ActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            Logger = AdjustFactory.Logger;

            InternalQueue = new ActionQueue("adjust.ActivityQueue");
            // default values
            State.enabled = true;
            State.offline = false;

            Init(adjustConfig, deviceUtil);

            InternalQueue.Enqueue(InitI);
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
            InternalQueue.Enqueue(() => TrackEventI(adjustEvent));
        }

        public void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(StartI);
        }

        public void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(EndI);
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

            State.enabled = enabled;

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
                return State.enabled;
            }
        }

        public void SetOfflineMode(bool offline)
        {
            if (!HasChangedState(
                previousState: State.IsOffline,
                newState: offline, 
                trueMessage: "Adjust already in offline mode",
                falseMessage: "Adjust already in online mode"))
            {
                return;
            }

            State.offline = offline;

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

            if (PausedI())
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
            InternalQueue.Enqueue(() => OpenUrlI(uri));
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
            return GetAttributionPackageI();
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters, AdjustAttribution attribution)
        {
            return GetDeeplinkClickPackageI(extraParameters, attribution);
        }

        private void UpdateStatus()
        {
            InternalQueue.Enqueue(UpdateStatusI);
        }

        private void WriteActivityState()
        {
            InternalQueue.Enqueue(WriteActivityStateI);
        }

        private void WriteAttribution()
        {
            InternalQueue.Enqueue(WriteAttributionI);
        }

        private void InitI()
        {
            DeviceInfo = DeviceUtil.GetDeviceInfo();
            DeviceInfo.SdkPrefix = AdjustConfig.SdkPrefix;

            TimeSpan timerInterval = AdjustFactory.GetTimerInterval();
            TimeSpan timerStart = AdjustFactory.GetTimerStart();
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

            ReadAttributionI();
            ReadActivityStateI();

            PackageHandler = AdjustFactory.GetPackageHandler(this, PausedI());

            var attributionPackage = GetAttributionPackageI();

            AttributionHandler = AdjustFactory.GetAttributionHandler(this,
                attributionPackage,
                PausedI(),
                AdjustConfig.HasDelegate);

            Timer = new TimerCycle(InternalQueue, TimerFiredI, timeInterval: timerInterval, timeStart: timerStart);

            StartI();
        }

        private void StartI()
        {
            if (ActivityState != null
                && !ActivityState.Enabled)
            {
                return;
            }

            UpdateStatusI();

            ProcessSessionI();

            CheckAttributionStateI();

            StartTimerI();
        }

        private void ProcessSessionI()
        {
            var now = DateTime.Now;

            // very firsts Session
            if (ActivityState == null)
            {
                // create fresh activity state
                ActivityState = new ActivityState();
                ActivityState.SessionCount = 1; // first session

                TransferSessionPackageI();

                ActivityState.ResetSessionAttributes(now);
                ActivityState.Enabled = State.IsEnabled;
                WriteActivityStateI();

                return;
            }

            var lastInterval = now - ActivityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                Logger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                WriteActivityStateI();
                return;
            }

            // new session
            if (lastInterval > SessionInterval)
            {
                ActivityState.SessionCount++;
                ActivityState.LastInterval = lastInterval;

                TransferSessionPackageI();

                ActivityState.ResetSessionAttributes(now);
                WriteActivityStateI();

                return;
            }

            // new subsession
            if (lastInterval > SubsessionInterval)
            {
                ActivityState.SubSessionCount++;
                ActivityState.SessionLenght += lastInterval;
                ActivityState.LastActivity = now;

                WriteActivityStateI();
                Logger.Info("Started subsession {0} of session {1}",
                    ActivityState.SubSessionCount, ActivityState.SessionCount);
                return;
            }
        }

        private void CheckAttributionStateI()
        {
            // if it's a new session
            if (ActivityState.SubSessionCount <= 1) { return; }

            // if there is already an attribution saved and there was no attribution being asked
            if (Attribution != null && !ActivityState.AskingAttribution) { return; }

            AttributionHandler.AskAttribution();
        }

        private void EndI()
        {
            PackageHandler.PauseSending();
            AttributionHandler.PauseSending();
            StopTimerI();
            if (UpdateActivityStateI(DateTime.Now))
            {
                WriteActivityStateI();
            }
        }

        private void TrackEventI(AdjustEvent adjustEvent)
        {
            if (!IsEnabledI()) { return; }
            if (!CheckEventI(adjustEvent)) { return; }

            var now = DateTime.Now;

            ActivityState.EventCount++;
            UpdateActivityStateI(now);

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

            WriteActivityStateI();
        }

        private void OpenUrlI(Uri uri)
        {
            if (uri == null) return;

            var sUri = Uri.UnescapeDataString(uri.ToString());

            var windowsPhone80Protocol = "/Protocol?";
            if (sUri.StartsWith(windowsPhone80Protocol))
            {
                sUri = sUri.Substring(windowsPhone80Protocol.Length);
            }
            
            var queryStringIdx = sUri. IndexOf("?");
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
                if (ReadQueryStringI(pair, extraParameters, attribution))
                {
                    hasAdjustTags = true;
                }
            }

            if (!hasAdjustTags) { return; }

            var clickPackage = GetDeeplinkClickPackageI(extraParameters, attribution);
            PackageHandler.AddPackage(clickPackage);
            PackageHandler.SendFirstPackage();
        }

        private ActivityPackage GetDeeplinkClickPackageI(Dictionary<string, string> extraParameters, AdjustAttribution attribution)
        {
            var now = DateTime.Now;

            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, ActivityState, now);
            packageBuilder.ExtraParameters = extraParameters;

            return packageBuilder.BuildClickPackage("deeplink", now, attribution);
        }

        private bool ReadQueryStringI(string queryString,
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

            if (!ReadAttributionQueryStringI(attribution, keyWOutPrefix, value))
            {
                extraParameters.Add(keyWOutPrefix, value);
            }

            return true;
        }

        private bool ReadAttributionQueryStringI(AdjustAttribution attribution,
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

        private bool IsEnabledI()
        {
            if (ActivityState != null)
            {
                return ActivityState.Enabled;
            }
            else
            {
                return State.IsEnabled;
            }
        }

        private ActivityPackage GetAttributionPackageI()
        {
            var now = DateTime.Now;
            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, now);
            return packageBuilder.BuildAttributionPackage();
        }

        private void WriteActivityStateI()
        {
            Util.SerializeToFileAsync(
                fileName: ActivityStateFileName,
                objectWriter: ActivityState.SerializeToStream, 
                input: ActivityState,
                objectName: ActivityStateName)
                .Wait();
        }

        private void WriteAttributionI()
        {
            Util.SerializeToFileAsync(
                fileName: AttributionFileName, 
                objectWriter: AdjustAttribution.SerializeToStream,
                input: Attribution,
                objectName: AttributionName)
                .Wait();
        }

        private void ReadActivityStateI()
        {
            ActivityState = Util.DeserializeFromFileAsync(ActivityStateFileName,
                ActivityState.DeserializeFromStream, //deserialize function from Stream to ActivityState
                () => null, //default value in case of error
                ActivityStateName) // activity state name
                .Result;
        }

        private void ReadAttributionI()
        {
            Attribution = Util.DeserializeFromFileAsync(AttributionFileName,
                AdjustAttribution.DeserializeFromStream, //deserialize function from Stream to Attribution
                () => null, //default value in case of error
                AttributionName) // attribution name
                .Result;
        }

        // return whether or not activity state should be written
        private bool UpdateActivityStateI(DateTime now)
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

        private void TransferSessionPackageI()
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

        private void UpdateStatusI()
        {
            UpdateAttributionHandlerStatusI();
            UpdatePackageHandlerStatusI();
        }

        private void UpdateAttributionHandlerStatusI()
        {
            if (PausedI())
            {
                AttributionHandler.PauseSending();
            }
            else
            {
                AttributionHandler.ResumeSending();
            }
        }

        private void UpdatePackageHandlerStatusI()
        {
            if (PausedI())
            {
                PackageHandler.PauseSending();
            }
            else
            {
                PackageHandler.ResumeSending();
            }
        }

        private bool PausedI()
        {
            return State.IsOffline || !IsEnabledI();
        }

        #region Timer

        private void StartTimerI()
        {
            if (PausedI())
            {
                return;
            }

            Timer.Resume();
        }

        private void StopTimerI()
        {
            Timer.Suspend();
        }

        private void TimerFiredI()
        {
            if (PausedI())
            {
                StopTimerI();
                return;
            }

            Logger.Debug("Session timer fired");
            PackageHandler.SendFirstPackage();

            if (UpdateActivityStateI(DateTime.Now))
            {
                WriteActivityStateI();
            }
        }

        #endregion Timer

        private bool CheckEventI(AdjustEvent adjustEvent)
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