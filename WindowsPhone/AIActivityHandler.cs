using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace adeven.AdjustIo
{
    internal class AIActivityHandler
    {
        private const string ActivityStateFilename = "AdjustIOActivityState";
        private static readonly TimeSpan SessionInterval = new TimeSpan(0, 0, 10);//todo 10 seconds          // 30 minutes
        private static readonly TimeSpan SubSessionInterval = new TimeSpan(0, 0, 1);        // 1 second 
        private static readonly TimeSpan TimerInterval = new TimeSpan(0, 0, 5);//todo 5 sec             // 1 minute

        //private static AIRequestHandler RequestHandler = new AIRequestHandler();
        private static AIPackageHandler PackageHandler = new AIPackageHandler();
        private static AIActivityState ActivityState = null;
        private static AITimer TimeKeeper = null;

        private static string AppToken;
        private static string MacSha1;
        private static string MacShortMd5;
        private static string IdForAdvertisers;
        private static string FbAttributionId;
        private static string UserAgent;
        private static string ClientSdk;
        private static bool IsTrackingEnabled;

        internal static string Environment { get; private set; }
        internal static bool IsBufferedEventsEnabled { get; private set; }

        //private static NitoTaskQueue InternalQueue;
        private static AITaskQueue InternalQueue;

        internal AIActivityHandler(string appToken)
        {
            InternalQueue = new AITaskQueue("io.adjust.ActivityQueue");
            AIActivityHandler.Environment = "unknown";
            AIActivityHandler.ClientSdk = Util.ClientSdk;

            InternalQueue.Enqueue(() => InitInternalAsync(appToken));
        }

        internal void SetEnvironment(string enviornment)
        {
            AIActivityHandler.Environment = enviornment;
        }

        internal void SetBufferedEvents(bool enabledEventBuffering)
        {
            AIActivityHandler.IsBufferedEventsEnabled = enabledEventBuffering;
        }

        internal void TrackSubsessionStart()
        {
            InternalQueue.Enqueue(() => InternalStartAsync());
        }

        internal void TrackSubsessionEnd()
        {
            InternalQueue.Enqueue(() => InternalEndAsync());
        }

        internal void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => InternalTrackEventAsync(eventToken, callbackParameters));
        }

        internal void TrackRevenue(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
        {
            InternalQueue.Enqueue(() => InternalTrackRevenueAsync (amountInCents, eventToken, callbackParameters));
        }

        private async Task InitInternalAsync(string appToken)
        {
            AIActivityHandler.AppToken = appToken;
            //AIActivityHandler.MacSha1 = Util.GetDeviceId();
            AIActivityHandler.MacShortMd5 = Util.GetMd5Hash(Util.GetDeviceId());
            AIActivityHandler.IdForAdvertisers = "";
            AIActivityHandler.FbAttributionId = "";
            AIActivityHandler.IsTrackingEnabled = false;
            AIActivityHandler.UserAgent = Util.GetUserAgent();
            AIActivityHandler.IsBufferedEventsEnabled = false;

            PackageHandler = new AIPackageHandler();

            //todo test file not exists
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (storage.FileExists(ActivityStateFilename))
                storage.DeleteFile(ActivityStateFilename);

            ReadActivityState();

            await InternalStartAsync();
        }

        private async Task InternalTrackEventAsync(string eventToken,
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

        private async Task InternalTrackRevenueAsync(double amountInCents, string eventToken, Dictionary<string, string> callbackParameters)
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
                UserAgent = AIActivityHandler.UserAgent,
                ClientSdk = AIActivityHandler.ClientSdk,
                AppToken = AIActivityHandler.AppToken,
                MacShortMD5 = AIActivityHandler.MacShortMd5,
                MacSha1 = AIActivityHandler.MacSha1,
                IsTrackingEnable = AIActivityHandler.IsTrackingEnabled,
                IdForAdvertisers = AIActivityHandler.IdForAdvertisers,
                FbAttributionId = AIActivityHandler.FbAttributionId,
                Environment = AIActivityHandler.Environment,
            };
            return packageBuilder;
        }

        #region ActivityStateIO
        
        private void WriteActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

            using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.OpenOrCreate))
            {
                stream.Seek(0, SeekOrigin.Begin);
                AIActivityState.SerializeToStream(stream, ActivityState);
            }
        }

        private void ReadActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.Open))
                {
                    ActivityState = AIActivityState.DeserializeFromStream(stream);
                }
                AILogger.Verbose("Restored activity state {0}", ActivityState);
                return;
            }
            catch (IsolatedStorageException)
            {
                AILogger.Verbose("Activity state file not found");
            }
            catch(FileNotFoundException)
            {
                AILogger.Verbose("Activity state file not found");
            }
            catch (Exception e)
            {
                AILogger.Error("Failed to read activity state ({0})", e);
            }
            
            //start with a fresh activity state in case of any exception
            ActivityState = null;
        }

        #endregion

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
        private async Task InternalStartAsync()
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

        private async Task InternalEndAsync()
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
            InternalQueue.Enqueue(() => TimerFiredAsync());
        }

        private async Task TimerFiredAsync()
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
