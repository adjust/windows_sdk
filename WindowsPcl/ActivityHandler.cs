using AdjustSdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class ActivityHandler : IActivityHandler
    {
        private const string ActivityStateFileName = "AdjustIOActivityState";
        private const string AdjustPrefix = "adjust_";

        private DeviceUtil DeviceUtil { get; set; }
        private AdjustConfig AdjustConfig { get; set; }
        private DeviceInfo DeviceInfo { get; set; }
        private ActivityState ActivityState { get; set; }

        private bool Enabled { get; set; }
        private bool Offline { get; set; }
        private TimeSpan SessionInterval { get; set; }
        private TimeSpan SubsessionInterval { get; set; }
        private TimeSpan TimerInterval { get; set; }
        private TimeSpan TimerStart { get; set; }
        private IPackageHandler PackageHandler { get; set; }
        private TimerPclNet45 TimeKeeper { get; set; }
        private ActionQueue InternalQueue { get; set; }

        private static ILogger Logger = AdjustFactory.Logger;

        private ActivityHandler(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            // default values
            Enabled = true;
            
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
                Logger.Error("AdjustConfig is missing");
                return null;
            }

            if (!adjustConfig.isValid())
            {
                Logger.Error("AdjustConfig not initialized correctly");
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
            if (jsonDict == null) { return; }
            launchDeepLink(jsonDict);
            //runDelegate(jsonDict);
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled == Enabled)
            {
                if (enabled)
                {
                    Logger.Debug("Adjust already enabled");
                }
                else
                {
                    Logger.Debug("Adjust already disabled");
                }

                return;
            }
            
            Enabled = enabled;

            if (ActivityState != null)
            {
                ActivityState.Enabled = enabled;
            }

            if (enabled)
            {
                if (ToPause())
                {
                    Logger.Info("Package and attribution handler remain paused due to the SDK is offline");
                }
                else
                {
                    Logger.Info("Resuming package and attribution handler to enabled the SDK");
                }
                TrackSubsessionStart();
            }
            else
            {
                Logger.Info("Pausing package and attribution handler to disable the SDK");
                TrackSubsessionEnd();
            }
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
            if (offline == Offline)
            {
                if (offline)
                {
                    Logger.Debug("Adjust already in offline mode");
                }
                else
                {
                    Logger.Debug("Adjust already in online mode");
                }
                return;
            }
            Offline = offline;
            if (offline)
            {
                Logger.Info("Pausing package and attribution handler to put in offline mode");
            }
            else
            {
                if (ToPause())
                {
                    Logger.Info("Package and attribution handler remain paused because the SDK is disabled");
                }
                else
                {
                    Logger.Info("Resuming package and attribution handler to put in online mode");
                }
            }
            UpdateStatus();
        }

        public void OpenUrl(Uri uri)
        {
            InternalQueue.Enqueue(() => OpenUrlInternal(uri));
        }

        private void UpdateStatus()
        {
            InternalQueue.Enqueue(UpdateStatusInternal);
        }

        private void InitInternal(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            Init(adjustConfig, deviceUtil);
            DeviceInfo = DeviceUtil.GetDeviceInfo();
            Logger.LogDelegate = AdjustConfig.LogDelegate;

            TimerInterval = AdjustFactory.GetTimerInterval();
            TimerStart = AdjustFactory.GetTimerStart();
            SessionInterval = AdjustFactory.GetSessionInterval();
            SubsessionInterval = AdjustFactory.GetSubsessionInterval();

            if (AdjustConfig.Environment.Equals(AdjustConfig.EnvironmentProduction))
            {
                Logger.LogLevel = LogLevel.Assert;
            }
            else
            {
                Logger.LogLevel = AdjustConfig.LogLevel;
            }

            if (AdjustConfig.EventBufferingEnabled)
            {
                Logger.Info("Event buffering is enabled");
            }

            if (AdjustConfig.DefaultTracker != null)
            {
                Logger.Info("Default tracker: '{0}'", AdjustConfig.DefaultTracker);
            }

            //ReadAttribution
            ReadActivityState();

            PackageHandler = AdjustFactory.GetPackageHandler(this, ToPause());

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

            //checkAttributionState();

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
                WriteActivityState();

                return;
            }

            var lastInterval = now - ActivityState.LastActivity.Value;

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
            PackageHandler.PauseSending();
            StopTimer();
            if (UpdateActivityState(DateTime.Now))
            {
                WriteActivityState();
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
                Logger.Info("Buffered event{0}", eventPackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityState();
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

            var now = DateTime.Now;

            var packageBuilder = new PackageBuilder(AdjustConfig, DeviceInfo, ActivityState, now);
            packageBuilder.ExtraParameters = adjustDeepLinks;

            var clickPackage = packageBuilder.BuildClickPackage("deeplink", now);
            PackageHandler.AddPackage(clickPackage);
            PackageHandler.SendFirstPackage();
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

        private void runDelegate(AdjustAttribution adjustAttribution)
        {
            if (AdjustConfig.AttributionChanged == null) return;
            if (adjustAttribution == null) return;

            DeviceUtil.RunAttributionChanged(AdjustConfig.AttributionChanged, adjustAttribution);
        }

        private void launchDeepLink(Dictionary<string, string> jsonDict)
        {
            if (jsonDict == null) { return; }
            
            string deeplink;
            if (!jsonDict.TryGetValue("deeplink", out deeplink)) { return; }

            if (!Uri.IsWellFormedUriString(deeplink, UriKind.Absolute))
            {
                Logger.Error("Malformed deeplink '{0}'", deeplink);
                return;
            }

            var deeplinkUri = new Uri(deeplink);
            DeviceUtil.LauchDeeplink(deeplinkUri);
        }

        private void UpdateStatusInternal()
        {
            //UpdateAttributionHandlerStatus()
            UpdatePackageHandlerStatus();
        }

        private void UpdatePackageHandlerStatus()
        {
            if (PackageHandler == null) { return; }

            if (ToPause())
            {
                PackageHandler.PauseSending();
            }
            else
            {
                PackageHandler.ResumeSending();
            }
        }

        private bool ToPause()
        {
            return Offline || !IsEnabled();
        }

        #region Timer

        private void StartTimer()
        {
            if (TimeKeeper == null)
            {
                TimeKeeper = new TimerPclNet45(SystemThreadingTimer, null, TimerInterval, TimerStart);
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
                && !ActivityState.Enabled)
            {
                return;
            }
            PackageHandler.SendFirstPackage();
            if (UpdateActivityState(DateTime.Now))
            {
                WriteActivityState();
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

            if (!adjustEvent.isValid())
            {
                Logger.Error("Event not initialized correctly");
                return false;
            }

            return true;
        }
    }
}