using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static AdjustSdk.Pcl.Constants;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler : IActivityHandler
    {
        private const string ActivityStateLegacyFileName = "AdjustIOActivityState";
        private const string ActivityStateLegacyName = "Activity state";
        private const string ActivityStateStorageName = "adjust_activity_state";
        private const string AttributionLegacyFileName = "AdjustAttribution";
        private const string AttributionLegacyName = "Attribution";
        private const string AttributionStorageName = "adjust_attribution";
        private const string AdjustPrefix = "adjust_";
        private const string SessionParametersStorageName = "adjust_session_params";

        private TimeSpan _backgroundTimerInterval;

        private ILogger _logger = AdjustFactory.Logger;
        private readonly ActionQueue _actionQueue = new ActionQueue("adjust.ActivityHandler");
        private readonly InternalState _state = new InternalState();

        private IDeviceUtil _deviceUtil;
        private AdjustConfig _config;
        private DeviceInfo _deviceInfo;
        private ActivityState _activityState;
        private AdjustAttribution _attribution;
        private TimeSpan _sessionInterval;
        private TimeSpan _subsessionInterval;
        private IPackageHandler _packageHandler;
        private IAttributionHandler _attributionHandler;
        private TimerCycle _foregroundTimer;
        private TimerOnce _backgroundTimer;
        private TimerOnce _delayStartTimer;
        private ISdkClickHandler _sdkClickHandler;
        private SessionParameters _sessionParameters;

        private string _basePath;

        public class InternalState
        {
            public bool IsEnabled { get; internal set; }
            public bool IsDisabled => !IsEnabled;
            public bool IsOffline { get; internal set; }
            public bool IsOnline => !IsOffline;
            public bool IsInBackground { get; internal set; }
            public bool IsInForeground => !IsInBackground;
            public bool IsInDelayStart { get; internal set; }
            public bool IsNotInDelayedStart => !IsInDelayStart;
            public bool ItHasToUpdatePackages { get; internal set; }
            public bool IsFirstLaunch { get; internal set; }
            public bool HasSessionResponseNotBeenProcessed { get; internal set; }
        }

        private ActivityHandler(AdjustConfig adjustConfig, IDeviceUtil deviceUtil)
        {
            Init(adjustConfig, deviceUtil);

            _logger.IsLocked = true;

            // enabled by default
            _state.IsEnabled = adjustConfig.StartEnabled ?? true;

            _state.IsOffline = adjustConfig.StartOffline;

            // in the background by default
            _state.IsInBackground = true;
            // delay start not configured by default
            _state.IsInDelayStart = false;
            // does not need to update packages by default
            _state.ItHasToUpdatePackages = false;
            // does not have the session response by default
            _state.HasSessionResponseNotBeenProcessed = false;
            
            _actionQueue.Enqueue(InitI);
        }

        public void Init(AdjustConfig adjustConfig, IDeviceUtil deviceUtil)
        {
            _config = adjustConfig;
            _deviceUtil = deviceUtil;
            _basePath = adjustConfig.BasePath;
        }

        public void Teardown(bool deleteState)
        {
            if (deleteState)
            {
                _deviceUtil.ClearAllPeristedValues();
                _deviceUtil.ClearAllPersistedObjects();
            }

            _backgroundTimer?.Teardown();
            _foregroundTimer?.Teardown();
            _delayStartTimer?.Teardown();
            _actionQueue?.Teardown();
            _packageHandler.Teardown();
            _attributionHandler.Teardown();
            _sdkClickHandler.Teardown();
            _sessionParameters?.CallbackParameters?.Clear();
            _sessionParameters?.PartnerParameters?.Clear();
            _sessionParameters = null;
            _packageHandler = null;
            _logger = null;
            _backgroundTimer = null;
            _foregroundTimer = null;
            _delayStartTimer = null;
            _deviceInfo = null;
            _sdkClickHandler = null;
            _attributionHandler = null;
            _config = null;
        }

        public static ActivityHandler GetInstance(AdjustConfig adjustConfig,
            IDeviceUtil deviceUtil)
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

        public void ApplicationActivated()
        {
            _state.IsInBackground = false;

            _actionQueue.Enqueue(() =>
            {
                DelayStartI();

                StopBackgroundTimerI();

                StartForegroundTimerI();

                _logger.Verbose("Subsession start");

                StartI();
            });
        }

        public void ApplicationDeactivated()
        {
            _state.IsInBackground = true;

            _actionQueue.Enqueue(() =>
            {
                StopForegroundTimerI();

                StartBackgroundTimerI();

                _logger.Verbose("Subsession end");

                EndI();
            });
        }

        public void TrackEvent(AdjustEvent adjustEvent)
        {
            _actionQueue.Enqueue(() =>
            {
                if (_activityState == null)
                {
                    _logger.Warn("Event tracked before first activity resumed.\n" +
                                 "If it was triggered in the Application class, it might timestamp or even send an install long before the user opens the app.\n" +
                                 "Please check https://github.com/adjust/android_sdk#can-i-trigger-an-event-at-application-launch for more information.");
                    StartI();
                }
                TrackEventI(adjustEvent);
            });
        }

        public void FinishedTrackingActivity(ResponseData responseData)
        {
            // redirect session responses to attribution handler to check for attribution information
            if (responseData is SessionResponseData)
            {
                _attributionHandler.CheckSessionResponse(responseData as SessionResponseData);
            }
            else if (responseData is SdkClickResponseData)
            {
                _attributionHandler.CheckSdkClickResponse(responseData as SdkClickResponseData);
            }
            else if (responseData is EventResponseData)
            {
                LaunchEventResponseTasks(responseData as EventResponseData);
            }
        }

        public void SetEnabled(bool enabled)
        {
            _actionQueue.Enqueue(() =>
            {
                SetEnabledI(enabled);
            });
        }

        private void SetEnabledI(bool enabled)
        {
            // compare with the saved or internal state
            if (!HasChangedStateI(
                previousState: IsEnabled(),
                newState: enabled,
                trueMessage: "Adjust already enabled", 
                falseMessage: "Adjust already disabled"))
            {
                return;
            }

            // save new enabled state in internal state
            _state.IsEnabled = enabled;

            if (_activityState == null)
            {
                UpdateStatusI(pausingState: !enabled,
                    pausingMessage: "Handlers will start as paused due to the SDK being disabled",
                    remainsPausedMessage: "Handlers will still start as paused",
                    unPausingMessage: "Handlers will start as active due to the SDK being enabled");
                return;
            }

            if (enabled)
            {
                if (!_deviceUtil.IsInstallTracked())
                {
                    _logger.Debug("SDK enabled -> Tracking new session.");
                    var now = DateTime.Now;
                    TrackNewSessionI(now);
                }

                string pushToken;
                _deviceUtil.TryTakeSimpleValue(ADJUST_PUSH_TOKEN, out pushToken);
                if (pushToken != null && pushToken != _activityState.PushToken)
                {
                    SetPushToken(pushToken);
                }
            }

            _activityState.Enabled = enabled;
            WriteActivityStateI();

            UpdateStatusI(pausingState: !enabled,
                pausingMessage: "Pausing handlers due to SDK being disabled",
                remainsPausedMessage: "Handlers remain paused",
                unPausingMessage: "Resuming handlers due to SDK being enabled");
        }

        private void UpdateStatusI(bool pausingState, string pausingMessage,
            string remainsPausedMessage, string unPausingMessage)
        {
            // it is changing from an active state to a pause state
            if (pausingState)
            {
                _logger.Info(pausingMessage);
            }
            // check if it's remaining in a pause state
            else if (IsPausedI(sdkClickHandlerOnly: false)) // safe to use internal version of paused (read only), can suffer from phantom read but not an issue
            {
                // including the sdk click handler
                if (IsPausedI(sdkClickHandlerOnly: true))
                {
                    _logger.Info(remainsPausedMessage);
                }
                else
                {
                    _logger.Info(remainsPausedMessage + ", except the Sdk Click Handler");
                }
            }
            else
            {
                // it is changing from a pause state to an active state
                _logger.Info(unPausingMessage);
            }

            UpdateHandlersStatusAndSend();
        }

        private void SetAskingAttributionI(bool askingAttribution)
        {
            _activityState.AskingAttribution = askingAttribution;

            WriteActivityStateI();
        }

        private bool HasChangedStateI(bool previousState, bool newState,
            string trueMessage, string falseMessage)
        {
            if (previousState != newState)
            {
                return true;
            }

            _logger.Debug(previousState ? trueMessage : falseMessage);

            return false;
        }

        public bool IsEnabled()
        {
            return IsEnabledI();
        }

        private bool IsEnabledI()
        {
            return _activityState?.Enabled ?? _state.IsEnabled;
        }

        public void SetOfflineMode(bool offline)
        {
            _actionQueue.Enqueue(() =>
            {
                SetOfflineModeI(offline);
            });
        }

        private void SetOfflineModeI(bool offline)
        {
            // compare with the internal state
            if (!HasChangedStateI(
                previousState: _state.IsOffline,
                newState: offline,
                trueMessage: "Adjust already in offline mode",
                falseMessage: "Adjust already in online mode"))
            {
                return;
            }

            _state.IsOffline = offline;

            if (_activityState == null)
            {
                UpdateStatusI(pausingState: offline,
                    pausingMessage: "Handlers will start paused due to SDK being offline",
                    remainsPausedMessage: "Handlers will still start as paused",
                    unPausingMessage: "Handlers will start as active due to SDK being online");
                return;
            }

            UpdateStatusI(
                pausingState: offline,
                pausingMessage: "Pausing handlers to put SDK offline mode",
                remainsPausedMessage: "Handlers remain paused",
                unPausingMessage: "Resuming handlers to put SDK in online mode");
        }

        public void OpenUrl(Uri uri, DateTime clickTime)
        {
            _actionQueue.Enqueue(() => OpenUrlI(uri, clickTime));
        }

        public void AddSessionCallbackParameter(string key, string value)
        {
            _actionQueue.Enqueue(() => AddSessionCallbackParameterI(key, value));
        }

        public void AddSessionPartnerParameter(string key, string value)
        {
            _actionQueue.Enqueue(() => AddSessionPartnerParameterI(key, value));
        }

        public void RemoveSessionCallbackParameter(string key)
        {
            _actionQueue.Enqueue(() => RemoveSessionCallbackParameterI(key));
        }

        public void RemoveSessionPartnerParameter(string key)
        {
            _actionQueue.Enqueue(() => RemoveSessionPartnerParameterI(key));
        }

        public void ResetSessionCallbackParameters()
        {
            _actionQueue.Enqueue(ResetSessionCallbackParametersI);
        }

        public void ResetSessionPartnerParameters()
        {
            _actionQueue.Enqueue(ResetSessionPartnerParametersI);
        }
        
        public void SetPushToken(string pushToken)
        {
            _actionQueue.Enqueue(() => {
                if (_activityState == null)
                {
                    // No install has been tracked so far.
                    // Push token is saved, ready for the session package to pick it up.
                    return;
                }
                else
                {
                    SetPushTokenI(pushToken);
                }
            });
        }

        public void LaunchEventResponseTasks(EventResponseData eventResponseData)
        {
            _actionQueue.Enqueue(() => LaunchEventResponseTasksI(eventResponseData));
        }

        public void LaunchSessionResponseTasks(SessionResponseData sessionResponseData)
        {
            _actionQueue.Enqueue(() => LaunchSessionResponseTasksI(sessionResponseData));
        }

        public void LaunchSdkClickResponseTasks(SdkClickResponseData sdkClickResponseData)
        {
            _actionQueue.Enqueue(() => LaunchSdkClickResponseTasksI(sdkClickResponseData));
        }

        public void LaunchAttributionResponseTasks(AttributionResponseData attributionResponseData)
        {
            _actionQueue.Enqueue(() => LaunchAttributionResponseTasksI(attributionResponseData));
        }

        public void SetAskingAttribution(bool askingAttribution)
        {
            _actionQueue.Enqueue(() => SetAskingAttributionI(askingAttribution));
        }

        public ActivityPackage GetAttributionPackage()
        {
            return GetAttributionPackageI();
        }

        public ActivityPackage GetDeeplinkClickPackage(Dictionary<string, string> extraParameters,
            AdjustAttribution attribution, string deeplink, DateTime clickTime)
        {
            return GetDeeplinkClickPackageI(extraParameters, attribution, deeplink, clickTime);
        }

        public void SendFirstPackages()
        {
            _actionQueue.Enqueue(SendFirstPackagesI);
        }

        public string GetBasePath()
        {
            return _basePath;
        }

        #region private
        private void WriteActivityState()
        {
            _actionQueue.Enqueue(WriteActivityStateI);
        }

        private void WriteAttribution()
        {
            _actionQueue.Enqueue(WriteAttributionI);
        }

        private void UpdateHandlersStatusAndSend()
        {
            _actionQueue.Enqueue(UpdateHandlersStatusAndSendI);
        }

        private void InitI()
        {
            _deviceInfo = _deviceUtil.GetDeviceInfo();
            _deviceInfo.SdkPrefix = _config.SdkPrefix;
            
            ReadAttributionI();
            ReadActivityStateI();
            ReadSessionParametersI();

            if (_config.StartEnabled.HasValue)
            {
                if (_config.PreLaunchActions == null)
                {
                    _config.PreLaunchActions = new List<Action<ActivityHandler>>();
                }

                _config.PreLaunchActions.Add(activityHandler =>
                {
                    activityHandler.SetEnabledI(_config.StartEnabled.Value);
                });
            }

            // first launch if activity state is null
            if (_activityState != null)
            {
                _state.IsEnabled = _activityState.Enabled;
                _state.ItHasToUpdatePackages = _activityState.UpdatePackages;
                _state.IsFirstLaunch = false;
            }
            else
            {
                _state.IsFirstLaunch = true;
            }

            TimeSpan foregroundTimerInterval = AdjustFactory.GetTimerInterval();
            TimeSpan foregroundTimerStart = AdjustFactory.GetTimerStart();
            _backgroundTimerInterval = AdjustFactory.GetTimerInterval();

            _sessionInterval = AdjustFactory.GetSessionInterval();
            _subsessionInterval = AdjustFactory.GetSubsessionInterval();

            if (_config.EventBufferingEnabled)
            {
                _logger.Info("Event buffering is enabled");
            }

            if (_config.DefaultTracker != null)
            {
                _logger.Info("Default tracker: '{0}'", _config.DefaultTracker);
            }

            if (_activityState != null)
            {
                string pushToken;
                _deviceUtil.TryTakeSimpleValue(ADJUST_PUSH_TOKEN, out pushToken);
                SetPushToken(pushToken);
            }

            _foregroundTimer = new TimerCycle(_actionQueue, ForegroundTimerFiredI, timeInterval: foregroundTimerInterval, timeStart: foregroundTimerStart);

            // create background timer
            if (_config.SendInBackground)
            {
                _logger.Info("Send in background configured");
                _backgroundTimer = new TimerOnce(_actionQueue, BackgroundTimerFiredI);
            }

            // configure delay start timer
            if (_activityState == null &&
                    _config.DelayStart.HasValue &&
                    _config.DelayStart > TimeSpan.Zero)
            {
                _logger.Info("Delay start configured");
                _state.IsInDelayStart = true;
                _delayStartTimer = new TimerOnce(_actionQueue, SendFirstPackagesI);
            }

            Util.UserAgent = _config.UserAgent;

            Util.ConfigureHttpClient(_deviceInfo.ClientSdk);

            _packageHandler = AdjustFactory.GetPackageHandler(this, _deviceUtil, IsPausedI(sdkClickHandlerOnly: false), _config.UserAgent);

            var attributionPackage = GetAttributionPackageI();

            _attributionHandler = AdjustFactory.GetAttributionHandler(this,
                attributionPackage,
                IsPausedI(sdkClickHandlerOnly: false));

            _sdkClickHandler = AdjustFactory.GetSdkClickHandler(this, IsPausedI(sdkClickHandlerOnly: true), _config.UserAgent);

            // fail-safe check if app crashed during delay state, to update packages in queue
            if (IsToUpdatePackagesI())
            {
                UpdatePackagesI();
            }

            PreLaunchActionsI(_config.PreLaunchActions);

            //StartI();
        }

        private void PreLaunchActionsI(List<Action<ActivityHandler>> preLaunchActions)
        {
            if (preLaunchActions == null) { return; }

            foreach (var action in preLaunchActions)
            {
                action(this);
            }
        }

        private void StartI()
        {
            // it shouldn't start if it was disabled after a first session
            if (_activityState != null && !_activityState.Enabled)
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
            if (_activityState == null)
            {
                // create fresh activity state
                _activityState = new ActivityState();                
                
                //_activityState.PushToken = _config.PushToken;
                string pushToken;
                _deviceUtil.TryTakeSimpleValue(ADJUST_PUSH_TOKEN, out pushToken);
                _activityState.PushToken = pushToken;

                if (_state.IsEnabled)
                {
                    _activityState.SessionCount = 1; // first session
                    TransferSessionPackageI();
                }

                _activityState.ResetSessionAttributes(now);
                _activityState.Enabled = _state.IsEnabled;
                _activityState.UpdatePackages = _state.ItHasToUpdatePackages;

                WriteActivityStateI();

                // remove old push token from device
                _deviceUtil.ClearSimpleValue(ADJUST_PUSH_TOKEN);

                return;
            }

            var lastInterval = now - _activityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                _logger.Error("Time Travel!");
                _activityState.LastActivity = now;
                WriteActivityStateI();
                return;
            }

            // new session
            if (lastInterval > _sessionInterval)
            {
                TrackNewSessionI(now);
                return;
            }

            // new subsession
            if (lastInterval > _subsessionInterval)
            {
                _activityState.SubSessionCount++;
                _activityState.SessionLenght += lastInterval;
                _activityState.LastActivity = now;

                WriteActivityStateI();
                _logger.Info("Started subsession {0} of session {1}",
                    _activityState.SubSessionCount, _activityState.SessionCount);
                return;
            }

            _logger.Verbose("Time span since last activity too short for a new subsession");
        }

        private void TrackNewSessionI(DateTime now)
        {
            var lastInterval = now - _activityState.LastActivity.Value;

            _activityState.SessionCount++;
            _activityState.LastInterval = lastInterval;

            TransferSessionPackageI();

            _activityState.ResetSessionAttributes(now);
            WriteActivityStateI();
        }

        private void TransferSessionPackageI()
        {
            // build Session Package
            var sessionBuilder = new PackageBuilder(_config, _deviceInfo, _activityState, _sessionParameters, DateTime.Now);
            var sessionPackage = sessionBuilder.BuildSessionPackage(_state.IsInDelayStart);

            // send Session Package
            _packageHandler.AddPackage(sessionPackage);
            _packageHandler.SendFirstPackage();
        }

        private void CheckAttributionStateI()
        {
            // if it's a new session
            if (!CheckActivityStateI(_activityState)) { return; }

            // if it's the first launch
            if (_state.IsFirstLaunch)
            {
                // and it hasn't received the session response
                if (!_state.HasSessionResponseNotBeenProcessed)
                {
                    return;
                }
            }

            // if there is already an attribution saved and there was no attribution being asked
            if (_attribution != null && !_activityState.AskingAttribution) { return; }

            _attributionHandler.GetAttribution();
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
            if (!CheckPurchaseIdI(adjustEvent.PurchaseId)) { return; }

            var now = DateTime.Now;

            _activityState.EventCount++;
            UpdateActivityStateI(now);

            var packageBuilder = new PackageBuilder(_config, _deviceInfo, _activityState, _sessionParameters, now);
            ActivityPackage eventPackage = packageBuilder.BuildEventPackage(adjustEvent, _state.IsInDelayStart);
            _packageHandler.AddPackage(eventPackage);

            if (_config.EventBufferingEnabled)
            {
                _logger.Info("Buffered event {0}", eventPackage.Suffix);
            }
            else
            {
                _packageHandler.SendFirstPackage();
            }

            // if it is in the background and it can send, start the background timer
            if (_config.SendInBackground && _state.IsInBackground)
            {
                StartBackgroundTimerI();
            }

            WriteActivityStateI();
        }

        #region post response
        private void LaunchEventResponseTasksI(EventResponseData eventResponseData)
        {
            // try to update adid from response
            UpdateAdidI(eventResponseData.Adid);

            // success callback
            if (eventResponseData.Success && _config.EventTrackingSucceeded != null)
            {
                _logger.Debug("Launching success event tracking action");
                _deviceUtil.RunActionInForeground(() => _config?.EventTrackingSucceeded(eventResponseData.GetSuccessResponseData()));
            }
            // failure callback
            if (!eventResponseData.Success && _config.EventTrackingFailed != null)
            {
                _logger.Debug("Launching failed event tracking action");
                _deviceUtil.RunActionInForeground(() => _config.EventTrackingFailed(eventResponseData.GetFailureResponseData()));
            }
        }

        private void LaunchSessionResponseTasksI(SessionResponseData sessionResponseData)
        {
            // try to update adid from response
            UpdateAdidI(sessionResponseData.Adid);

            // try to update the attribution
            var attributionUpdated = UpdateAttributionI(sessionResponseData.Attribution);

            Task task = null;
            // if attribution changed, launch attribution changed delegate
            if (attributionUpdated)
            {
                task = LaunchAttributionActionI();
            }

            if (sessionResponseData.Success)
            {
                _deviceUtil.SetInstallTracked();
            }

            // launch Session tracking listener if available
            LaunchSessionAction(sessionResponseData, task);

            // mark session response has been proccessed
            _state.HasSessionResponseNotBeenProcessed = true;
        }

        private void LaunchSdkClickResponseTasksI(SdkClickResponseData sdkClickResponseData)
        {
            // try to update adid from response
            UpdateAdidI(sdkClickResponseData.Adid);

            // try to update the attribution
            var attributionUpdated = UpdateAttributionI(sdkClickResponseData.Attribution);

            // if attribution changed, launch attribution changed delegate
            if (attributionUpdated)
            {
                LaunchAttributionActionI();
            }
        }

        private void LaunchSessionAction(SessionResponseData sessionResponseData, Task previousTask)
        {
            // success callback
            if (sessionResponseData.Success && _config.SesssionTrackingSucceeded != null)
            {
                _logger.Debug("Launching success session tracking action");
                _deviceUtil.RunActionInForeground(() => _config.SesssionTrackingSucceeded(sessionResponseData.GetSessionSuccess()),
                    previousTask);
            }
            // failure callback
            if (!sessionResponseData.Success && _config.SesssionTrackingFailed != null)
            {
                _logger.Debug("Launching failed session tracking action");
                _deviceUtil.RunActionInForeground(() => _config.SesssionTrackingFailed(sessionResponseData.GetFailureResponseData()),
                    previousTask);
            }
        }

        private void UpdateAdidI(string adid)
        {
            if (adid == null)
            {
                return;
            }

            if (adid == _activityState.Adid)
            {
                return;
            }

            _activityState.Adid = adid;
            WriteActivityStateI();
        }

        private bool UpdateAttributionI(AdjustAttribution attribution)
        {
            if (attribution == null) { return false; }

            if (attribution.Equals(_attribution)) { return false; }

            _attribution = attribution;
            WriteAttributionI();

            return true;
        }

        private Task LaunchAttributionActionI()
        {
            if (_config.AttributionChanged == null) { return null; }
            if (_attribution == null) { return null; }

            return _deviceUtil.RunActionInForeground(() => _config.AttributionChanged(_attribution));
        }

        private void LaunchAttributionResponseTasksI(AttributionResponseData attributionResponseData)
        {
            // try to update adid from response
            UpdateAdidI(attributionResponseData.Adid);

            // try to update the attribution
            var attributionUpdated = UpdateAttributionI(attributionResponseData.Attribution);

            Task task = null;
            // if attribution changed, launch attribution changed delegate
            if (attributionUpdated)
            {
                task = LaunchAttributionActionI();
            }

            // if there is any, try to launch the deeplink
            PrepareDeeplinkI(attributionResponseData.Deeplink, task);
        }

        private void PrepareDeeplinkI(Uri deeplink, Task previousTask)
        {
            if (deeplink == null) { return; }

            _logger.Info($"Deferred deeplink received {deeplink}");

            _deviceUtil.RunActionInForeground(() =>
            {
                if (_config == null) { return; }

                bool toLaunchDeeplink = true;
                if (_config.DeeplinkResponse != null)
                {
                    toLaunchDeeplink = _config.DeeplinkResponse(deeplink);
                }

                if (toLaunchDeeplink)
                {
                    _deviceUtil.LauchDeeplink(deeplink, previousTask);
                }
            }, previousTask);
        }
        #endregion post response

        private void OpenUrlI(Uri uri, DateTime clickTime)
        {
            if (!IsEnabledI()) { return; }
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

            var clickPackage = GetDeeplinkClickPackageI(extraParameters, attribution, deeplink, clickTime);

            _sdkClickHandler.SendSdkClick(clickPackage);
        }

        #region session parameters
        internal void AddSessionCallbackParameterI(string key, string value)
        {
            if (!Util.CheckParameter(key, "key", "Session Callback")) { return; }
            if (!Util.CheckParameter(value, "value", "Session Callback")) { return; }

            if (_sessionParameters.CallbackParameters == null)
            {
                _sessionParameters.CallbackParameters = new Dictionary<string, string>();
            }

            string oldValue = null;
            if (_sessionParameters.CallbackParameters.TryGetValue(key, out oldValue))
            {
                if (value.Equals(oldValue))
                {
                    _logger.Verbose("Key {0} already present with the same value", key);
                    return;
                }

                _logger.Warn("Key {0} will be overwritten", key);
            }

            _sessionParameters.CallbackParameters.AddSafe(key, value);

            WriteSessionParametersI();
        }

        internal void AddSessionPartnerParameterI(string key, string value)
        {
            if (!Util.CheckParameter(key, "key", "Session Partner")) { return; }
            if (!Util.CheckParameter(value, "value", "Session Partner")) { return; }

            if (_sessionParameters.PartnerParameters == null)
            {
                _sessionParameters.PartnerParameters = new Dictionary<string, string>();
            }

            string oldValue = null;
            if (_sessionParameters.PartnerParameters.TryGetValue(key, out oldValue))
            {
                if (value.Equals(oldValue))
                {
                    _logger.Verbose("Key {0} already present with the same value", key);
                    return;
                }

                _logger.Warn("Key {0} will be overwritten", key);
            }

            _sessionParameters.PartnerParameters.AddSafe(key, value);

            WriteSessionParametersI();
        }

        internal void RemoveSessionCallbackParameterI(string key)
        {
            if (!Util.CheckParameter(key, "key", "Session Callback")) { return; }

            if (_sessionParameters.CallbackParameters == null)
            {
                _logger.Warn("Session Callback parameters are not set");
                return;
            }

            if (!_sessionParameters.CallbackParameters.Remove(key))
            {
                _logger.Warn("Key {0} does not exist", key);
                return;
            }

            _logger.Debug("Key {0} will be removed", key);

            WriteSessionParametersI();
        }

        internal void RemoveSessionPartnerParameterI(string key)
        {
            if (!Util.CheckParameter(key, "key", "Session Partner")) { return; }

            if (_sessionParameters.PartnerParameters == null)
            {
                _logger.Warn("Session Partner parameters are not set");
                return;
            }

            if (!_sessionParameters.PartnerParameters.Remove(key))
            {
                _logger.Warn("Key {0} does not exist", key);
                return;
            }

            _logger.Debug("Key {0} will be removed", key);

            WriteSessionParametersI();
        }

        internal void ResetSessionCallbackParametersI()
        {
            if (_sessionParameters.CallbackParameters == null)
            {
                _logger.Warn("Session Callback parameters are not set");
            }

            _sessionParameters.CallbackParameters = null;

            WriteSessionParametersI();
        }

        internal void ResetSessionPartnerParametersI()
        {
            if (_sessionParameters.PartnerParameters == null)
            {
                _logger.Warn("Session Partner parameters are not set");
            }

            _sessionParameters.PartnerParameters = null;

            WriteSessionParametersI();
        }
        #endregion session parameters

        private void SetPushTokenI(string pushToken)
        {
            if (!CheckActivityStateI(_activityState)) { return; }
            if (!IsEnabledI()) { return; }

            if (pushToken == null) { return; }
            if (pushToken == _activityState.PushToken) { return; }

            // save new push token
            _activityState.PushToken = pushToken;
            WriteActivityStateI();

            // build info package
            var now = DateTime.Now;
            var infoBuilder = new PackageBuilder(_config, _deviceInfo, _activityState, now);
            var infoPackage = infoBuilder.BuildInfoPackage("push");

            // send info package
            _packageHandler.AddPackage(infoPackage);

            // If push token was cached, remove it.
            _deviceUtil.ClearSimpleValue(ADJUST_PUSH_TOKEN);

            if (_config.EventBufferingEnabled)
            {
                _logger.Info("Buffered event {0}", infoPackage.Suffix);
            }
            else
            {
                _packageHandler.SendFirstPackage();
            }
        }

        private ActivityPackage GetDeeplinkClickPackageI(Dictionary<string, string> extraParameters,
            AdjustAttribution attribution, string deeplink, DateTime clickTime)
        {
            //if (_activityState.LastActivity.HasValue)
            //{
            //    _activityState.LastInterval = clickTime - _activityState.LastActivity.Value;
            //}

            var clickBuilder = new PackageBuilder(_config, _deviceInfo, _activityState, _sessionParameters, clickTime);
            clickBuilder.ExtraParameters = extraParameters;
            clickBuilder.Deeplink = deeplink;
            clickBuilder.Attribution = attribution;
            clickBuilder.ClickTime = clickTime;
            
            return clickBuilder.BuildClickPackage(DEEPLINK);
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
                extraParameters.AddSafe(keyWOutPrefix, value);
            }
        }

        private bool ReadAttributionQueryStringI(AdjustAttribution attribution,
            string key,
            string value)
        {
            if (key.Equals(TRACKER))
            {
                attribution.TrackerName = value;
                return true;
            }

            if (key.Equals(CAMPAIGN))
            {
                attribution.Campaign = value;
                return true;
            }

            if (key.Equals(ADGROUP))
            {
                attribution.Adgroup = value;
                return true;
            }

            if (key.Equals(CREATIVE))
            {
                attribution.Creative = value;
                return true;
            }

            return false;
        }

        public string GetAdid()
        {
            return _activityState?.Adid;
        }

        public AdjustAttribution GetAttribution()
        {
            return _attribution;
        }

        private ActivityPackage GetAttributionPackageI()
        {
            var now = DateTime.Now;
            var packageBuilder = new PackageBuilder(_config, _deviceInfo, _activityState, now);
            return packageBuilder.BuildAttributionPackage();
        }
        
        private void WriteActivityStateI()
        {
            _deviceUtil.PersistObject(ActivityStateStorageName, ActivityState.ToDictionary(_activityState));
        }
        
        private void WriteAttributionI()
        {
            _deviceUtil.PersistObject(AttributionStorageName, AdjustAttribution.ToDictionary(_attribution));
        }

        private void WriteSessionParametersI()
        {
            //_deviceUtil.PersistObject(SessionParametersStorageName, SessionParameters.ToDictionary(_sessionParameters));
            var sessionParamsMap = SessionParameters.ToDictionary(_sessionParameters);
            string sessionParamsJson = JsonConvert.SerializeObject(sessionParamsMap);

            bool sessionParamsPersisted = _deviceUtil.PersistValue(SessionParametersStorageName, sessionParamsJson);
            if (!sessionParamsPersisted)
                _logger.Error("Error. Session Parameters not persisted on device within specific time frame (60 seconds default).");
        }

        private void ReadActivityStateI()
        {
            Dictionary<string, object> activityStateObjectMap;
            if (_deviceUtil.TryTakeObject(ActivityStateStorageName, out activityStateObjectMap))
            {
                _activityState = ActivityState.FromDictionary(activityStateObjectMap);
            }
            else
            {
                var activityStateLegacyFile = _deviceUtil.GetLegacyStorageFile(ActivityStateLegacyFileName).Result;

                // if activity state is not found, try to read it from the legacy file
                _activityState = Util.DeserializeFromFileAsync(
                        file: activityStateLegacyFile,
                        objectReader: ActivityState.DeserializeFromStreamLegacy, //deserialize function from Stream to ActivityState
                        defaultReturn: () => null, //default value in case of error
                        objectName: ActivityStateLegacyName) // activity state name
                    .Result;

                // if it's successfully read from legacy source, store it using new persistance
                // and then delete the old file
                if (_activityState != null)
                {
                    _logger.Info("Legacy ActivityState File found and successfully read.");

                    WriteActivityStateI();

                    // check whether the activity state is persisted, and THEN delete
                    if (_deviceUtil.TryTakeObject(ActivityStateStorageName, out activityStateObjectMap))
                    {
                        activityStateLegacyFile.DeleteAsync();
                        _logger.Info("Legacy ActivityState File deleted.");
                    }
                }
            }
        }

        private void ReadAttributionI()
        {
            Dictionary<string, object> attributionObjectMap;
            if (_deviceUtil.TryTakeObject(AttributionStorageName, out attributionObjectMap))
            {
                _attribution = AdjustAttribution.FromDictionary(attributionObjectMap);
            }
            else
            {
                var attributionLegacyFile = _deviceUtil.GetLegacyStorageFile(AttributionLegacyFileName).Result;

                // if attribution is not found, try to read it from the legacy file
                _attribution = Util.DeserializeFromFileAsync(
                        file: attributionLegacyFile,
                        objectReader: AdjustAttribution.DeserializeFromStreamLegacy, //deserialize function from Stream to Attribution
                        defaultReturn: () => null, //default value in case of error
                        objectName: AttributionLegacyName) // attribution name
                    .Result;

                // if it's successfully read from legacy source, store it using new persistance
                // and then delete the old file
                if (_attribution != null)
                {
                    _logger.Info("Legacy Attribution File found and successfully read.");

                    WriteAttributionI();

                    // check whether the attribution is persisted, and THEN delete
                    if (_deviceUtil.TryTakeObject(AttributionStorageName, out attributionObjectMap))
                    {
                        attributionLegacyFile.DeleteAsync();
                        _logger.Info("Legacy Attribution File deleted.");
                    }
                }
            }
        }

        private void ReadSessionParametersI()
        {
            string sessionParamsJson;
            _sessionParameters = _deviceUtil.TryTakeValue(SessionParametersStorageName, out sessionParamsJson) ? 
                SessionParameters.FromDictionary(JsonConvert.DeserializeObject<Dictionary<string, object>>(sessionParamsJson)) : 
                new SessionParameters();
        }
        
        // return whether or not activity state should be written
        private bool UpdateActivityStateI(DateTime now)
        {
            if (!CheckActivityStateI(_activityState)) { return false; }

            var lastInterval = now - _activityState.LastActivity.Value;

            // ignore past updates
            if (lastInterval > _sessionInterval) { return false; }

            _activityState.LastActivity = now;

            if (lastInterval.Ticks < 0)
            {
                _logger.Error("Time Travel!");
            }
            else
            {
                _activityState.SessionLenght += lastInterval;
                _activityState.TimeSpent += lastInterval;
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

            // try to send if it's the first launch and it hasn't received the session response
            //  even if event buffering is enabled
            if (_state.IsFirstLaunch && _state.HasSessionResponseNotBeenProcessed)
            {
                _packageHandler.SendFirstPackage();
            }

            // try to send
            if (!_config.EventBufferingEnabled)
            {
                _packageHandler.SendFirstPackage();
            }
        }

        private void PauseSendingI()
        {
            _attributionHandler.PauseSending();
            _packageHandler.PauseSending();

            // the conditions to pause the sdk click handler are less restrictive
            // it's possible for the sdk click handler to be active while others are paused
            if (!IsToSendI(true))
            {
                _sdkClickHandler.PauseSending();
            }
            else
            {
                _sdkClickHandler.ResumeSending();
            }
        }

        private void ResumeSendingI()
        {
            _attributionHandler.ResumeSending();
            _packageHandler.ResumeSending();
            _sdkClickHandler.ResumeSending();
        }

        private bool CheckPurchaseIdI(string purchaseId)
        {
            if (string.IsNullOrEmpty(purchaseId))
                return true;  // no purchase ID given

            if (_activityState.FindPurchaseId(purchaseId))
            {
                _logger.Info("Skipping duplicated purchase ID '{0}'", purchaseId);
                return false; // purchase ID found -> used already
            }

            _activityState.AddPurchaseId(purchaseId);
            _logger.Verbose("Added purchase ID '{0}'", purchaseId);
            // activity state will get written by caller
            return true;
        }

        private bool CheckActivityStateI(ActivityState activityState)
        {
            if (activityState == null)
            {
                _logger.Error("Missing activity state");
                return false;
            }
            return true;
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
                return _state.IsOffline ||  // it's offline
                        !IsEnabledI();      // is disabled
            }
            // other handlers are paused if either:
            return _state.IsOffline ||  // it's offline
                    !IsEnabledI() ||    // is disabled
                    _state.IsInDelayStart;    // is in delayed start
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
            if (_config.SendInBackground)
            {
                return true;
            }

            // doesn't have the option -> depends on being on the background/foreground
            return _state.IsInForeground;
        }

        #region delay start
        private void DelayStartI()
        {
            // it's not configured to start delayed or already finished
            if (_state.IsNotInDelayedStart)
            {
                return;
            }

            // the delay has already started
            if (IsToUpdatePackagesI())
            {
                return;
            }

            // check against max start delay
            TimeSpan delayStart = _config.DelayStart.HasValue ? _config.DelayStart.Value : TimeSpan.Zero;
            TimeSpan maxDelayStart = AdjustFactory.GetMaxDelayStart();

            if (delayStart > maxDelayStart)
            {
                _logger.Warn("Delay start of {0} seconds bigger than max allowed value of {1} seconds",
                    Util.SecondDisplayFormat(delayStart),
                    Util.SecondDisplayFormat(maxDelayStart));
                delayStart = maxDelayStart;
            }
            
            _logger.Info("Waiting {0} seconds before starting first session", Util.SecondDisplayFormat(delayStart));

            _delayStartTimer.StartIn(delayStart);

            _state.ItHasToUpdatePackages = true;

            if (_activityState != null)
            {
                _activityState.UpdatePackages = true;
                WriteActivityStateI();
            }
        }

        private void SendFirstPackagesI()
        {
            if (_state.IsNotInDelayedStart)
            {
                _logger.Info("Start delay expired or never configured");
                return;
            }

            // update packages in queue
            UpdatePackagesI();
            // no longer is in delay start
            _state.IsInDelayStart = false;
            // cancel possible still running timer if it was called by user
            _delayStartTimer.Cancel();
            // and release timer
            _delayStartTimer = null;
            // update the status and try to send first package
            UpdateHandlersStatusAndSendI();
        }

        private void UpdatePackagesI()
        {
            // update activity packages
            _packageHandler.UpdatePackages(_sessionParameters);
            // no longer needs to update packages
            _state.ItHasToUpdatePackages = false;
            if (_activityState != null)
            {
                _activityState.UpdatePackages = false;
                WriteActivityStateI();
            }
        }

        private bool IsToUpdatePackagesI()
        {
            return _activityState?.UpdatePackages ?? _state.ItHasToUpdatePackages;
        }

        #endregion delay start

        #region timers
        private void StartForegroundTimerI()
        {
            if (!IsEnabledI())
            {
                return;
            }

            _foregroundTimer.Resume();
        }

        private void StopForegroundTimerI()
        {
            _foregroundTimer.Suspend();
        }

        private void ForegroundTimerFiredI()
        {
            if (IsPausedI())
            {
                StopForegroundTimerI();
                return;
            }

            _logger.Debug("Session timer fired");
            _packageHandler.SendFirstPackage();

            if (UpdateActivityStateI(DateTime.Now))
            {
                WriteActivityStateI();
            }
        }

        private void StartBackgroundTimerI()
        {
            if (_backgroundTimer == null)
            {
                return;
            }

            // check if it can send in the background
            if (!IsToSendI())
            {
                return;
            }

            // background timer already started
            if (_backgroundTimer.FireIn > TimeSpan.Zero)
            {
                return;
            }

            _backgroundTimer.StartIn(_backgroundTimerInterval);
        }

        private void StopBackgroundTimerI()
        {
            _backgroundTimer?.Cancel();
        }

        private void BackgroundTimerFiredI()
        {
            if (IsToSendI())
            {
                _packageHandler.SendFirstPackage();
            }
        }
        #endregion timers

        private bool CheckEventI(AdjustEvent adjustEvent)
        {
            if (adjustEvent == null)
            {
                _logger.Error("Event missing");
                return false;
            }

            if (!adjustEvent.IsValid())
            {
                _logger.Error("Event not initialized correctly");
                return false;
            }

            return true;
        }
        #endregion private
    }
}
