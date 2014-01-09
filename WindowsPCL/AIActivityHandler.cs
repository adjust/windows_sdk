using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    public class AIActivityHandler
    {
        private const string ActivityStateFilename = "AdjustIOActivityState";
        private static readonly TimeSpan SessionInterval =      new TimeSpan(0, 30,  0);    // 30 minutes
        private static readonly TimeSpan SubSessionInterval =   new TimeSpan(0,  0,  1);      // 1 second 
        private static readonly TimeSpan TimerInterval =        new TimeSpan(0,  1,  0);      // 1 minute

        private AIPackageHandler PackageHandler = new AIPackageHandler();
        private AIActivityState ActivityState = null;
        private AITimer TimeKeeper = null;

        private string AppToken;
        private string MacSha1; //todo generate a sha1?
        private string MacShortMd5;
        private string IdForAdvertisers;
        private string FbAttributionId;
        private string UserAgent;
        private string ClientSdk;
        private bool IsTrackingEnabled;
        private string DeviceId;

        public string Environment { get; private set; }
        public static bool IsBufferedEventsEnabled { get; private set; }

        public delegate string GetMd5Hash(string input);

        private GetMd5Hash Md5Function;

        //private static NitoTaskQueue InternalQueue;
        private static AIActionQueue InternalQueue;

        public class DeviceUtil
        {
            public string DeviceId;
            public string ClientSdk;
            public string UserAgent;
            public GetMd5Hash Md5Function;
        }

        public AIActivityHandler(string appToken, DeviceUtil deviceUtil)
        {
            InternalQueue = new AIActionQueue("io.adjust.ActivityQueue");
            Environment = "unknown";
            ClientSdk = deviceUtil.ClientSdk;
            DeviceId = deviceUtil.DeviceId;
            UserAgent = deviceUtil.UserAgent;
            Md5Function = deviceUtil.Md5Function;

            InternalQueue.Enqueue(() => InitInternal(appToken));
        }

        public void SetEnvironment(string enviornment)
        {
            Environment = enviornment;
        }

        public void SetBufferedEvents(bool enabledEventBuffering)
        {
            IsBufferedEventsEnabled = enabledEventBuffering;
        }

        public void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(InternalStart);
        }

        public void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(InternalEndAsync);
        }

        public void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => InternalTrackEvent(eventToken, callbackParameters));
        }

        public void TrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => InternalTrackRevenue (amountInCents, eventToken, callbackParameters));
        }

        private void InitInternal(string appToken)
        {
            AppToken = appToken;
            //MacSha1 = Util.GetDeviceId();
            MacShortMd5 = Md5Function(DeviceId);
            IdForAdvertisers = "";
            FbAttributionId = "";
            IsTrackingEnabled = false;
            IsBufferedEventsEnabled = false;

            PackageHandler = new AIPackageHandler();

            //todo test file not exists
            Util.DeleteFile(ActivityStateFilename);

            ReadActivityState();

            InternalStart();
        }

        private void InternalTrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            var tcs = new TaskCompletionSource<object>();

            if (!CheckAppToken(AppToken)) return;
            if (!CheckActivityState(ActivityState)) return;
            if (!CheckEventToken(eventToken)) return;
            if (!CheckEventTokenLenght(eventToken)) return;

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.EventToken = eventToken;
            packageBuilder.CallBackParameters = callbackParameters;

            var now = DateTime.Now;

            UpdateActivityState();
            ActivityState.CreatedAt = now;
            ActivityState.EventCount++; 

            ActivityState.InjectEventAttributes(packageBuilder);
            var eventPackage = packageBuilder.BuildEventPackage();

            PackageHandler.AddPackage(eventPackage);

            if (IsBufferedEventsEnabled)
            {
                AILogger.Info("Buffered event{0}", eventPackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityState();
            AILogger.Debug("Event {0}", ActivityState.EventCount);
        }

        private void InternalTrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            if (!CheckAppToken(AppToken)) return;
            if (!CheckActivityState(ActivityState)) return;
            if (!CheckAmount(amountInCents)) return;
            if (!CheckEventTokenLenght(eventToken)) return;

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.AmountInCents = amountInCents;
            packageBuilder.EventToken = eventToken;
            packageBuilder.CallBackParameters = callbackParameters;

            var now = DateTime.Now;
            UpdateActivityState();

            ActivityState.CreatedAt = now;
            ActivityState.EventCount++;

            ActivityState.InjectEventAttributes(packageBuilder);

            var revenuePackage = packageBuilder.BuildRevenuePackage();

            PackageHandler.AddPackage(revenuePackage);

            if (IsBufferedEventsEnabled)
            {
                AILogger.Info("Buffered revenue{0}", revenuePackage.Suffix);
            }
            else
            {
                PackageHandler.SendFirstPackage();
            }

            WriteActivityState();
            AILogger.Debug("Event {0} revenue", ActivityState.EventCount);
        }
        
        private AIPackageBuilder GetDefaultPackageBuilder()
        {
            var packageBuilder = new AIPackageBuilder
            {
                UserAgent           = UserAgent,
                ClientSdk           = ClientSdk,
                AppToken            = AppToken,
                MacShortMD5         = MacShortMd5,
                MacSha1             = MacSha1,
                IsTrackingEnable    = IsTrackingEnabled,
                IdForAdvertisers    = IdForAdvertisers,
                FbAttributionId     = FbAttributionId,
                Environment         = Environment,
            };
            return packageBuilder;
        }

        private void WriteActivityState()
        {
            Util.SerializeToFile(ActivityStateFilename, AIActivityState.SerializeToStream, ActivityState);
        }

        private void ReadActivityState()
        {
            if (!Util.TryDeserializeFromFile(ActivityStateFilename,
                AIActivityState.DeserializeFromStream
                , out ActivityState))
            {
                //error read, start with fresh
                ActivityState = null;
            }
        }

        //return whether or not activity state should be written
        private bool UpdateActivityState()
        {
            if (!CheckActivityState(ActivityState)) 
                return false;

            var now = DateTime.Now;
            var lastInterval = now - ActivityState.LastActivity.Value;

            if (lastInterval.Ticks < 0)
            {
                AILogger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                return true;
            }

            //ignore past updates 
            if (lastInterval > SessionInterval)
                return false;

            ActivityState.SessionLenght += lastInterval;
            ActivityState.TimeSpent += lastInterval;
            ActivityState.LastActivity = now;

            return lastInterval > SubSessionInterval;
        }

        #region Sessions
        private void InternalStart()
        {
            if (!CheckAppToken(AppToken)) return;
            if (!CheckAppTokenLength(AppToken)) return;

            PackageHandler.ResumeSending();
            StartTimer();

            var now = DateTime.Now;

            AILogger.Verbose("now time ({0})", now);

            //if firsts Session
            if (ActivityState == null)
            {
                //Create fresh activity state
                ActivityState = new AIActivityState();
                ActivityState.SessionCount = 1; //first session
                ActivityState.CreatedAt = now;

                TransferSessionPackage();

                ActivityState.ResetSessionAttributes(now);
                WriteActivityState();

                AILogger.Info("First session");
                return;
            }

            var lastInterval = now - ActivityState.LastActivity.Value;

            AILogger.Verbose("last interval ({0})", lastInterval);

            if (lastInterval.Ticks < 0)
            {
                AILogger.Error("Time Travel!");
                ActivityState.LastActivity = now;
                WriteActivityState();
                return;
            }

            //new session
            if (lastInterval > SessionInterval)
            {
                ActivityState.SessionCount++;
                ActivityState.CreatedAt = now;
                ActivityState.LastInterval = lastInterval;

                TransferSessionPackage();
                ActivityState.ResetSessionAttributes(now);
                WriteActivityState();

                AILogger.Debug("Session {0}", ActivityState.SessionCount);
                return;
            }

            //new subsession
            if (lastInterval > SubSessionInterval)
            {
                ActivityState.SubSessionCount++;
                ActivityState.SessionLenght += lastInterval;
                ActivityState.LastActivity = now;

                WriteActivityState();
                AILogger.Info("Processed Subsession {0} of Session {1}",
                    ActivityState.SubSessionCount, ActivityState.SessionCount);
                return;
            }
        }

        private void InternalEndAsync()
        {
            if (!CheckAppToken(AppToken)) return;

            PackageHandler.PauseSending();
            StopTimer();
            UpdateActivityState();
            WriteActivityState();
        }

        private void TransferSessionPackage()
        {
            //Build Session Package
            var sessionBuilder = GetDefaultPackageBuilder();
            ActivityState.InjectSessionAttributes(sessionBuilder);
            var sessionPackage = sessionBuilder.BuildSessionPackage();

            //Send Session Package
            PackageHandler.AddPackage(sessionPackage);
            PackageHandler.SendFirstPackage();
        }
        #endregion

        #region Timer
        private void StartTimer()
        {
            if (TimeKeeper == null)
            {
                TimeKeeper = new AITimer(SystemThreadingTimer, null, TimerInterval);
            }
            TimeKeeper.Resume();
        }

        private void StopTimer()
        {
            TimeKeeper.Suspend();
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
        #endregion
        
        #region Checks
        private bool CheckAppToken(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                AILogger.Error("Missing App Token");
                return false;
            }
            return true;
        }

        private bool CheckAppTokenLength(string appToken)
        {
            if (appToken.Length != 12)
            {
                AILogger.Error("Malformed App Token '{0}'",appToken);
                return false;
            }
            return true;
        }

        private bool CheckActivityState(AIActivityState activityState)
        {
            if (activityState == null)
            {
                AILogger.Error("Missing activity state");
                return false;
            }
            return true;
        }

        private bool CheckEventToken(string eventToken)
        {
            if (eventToken == null)
            {
                AILogger.Error("Missing Event Token");
                return false;
            }
            return true;
        }

        private bool CheckEventTokenLenght(string eventToken)
        {
            if (eventToken != null && eventToken.Length != 6)
            {
                AILogger.Error("Malformed Event Token '{0}'", eventToken);
                return false; 
            }
            return true;
        }

        private bool CheckAmount(double amount)
        {
            if (amount < 0.0)
            {
                AILogger.Error("Invalid amount {0:.0}", amount);
                return false;
            }
            return true;
        }
        #endregion

    }
}
