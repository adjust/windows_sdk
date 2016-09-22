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

        private ILogger _Logger = AdjustFactory.Logger;
        private ActionQueue _ActionQueue = new ActionQueue("adjust.ActivityHandler");
        private InternalState _State = new InternalState();

        private DeviceUtil _DeviceUtil;
        private AdjustConfig _Config;
        private DeviceInfo _DeviceInfo;
        private ActivityState _ActivityState;
        private AdjustAttribution _Attribution;
        private TimeSpan _SessionInterval;
        private TimeSpan _SubsessionInterval;
        private IPackageHandler _PackageHandler;
        private IAttributionHandler _AttributionHandler;
        private TimerCycle _Timer;

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
            // default values
            _State.enabled = true;
            _State.offline = false;

            Init(adjustConfig, deviceUtil);
            _ActionQueue.Enqueue(InitI);
        }

        public void Init(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            _Config = adjustConfig;
            _DeviceUtil = deviceUtil;
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
            _ActionQueue.Enqueue(() => TrackEventI(adjustEvent));
        }

        public void TrackSubsessionStart()
        {
            _ActionQueue.Enqueue(StartI);
        }

        public void TrackSubsessionEnd()
        {
            _ActionQueue.Enqueue(EndI);
        }

        public void FinishedTrackingActivity(Dictionary<string, string> jsonDict)
        {
            LaunchDeepLink(jsonDict);
            _AttributionHandler.CheckAttribution(jsonDict);
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

            _State.enabled = enabled;

            if (_ActivityState != null)
            {
                _ActivityState.Enabled = enabled;
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
            if (_ActivityState != null)
            {
                return _ActivityState.Enabled;
            }
            else
            {
                return _State.enabled;
            }
        }

        public void SetOfflineMode(bool offline)
        {
            if (!HasChangedState(
                previousState: _State.IsOffline,
                newState: offline, 
                trueMessage: "Adjust already in offline mode",
                falseMessage: "Adjust already in online mode"))
            {
                return;
            }

            _State.offline = offline;

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
                _Logger.Debug(trueMessage);
            }
            else
            {
                _Logger.Debug(falseMessage);
            }

            return false;
        }

        private void UpdateStatusCondition(bool pausingState, string pausingMessage,
            string remainsPausedMessage, string unPausingMessage)
        {
            if (pausingState)
            {
                _Logger.Info(pausingMessage);
                TrackSubsessionEnd();
                return;
            }

            if (PausedI())
            {
                _Logger.Info(remainsPausedMessage);
            }
            else
            {
                _Logger.Info(unPausingMessage);
                TrackSubsessionStart();
            }
        }

        public void OpenUrl(Uri uri)
        {
            _ActionQueue.Enqueue(() => OpenUrlI(uri));
        }

        public bool UpdateAttribution(AdjustAttribution attribution)
        {
            if (attribution == null) { return false; }

            if (attribution.Equals(_Attribution)) { return false; }

            _Attribution = attribution;
            WriteAttribution();

            RunDelegate(attribution);
            return true;
        }
        
        public void SetAskingAttribution(bool askingAttribution)
        {
            _ActivityState.AskingAttribution = askingAttribution;
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
            _ActionQueue.Enqueue(UpdateStatusI);
        }

        private void WriteActivityState()
        {
            _ActionQueue.Enqueue(WriteActivityStateI);
        }

        private void WriteAttribution()
        {
            _ActionQueue.Enqueue(WriteAttributionI);
        }

        private void InitI()
        {
            _DeviceInfo = _DeviceUtil.GetDeviceInfo();
            _DeviceInfo.SdkPrefix = _Config.SdkPrefix;

            TimeSpan timerInterval = AdjustFactory.GetTimerInterval();
            TimeSpan timerStart = AdjustFactory.GetTimerStart();
            _SessionInterval = AdjustFactory.GetSessionInterval();
            _SubsessionInterval = AdjustFactory.GetSubsessionInterval();

            if (_Config.Environment.Equals(AdjustConfig.EnvironmentProduction))
            {
                _Logger.LogLevel = LogLevel.Assert;
            }
            
            if (_Config.EventBufferingEnabled)
            {
                _Logger.Info("Event buffering is enabled");
            }

            if (_Config.DefaultTracker != null)
            {
                _Logger.Info("Default tracker: '{0}'", _Config.DefaultTracker);
            }

            ReadAttributionI();
            ReadActivityStateI();

            _PackageHandler = AdjustFactory.GetPackageHandler(this, PausedI());

            var attributionPackage = GetAttributionPackageI();

            _AttributionHandler = AdjustFactory.GetAttributionHandler(this,
                attributionPackage,
                PausedI(),
                _Config.HasDelegate);

            _Timer = new TimerCycle(_ActionQueue, TimerFiredI, timeInterval: timerInterval, timeStart: timerStart);

            StartI();
        }

        private void StartI()
        {
            if (_ActivityState != null
                && !_ActivityState.Enabled)
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
            if (_ActivityState == null)
            {
                // create fresh activity state
                _ActivityState = new ActivityState();
                _ActivityState.SessionCount = 1; // first session

                TransferSessionPackageI();

                _ActivityState.ResetSessionAttributes(now);
                _ActivityState.Enabled = _State.IsEnabled;
                WriteActivityStateI();

                return;
            }

            var lastInterval = now - _ActivityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                _Logger.Error("Time Travel!");
                _ActivityState.LastActivity = now;
                WriteActivityStateI();
                return;
            }

            // new session
            if (lastInterval > _SessionInterval)
            {
                _ActivityState.SessionCount++;
                _ActivityState.LastInterval = lastInterval;

                TransferSessionPackageI();

                _ActivityState.ResetSessionAttributes(now);
                WriteActivityStateI();

                return;
            }

            // new subsession
            if (lastInterval > _SubsessionInterval)
            {
                _ActivityState.SubSessionCount++;
                _ActivityState.SessionLenght += lastInterval;
                _ActivityState.LastActivity = now;

                WriteActivityStateI();
                _Logger.Info("Started subsession {0} of session {1}",
                    _ActivityState.SubSessionCount, _ActivityState.SessionCount);
                return;
            }
        }

        private void CheckAttributionStateI()
        {
            // if it's a new session
            if (_ActivityState.SubSessionCount <= 1) { return; }

            // if there is already an attribution saved and there was no attribution being asked
            if (_Attribution != null && !_ActivityState.AskingAttribution) { return; }

            _AttributionHandler.AskAttribution();
        }

        private void EndI()
        {
            _PackageHandler.PauseSending();
            _AttributionHandler.PauseSending();
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

            _ActivityState.EventCount++;
            UpdateActivityStateI(now);

            var packageBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, now);
            ActivityPackage eventPackage = packageBuilder.BuildEventPackage(adjustEvent);
            _PackageHandler.AddPackage(eventPackage);

            if (_Config.EventBufferingEnabled)
            {
                _Logger.Info("Buffered event {0}", eventPackage.Suffix);
            }
            else
            {
                _PackageHandler.SendFirstPackage();
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
            _PackageHandler.AddPackage(clickPackage);
            _PackageHandler.SendFirstPackage();
        }

        private ActivityPackage GetDeeplinkClickPackageI(Dictionary<string, string> extraParameters, AdjustAttribution attribution)
        {
            var now = DateTime.Now;

            var packageBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, now);
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
            if (_ActivityState != null)
            {
                return _ActivityState.Enabled;
            }
            else
            {
                return _State.IsEnabled;
            }
        }

        private ActivityPackage GetAttributionPackageI()
        {
            var now = DateTime.Now;
            var packageBuilder = new PackageBuilder(_Config, _DeviceInfo, now);
            return packageBuilder.BuildAttributionPackage();
        }

        private void WriteActivityStateI()
        {
            Util.SerializeToFileAsync(
                fileName: ActivityStateFileName,
                objectWriter: ActivityState.SerializeToStream, 
                input: _ActivityState,
                objectName: ActivityStateName)
                .Wait();
        }

        private void WriteAttributionI()
        {
            Util.SerializeToFileAsync(
                fileName: AttributionFileName, 
                objectWriter: AdjustAttribution.SerializeToStream,
                input: _Attribution,
                objectName: AttributionName)
                .Wait();
        }

        private void ReadActivityStateI()
        {
            _ActivityState = Util.DeserializeFromFileAsync(ActivityStateFileName,
                ActivityState.DeserializeFromStream, //deserialize function from Stream to ActivityState
                () => null, //default value in case of error
                ActivityStateName) // activity state name
                .Result;
        }

        private void ReadAttributionI()
        {
            _Attribution = Util.DeserializeFromFileAsync(AttributionFileName,
                AdjustAttribution.DeserializeFromStream, //deserialize function from Stream to Attribution
                () => null, //default value in case of error
                AttributionName) // attribution name
                .Result;
        }

        // return whether or not activity state should be written
        private bool UpdateActivityStateI(DateTime now)
        {
            var lastInterval = now - _ActivityState.LastActivity.Value;

            // ignore past updates
            if (lastInterval > _SessionInterval) { return false; }

            _ActivityState.LastActivity = now;

            if (lastInterval.Ticks < 0)
            {
                _Logger.Error("Time Travel!");
            }
            else
            {
                _ActivityState.SessionLenght += lastInterval;
                _ActivityState.TimeSpent += lastInterval;
            }

            return true;
        }

        private void TransferSessionPackageI()
        {
            // build Session Package
            var sessionBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, DateTime.Now);
            var sessionPackage = sessionBuilder.BuildSessionPackage();

            // send Session Package
            _PackageHandler.AddPackage(sessionPackage);
            _PackageHandler.SendFirstPackage();
        }

        private void RunDelegate(AdjustAttribution adjustAttribution)
        {
            if (_Config.AttributionChanged == null) return;
            if (adjustAttribution == null) return;

            _DeviceUtil.RunAttributionChanged(_Config.AttributionChanged, adjustAttribution);
        }

        private void LaunchDeepLink(Dictionary<string, string> jsonDict)
        {
            if (jsonDict == null) { return; }

            string deeplink;
            if (!jsonDict.TryGetValue("deeplink", out deeplink)) { return; }

            if (!Uri.IsWellFormedUriString(deeplink, UriKind.Absolute))
            {
                _Logger.Error("Malformed deeplink '{0}'", deeplink);
                return;
            }

            _Logger.Error("Wellformed deeplink '{0}'", deeplink);

            var deeplinkUri = new Uri(deeplink);
            _DeviceUtil.LauchDeeplink(deeplinkUri);
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
                _AttributionHandler.PauseSending();
            }
            else
            {
                _AttributionHandler.ResumeSending();
            }
        }

        private void UpdatePackageHandlerStatusI()
        {
            if (PausedI())
            {
                _PackageHandler.PauseSending();
            }
            else
            {
                _PackageHandler.ResumeSending();
            }
        }

        private bool PausedI()
        {
            return _State.IsOffline || !IsEnabledI();
        }

        #region Timer

        private void StartTimerI()
        {
            if (PausedI())
            {
                return;
            }

            _Timer.Resume();
        }

        private void StopTimerI()
        {
            _Timer.Suspend();
        }

        private void TimerFiredI()
        {
            if (PausedI())
            {
                StopTimerI();
                return;
            }

            _Logger.Debug("Session timer fired");
            _PackageHandler.SendFirstPackage();

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
                _Logger.Error("Event missing");
                return false;
            }

            if (!adjustEvent.IsValid())
            {
                _Logger.Error("Event not initialized correctly");
                return false;
            }

            return true;
        }
    }
}