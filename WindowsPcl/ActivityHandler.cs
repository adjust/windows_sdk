using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler : IActivityHandler
    {
        private const string ActivityStateFileName = "AdjustIOActivityState";
        private const string ActivityStateName = "Activity state";
        private const string AttributionFileName = "AdjustAttribution";
        private const string AttributionName = "Attribution";
        private const string AdjustPrefix = "adjust_";
        private const string SessionCallbackParametersFilename = "AdjustSessionCallbackParameters";
        private const string SessionPartnerParametersFilename = "AdjustSessionPartnerParameters";
        private const string SessionCallbackParametersName = "Session Callback Parameters";
        private const string SessionPartnerParametersName = "Session Partner Parameters";

        private TimeSpan BackgroundTimerInterval;

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
        private TimerCycle _ForegroundTimer;
        private TimerOnce _BackgroundTimer;
        private TimerOnce _DelayStartTimer;
        private object _ActivityStateLock = new object();
        private ISdkClickHandler _SdkClickHandler;
        private SessionParameters _SessionParameters;

        public class InternalState
        {
            internal bool Enabled;
            internal bool Offline;
            internal bool Background;
            internal bool DelayStart;
            internal bool UpdatePackages;

            public bool IsEnabled { get { return Enabled; } }
            public bool IsDisabled { get { return !Enabled; } }
            public bool IsOffline { get { return Offline; } }
            public bool IsOnline { get { return !Offline; } }
            public bool IsBackground { get { return Background; } }
            public bool IsForeground { get { return !Background; } }
            public bool IsDelayStart { get { return DelayStart; } }
            public bool IsToStartNow { get { return !DelayStart; } }
            public bool IsToUpdatePackages { get { return UpdatePackages; } }
        }

        private ActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            // default values

            // enabled by default
            _State.Enabled = true;
            // online by default
            _State.Offline = false;
            // in the background by default
            _State.Background = true;
            // delay start not configured by default
            _State.DelayStart = false;
            // does not need to update packages by default
            _State.UpdatePackages = false;

            Init(adjustConfig, deviceUtil);
            _ActionQueue.Enqueue(() => InitI());
        }

        public void Init(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            _Config = adjustConfig;
            _DeviceUtil = deviceUtil;
        }

        public static ActivityHandler GetInstance(AdjustConfig adjustConfig,
            DeviceUtil deviceUtil)
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
            _State.Background = false;

            _ActionQueue.Enqueue(() =>
            {
                DelayStartI();

                StopBackgroundTimerI();

                StartForegroundTimerI();

                _Logger.Verbose("Subsession start");

                StartI();
            });
        }

        public void TrackSubsessionEnd()
        {
            _State.Background = true;

            _ActionQueue.Enqueue(() =>
            {
                StopForegroundTimerI();

                StartBackgroundTimerI();

                _Logger.Verbose("Subsession end");

                EndI();
            });
        }

        public void FinishedTrackingActivity(ResponseData responseData)
        {
            // redirect session responses to attribution handler to check for attribution information
            if (responseData is SessionResponseData)
            {
                _AttributionHandler.CheckSessionResponse(responseData as SessionResponseData);
                return;
            }
            if (responseData is EventResponseData)
            {
                LaunchEventResponseTasksI(responseData as EventResponseData);
                return;
            }
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

            _State.Enabled = enabled;

            if (_ActivityState != null)
            {
                WriteActivityStateS(() => _ActivityState.Enabled = enabled);
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
                return _State.Enabled;
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

            _State.Offline = offline;

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
            // it is changing from an active state to a pause state
            if (pausingState)
            {
                _Logger.Info(pausingMessage);
            }
            // check if it's remaining in a pause state
            else if (IsPausedI(sdkClickHandlerOnly: false)) // safe to use internal version of paused (read only), can suffer from phantom read but not an issue
            {                      
                // including the sdk click handler
                if (IsPausedI(sdkClickHandlerOnly: true))
                {
                    _Logger.Info(remainsPausedMessage);
                }
                else
                {
                    _Logger.Info(remainsPausedMessage + ", except the Sdk Click Handler");
                }
            }
            else
            {
                // it is changing from a pause state to an active state
                _Logger.Info(unPausingMessage);
            }

            UpdateHandlersStatusAndSend();
        }

        public void OpenUrl(Uri uri)
        {
            _ActionQueue.Enqueue(() => OpenUrlI(uri));
        }

        public void AddSessionCallbackParameter(string key, string value)
        {
            _ActionQueue.Enqueue(() => AddSessionCallbackParameterI(key, value));
        }

        public void AddSessionPartnerParameter(string key, string value)
        {
            _ActionQueue.Enqueue(() => AddSessionPartnerParameterI(key, value));
        }

        public void RemoveSessionCallbackParameter(string key)
        {
            _ActionQueue.Enqueue(() => RemoveSessionCallbackParameterI(key));
        }

        public void RemoveSessionPartnerParameter(string key)
        {
            _ActionQueue.Enqueue(() => RemoveSessionPartnerParameterI(key));
        }

        public void ResetSessionCallbackParameters()
        {
            _ActionQueue.Enqueue(() => ResetSessionCallbackParametersI());
        }

        public void ResetSessionPartnerParameters()
        {
            _ActionQueue.Enqueue(() => ResetSessionPartnerParametersI());
        }

        public void LaunchSessionResponseTasks(SessionResponseData sessionResponseData)
        {
            _ActionQueue.Enqueue(() => LaunchSessionResponseTasksI(sessionResponseData));
        }

        public void LaunchAttributionResponseTasks(AttributionResponseData attributionResponseData)
        {
            _ActionQueue.Enqueue(() => LaunchAttributionResponseTasksI(attributionResponseData));
        }

        public void SetAskingAttribution(bool askingAttribution)
        {
            WriteActivityStateS(() => _ActivityState.AskingAttribution = askingAttribution);
        }

        public ActivityPackage GetAttributionPackage()
        {
            return GetAttributionPackageI();
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters,
            AdjustAttribution attribution,
            string deeplink)
        {
            return GetDeeplinkClickPackageI(extraParameters, attribution, deeplink);
        }

        public void SendFirstPackages()
        {
            _ActionQueue.Enqueue(SendFirstPackagesI);
        }

        #region private
        private void WriteActivityState()
        {
            _ActionQueue.Enqueue(WriteActivityStateI);
        }

        private void WriteAttribution()
        {
            _ActionQueue.Enqueue(WriteAttributionI);
        }

        private void UpdateHandlersStatusAndSend()
        {
            _ActionQueue.Enqueue(UpdateHandlersStatusAndSendI);
        }

        private void InitI()
        {
            _DeviceInfo = _DeviceUtil.GetDeviceInfo();
            _DeviceInfo.SdkPrefix = _Config.SdkPrefix;

            ReadAttributionI();
            ReadActivityStateI();

            _SessionParameters = new SessionParameters();
            ReadSessionCallbackParametersI();
            ReadSessionPartnerParametersI();

            TimeSpan foregroundTimerInterval = AdjustFactory.GetTimerInterval();
            TimeSpan foregroundTimerStart = AdjustFactory.GetTimerStart();
            BackgroundTimerInterval = AdjustFactory.GetTimerInterval();

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

            _ForegroundTimer = new TimerCycle(_ActionQueue, ForegroundTimerFiredI, timeInterval: foregroundTimerInterval, timeStart: foregroundTimerStart);

            // create background timer
            if (_Config.SendInBackground)
            {
                _Logger.Info("Send in background configured");
                _BackgroundTimer = new TimerOnce(_ActionQueue, BackgroundTimerFiredI);
            }

            // configure delay start timer
            if (_ActivityState == null &&
                    _Config.DelayStart.HasValue &&
                    _Config.DelayStart > TimeSpan.Zero)
            {
                _Logger.Info("Delay start configured");
                _State.DelayStart = true;
                _DelayStartTimer = new TimerOnce(_ActionQueue, SendFirstPackagesI);
            }

            Util.ConfigureHttpClient(_DeviceInfo.ClientSdk);

            _PackageHandler = AdjustFactory.GetPackageHandler(this, IsPausedI(sdkClickHandlerOnly: false));

            var attributionPackage = GetAttributionPackageI();

            _AttributionHandler = AdjustFactory.GetAttributionHandler(this,
                attributionPackage,
                IsPausedI(sdkClickHandlerOnly: false),
                _Config.HasAttributionDelegate);

            _SdkClickHandler = AdjustFactory.GetSdkClickHandler(IsPausedI(sdkClickHandlerOnly: true));

            SessionParametersActionsI(_Config.SessionParametersActions);

            StartI();
        }

        private void SessionParametersActionsI(List<Action<ActivityHandler>> sessionParametersActions)
        {
            if (sessionParametersActions == null) { return; }

            foreach (var action in sessionParametersActions)
            {
                action(this);
            }
        }

        private void StartI()
        {
            // it shouldn't start if it was disabled after a first session
            if (_ActivityState != null
                && !_ActivityState.Enabled)
            {
                return;
            }

            UpdateHandlersStatusAndSendI();

            ProcessSessionI();

            CheckAttributionStateI();
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

        private void TransferSessionPackageI()
        {
            // build Session Package
            var sessionBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, _SessionParameters, DateTime.Now);
            var sessionPackage = sessionBuilder.BuildSessionPackage(_State.IsDelayStart);

            // send Session Package
            _PackageHandler.AddPackage(sessionPackage);
            _PackageHandler.SendFirstPackage();
        }

        private void CheckAttributionStateI()
        {
            // if it's a new session
            if (_ActivityState.SubSessionCount <= 1) { return; }

            // if there is already an attribution saved and there was no attribution being asked
            if (_Attribution != null && !_ActivityState.AskingAttribution) { return; }

            _AttributionHandler.GetAttribution();
        }

        private void EndI()
        {
            // pause sending if it's not allowed to send
            if (!IsToSendI())
            {
                PauseSendingI();
            }

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

            var packageBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, _SessionParameters, now);
            ActivityPackage eventPackage = packageBuilder.BuildEventPackage(adjustEvent, _State.IsDelayStart);
            _PackageHandler.AddPackage(eventPackage);

            if (_Config.EventBufferingEnabled)
            {
                _Logger.Info("Buffered event {0}", eventPackage.Suffix);
            }
            else
            {
                _PackageHandler.SendFirstPackage();
            }

            // if it is in the background and it can send, start the background timer
            if (_Config.SendInBackground && _State.IsBackground)
            {
                StartBackgroundTimerI();
            }

            WriteActivityStateI();
        }

        #region post response
        private void LaunchEventResponseTasksI(EventResponseData eventResponseData)
        {
            // success callback
            if (eventResponseData.Success && _Config.EventTrackingSucceeded != null)
            {
                _Logger.Debug("Launching success event tracking action");
                _DeviceUtil.RunActionInForeground(() => _Config.EventTrackingSucceeded(eventResponseData.GetSuccessResponseData()));
            }
            // failure callback
            if (!eventResponseData.Success && _Config.EventTrackingFailed != null)
            {
                _Logger.Debug("Launching failed event tracking action");
                _DeviceUtil.RunActionInForeground(() => _Config.EventTrackingFailed(eventResponseData.GetFailureResponseData()));
            }
        }

        private void LaunchSessionResponseTasksI(SessionResponseData sessionResponseData)
        {
            // try to update the attribution
            var attributionUpdated = UpdateAttributionI(sessionResponseData.Attribution);

            Task task = null;
            // if attribution changed, launch attribution changed delegate
            if (attributionUpdated)
            {
                task = LaunchAttributionActionI();
            }
            // launch Session tracking listener if available
            LaunchSessionAction(sessionResponseData, task);
        }

        private void LaunchSessionAction(SessionResponseData sessionResponseData, Task previousTask)
        {
            // success callback
            if (sessionResponseData.Success && _Config.SesssionTrackingSucceeded != null)
            {
                _Logger.Debug("Launching success session tracking action");
                _DeviceUtil.RunActionInForeground(() => _Config.SesssionTrackingSucceeded(sessionResponseData.GetSessionSuccess()),
                    previousTask);
            }
            // failure callback
            if (!sessionResponseData.Success && _Config.SesssionTrackingFailed != null)
            {
                _Logger.Debug("Launching failed session tracking action");
                _DeviceUtil.RunActionInForeground(() => _Config.SesssionTrackingFailed(sessionResponseData.GetFailureResponseData()),
                    previousTask);
            }
        }

        private bool UpdateAttributionI(AdjustAttribution attribution)
        {
            if (attribution == null) { return false; }

            if (attribution.Equals(_Attribution)) { return false; }

            _Attribution = attribution;
            WriteAttributionI();

            return true;
        }

        private Task LaunchAttributionActionI()
        {
            if (_Config.AttributionChanged == null) { return null; }
            if (_Attribution == null) { return null; }

            return _DeviceUtil.RunActionInForeground(() => _Config.AttributionChanged(_Attribution));
        }

        private void LaunchAttributionResponseTasksI(AttributionResponseData attributionResponseData)
        {
            // try to update the attribution
            var attributionUpdated = UpdateAttributionI(attributionResponseData.Attribution);

            Task task = null;
            // if attribution changed, launch attribution changed delegate
            if (attributionUpdated)
            {
                task = LaunchAttributionActionI();
            }

            // if there is any, try to launch the deeplink
            LaunchDeepLink(attributionResponseData.Deeplink, task);
        }

        private void LaunchDeepLink(Uri deeplink, Task previousTask)
        {
            if (deeplink == null) { return; }
            _DeviceUtil.LauchDeeplink(deeplink, previousTask);
        }
        #endregion post response

        private void OpenUrlI(Uri uri)
        {
            if (uri == null) { return; }

            var deeplink = Uri.UnescapeDataString(uri.ToString());

            if (deeplink?.Length == 0) { return; }

            var windowsPhone80Protocol = "/Protocol?";
            if (deeplink?.StartsWith(windowsPhone80Protocol) == true)
            {
                deeplink = deeplink.Substring(windowsPhone80Protocol.Length);
            }

            var queryString = "";
            var queryStringIdx = deeplink.IndexOf("?");
            // check if '?' exists and it's not the last char
            if (queryStringIdx != -1 && queryStringIdx + 1 != deeplink.Length)
            {
                queryString = deeplink.Substring(queryStringIdx + 1);
            }

            // remove any possible fragments
            var fragmentIdx = queryString.LastIndexOf("#");
            if (fragmentIdx != -1)
            {
                queryString = queryString.Substring(0, fragmentIdx);
            }

            var queryPairs = queryString.Split('&');
            var extraParameters = new Dictionary<string, string>(queryPairs.Length);
            var attribution = new AdjustAttribution();

            foreach (var pair in queryPairs)
            {
                ReadQueryStringI(pair, extraParameters, attribution);
            }

            var clickPackage = GetDeeplinkClickPackageI(extraParameters, attribution, deeplink);

            _SdkClickHandler.SendSdkClick(clickPackage);
        }

        #region session parameters
        internal void AddSessionCallbackParameterI(string key, string value)
        {
            if (!Util.CheckParameter(key, "key", "Session Callback")) { return; }
            if (!Util.CheckParameter(value, "value", "Session Callback")) { return; }

            if (_SessionParameters.CallbackParameters == null)
            {
                _SessionParameters.CallbackParameters = new Dictionary<string, string>();
            }

            string oldValue = null;
            if (_SessionParameters.CallbackParameters.TryGetValue(key, out oldValue))
            {
                if (value.Equals(oldValue))
                {
                    _Logger.Verbose("Key {0} already present with the same value", key);
                    return;
                }

                _Logger.Warn("Key {0} will be overwritten");
                _SessionParameters.CallbackParameters.Remove(key); 
            }

            _SessionParameters.CallbackParameters.Add(key, value);

            WriteSessionCallbackParametersI();
        }

        internal void AddSessionPartnerParameterI(string key, string value)
        {
            if (!Util.CheckParameter(key, "key", "Session Partner")) { return; }
            if (!Util.CheckParameter(value, "value", "Session Partner")) { return; }

            if (_SessionParameters.PartnerParameters == null)
            {
                _SessionParameters.PartnerParameters = new Dictionary<string, string>();
            }

            string oldValue = null;
            if (_SessionParameters.PartnerParameters.TryGetValue(key, out oldValue))
            {
                if (value.Equals(oldValue))
                {
                    _Logger.Verbose("Key {0} already present with the same value", key);
                    return;
                }

                _Logger.Warn("Key {0} will be overwritten");
                _SessionParameters.PartnerParameters.Remove(key);
            }

            _SessionParameters.PartnerParameters.Add(key, value);

            WriteSessionPartnerParametersI();
        }

        internal void RemoveSessionCallbackParameterI(string key)
        {
            if (!Util.CheckParameter(key, "key", "Session Callback")) { return; }

            if (_SessionParameters.CallbackParameters == null)
            {
                _Logger.Warn("Session Callback parameters are not set");
                return;
            }

            if (!_SessionParameters.CallbackParameters.Remove(key))
            {
                _Logger.Warn("Key {0} does not exist", key);
                return;
            }

            _Logger.Debug("Key {0} will be removed", key);

            WriteSessionCallbackParametersI();
        }

        internal void RemoveSessionPartnerParameterI(string key)
        {
            if (!Util.CheckParameter(key, "key", "Session Partner")) { return; }

            if (_SessionParameters.PartnerParameters == null)
            {
                _Logger.Warn("Session Partner parameters are not set");
                return;
            }

            if (!_SessionParameters.PartnerParameters.Remove(key))
            {
                _Logger.Warn("Key {0} does not exist", key);
                return;
            }

            _Logger.Debug("Key {0} will be removed", key);

            WriteSessionPartnerParametersI();
        }

        internal void ResetSessionCallbackParametersI()
        {
            if (_SessionParameters.CallbackParameters == null)
            {
                _Logger.Warn("Session Callback parameters are not set");
            }

            _SessionParameters.CallbackParameters = null;

            WriteSessionCallbackParametersI();
        }

        internal void ResetSessionPartnerParametersI()
        {
            if (_SessionParameters.PartnerParameters == null)
            {
                _Logger.Warn("Session Partner parameters are not set");
            }

            _SessionParameters.PartnerParameters = null;

            WriteSessionPartnerParametersI();
        }
        #endregion session parameters

        private ActivityPackage GetDeeplinkClickPackageI(Dictionary<string, string> extraParameters,
            AdjustAttribution attribution,
            string deeplink)
        {
            var now = DateTime.Now;

            var clickBuilder = new PackageBuilder(_Config, _DeviceInfo, _ActivityState, _SessionParameters, now);
            clickBuilder.ExtraParameters = extraParameters;
            clickBuilder.Deeplink = deeplink;
            clickBuilder.Attribution = attribution;
            clickBuilder.ClickTime = now;

            var clickPackage = clickBuilder.BuildClickPackage("deeplink");

            return clickBuilder.BuildClickPackage("deeplink");
        }

        private void ReadQueryStringI(string queryString,
            Dictionary<string, string> extraParameters,
            AdjustAttribution attribution)
        {
            var pairComponents = queryString.Split('=');
            if (pairComponents.Length != 2) return;

            var key = pairComponents[0];
            if (!key.StartsWith(AdjustPrefix)) return;

            var value = pairComponents[1];
            if (value.Length == 0) return;

            var keyWOutPrefix = key.Substring(AdjustPrefix.Length);
            if (keyWOutPrefix.Length == 0) return;

            if (!ReadAttributionQueryStringI(attribution, keyWOutPrefix, value))
            {
                extraParameters.Add(keyWOutPrefix, value);
            }
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

        #region read write
        private void WriteActivityStateI()
        {
            WriteActivityStateS(null);
        }

        private void WriteActivityStateS(Action action)
        {
            lock (_ActivityStateLock)
            {
                action?.Invoke();

                Util.SerializeToFileAsync(
                    fileName: ActivityStateFileName,
                    objectWriter: ActivityState.SerializeToStream,
                    input: _ActivityState,
                    objectName: ActivityStateName)
                    .Wait();
            }
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

        private void WriteSessionCallbackParametersI()
        {
            Util.SerializeToFileAsync(
                fileName: SessionCallbackParametersFilename,
                objectWriter: SessionParameters.SerializeDictionaryToStream,
                input: _SessionParameters.CallbackParameters,
                objectName: SessionCallbackParametersName)
                .Wait();
        }

        private void WriteSessionPartnerParametersI()
        {
            Util.SerializeToFileAsync(
                fileName: SessionPartnerParametersFilename,
                objectWriter: SessionParameters.SerializeDictionaryToStream,
                input: _SessionParameters.PartnerParameters,
                objectName: SessionPartnerParametersName)
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

        private void ReadSessionCallbackParametersI()
        {
            _SessionParameters.CallbackParameters = Util.DeserializeFromFileAsync(SessionCallbackParametersFilename,
                SessionParameters.DeserializeDictionaryFromStream, // deserialize function from Stream to Dictionary
                () => null, // default value in case of error
                SessionCallbackParametersName) // session callback parameters name
                .Result;
        }

        private void ReadSessionPartnerParametersI()
        {
            _SessionParameters.PartnerParameters = Util.DeserializeFromFileAsync(SessionPartnerParametersFilename,
                SessionParameters.DeserializeDictionaryFromStream, // deserialize function from Stream to Dictionary
                () => null, // default value in case of error
                SessionPartnerParametersName) // session callback parameters name
                .Result;
        }
        #endregion read write

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

        private void UpdateHandlersStatusAndSendI()
        {
            // check if it should stop sending
            if (!IsToSendI())
            {
                PauseSendingI();
                return;
            }

            ResumeSendingI();

            // try to send
            if (!_Config.EventBufferingEnabled)
            {
                _PackageHandler.SendFirstPackage();
            }
        }

        private void PauseSendingI()
        {
            _AttributionHandler.PauseSending();
            _PackageHandler.PauseSending();
            _SdkClickHandler.PauseSending();
        }

        private void ResumeSendingI()
        {
            _AttributionHandler.ResumeSending();
            _PackageHandler.ResumeSending();
            _SdkClickHandler.ResumeSending();
        }

        private bool IsPausedI()
        {
            return IsPausedI(sdkClickHandlerOnly: false);
        }

        private bool IsPausedI(bool sdkClickHandlerOnly)
        {
            if (sdkClickHandlerOnly)
            {
                // sdk click handler is paused if either:
                return _State.IsOffline ||  // it's offline
                        !IsEnabledI();      // is disabled
            }
            // other handlers are paused if either:
            return _State.IsOffline ||  // it's offline
                    !IsEnabledI() ||    // is disabled
                    _State.IsDelayStart;    // is in delayed start
        }

        private bool IsToSendI()
        {
            return IsToSendI(sdkClickHandlerOnly: false);
        }

        private bool IsToSendI(bool sdkClickHandlerOnly)
        {
            // don't send when it's paused
            if (IsPausedI(sdkClickHandlerOnly))
            {
                return false;
            }

            // has the option to send in the background -> is to send
            if (_Config.SendInBackground)
            {
                return true;
            }

            // doesn't have the option -> depends on being on the background/foreground
            return _State.IsForeground;
        }

        #region delay start
        private void DelayStartI()
        {
            // it's not configured to start delayed or already finished
            if (_State.IsToStartNow)
            {
                return;
            }

            // the delay has already started
            if (isToUpdatePackagesI())
            {
                return;
            }

            // check against max start delay
            TimeSpan delayStart = _Config.DelayStart.HasValue ? _Config.DelayStart.Value : TimeSpan.Zero;
            TimeSpan maxDelayStart = AdjustFactory.GetMaxDelayStart();

            if (delayStart > maxDelayStart)
            {
                _Logger.Warn("Delay start of {0} seconds bigger than max allowed value of {1} seconds",
                    Util.SecondDisplayFormat(delayStart),
                    Util.SecondDisplayFormat(maxDelayStart));
                delayStart = maxDelayStart;
            }
            
            _Logger.Info("Waiting {0} seconds before starting first session", Util.SecondDisplayFormat(delayStart));

            _DelayStartTimer.StartIn(delayStart);

            _State.UpdatePackages = true;

            if (_ActivityState != null)
            {
                _ActivityState.UpdatePackages = true;
                WriteActivityStateI();
            }
        }

        private void SendFirstPackagesI()
        {
            if (_State.IsToStartNow)
            {
                _Logger.Info("Start delay expired or never configured");
                return;
            }

            // update packages in queue
            UpdatePackagesI();
            // no longer is in delay start
            _State.DelayStart = false;
            // cancel possible still running timer if it was called by user
            _DelayStartTimer.Cancel();
            // and release timer
            _DelayStartTimer = null;
            // update the status and try to send first package
            UpdateHandlersStatusAndSendI();
        }

        private void UpdatePackagesI()
        {
            // update activity packages
            _PackageHandler.UpdatePackages(_SessionParameters);
            // no longer needs to update packages
            _State.UpdatePackages = false;
            if (_ActivityState != null)
            {
                _ActivityState.UpdatePackages = false;
                WriteActivityStateI();
            }
        }

        private bool isToUpdatePackagesI()
        {
            if (_ActivityState != null)
            {
                return _ActivityState.UpdatePackages;
            }
            else
            {
                return _State.IsToUpdatePackages;
            }
        }

        #endregion delay start

        #region timers
        private void StartForegroundTimerI()
        {
            if (IsPausedI())
            {
                return;
            }

            _ForegroundTimer.Resume();
        }

        private void StopForegroundTimerI()
        {
            _ForegroundTimer.Suspend();
        }

        private void ForegroundTimerFiredI()
        {
            if (IsPausedI())
            {
                StopForegroundTimerI();
                return;
            }

            _Logger.Debug("Session timer fired");
            _PackageHandler.SendFirstPackage();

            if (UpdateActivityStateI(DateTime.Now))
            {
                WriteActivityStateI();
            }
        }

        private void StartBackgroundTimerI()
        {
            if (_BackgroundTimer == null)
            {
                return;
            }

            // check if it can send in the background
            if (!IsToSendI())
            {
                return;
            }

            // background timer already started
            if (_BackgroundTimer.FireIn > TimeSpan.Zero)
            {
                return;
            }

            _BackgroundTimer.StartIn(BackgroundTimerInterval);
        }

        private void StopBackgroundTimerI()
        {
            _BackgroundTimer?.Cancel();
        }

        private void BackgroundTimerFiredI()
        {
            if (IsToSendI())
            {
                _PackageHandler.SendFirstPackage();
            }
        }
        #endregion timers

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
        #endregion private
    }
}
