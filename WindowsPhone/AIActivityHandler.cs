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
    class AIActivityHandler
    {
        private const string ActivityStateFilename = "AdjustIOActivityState";
        private const double SessionInterval      = 30 * 60;  // 30 minutes
        private const double SubSessionInterval = 1;        // 1 second 

        static AIRequestHandler requestHandler = new AIRequestHandler();
        static AIActivityState activityState = null;

        static string AppToken;
        static string MacSha1;
        static string MacShortMd5;
        static string IdForAdvertisers;
        static string FbAttributionId;
        static string UserAgent;
        static string ClientSdk;
        static bool IsTrackingEnabled;
        static string Environment;

        public AIActivityHandler(string appToken)
        {
            if (IsAppTokenNull(appToken)) return;

            if (appToken.Length != 12) {
                AILogger.Error("Malformed App Token '{0}'", appToken);
                return;
            }

            AIActivityHandler.AppToken = appToken;
            AIActivityHandler.MacSha1 = Util.GetDeviceId();
            AIActivityHandler.MacShortMd5 = Util.GetMd5Hash(AIActivityHandler.MacSha1);
            AIActivityHandler.IdForAdvertisers = "";
            AIActivityHandler.FbAttributionId = "";
            AIActivityHandler.IsTrackingEnabled = false;
            AIActivityHandler.UserAgent = Util.GetUserAgent();
            AIActivityHandler.ClientSdk = Util.ClientSdk;
            AIActivityHandler.Environment = "unknown";

            //test file not exists
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (storage.FileExists(ActivityStateFilename))
                storage.DeleteFile(ActivityStateFilename);

            //we can run synchronously because there is no result
            //see http://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously
            //RestoreActivityStateAsync().Wait();
            ReadActivityState();

            Start();
        }

        public void SetEnvironment(string enviornment)
        {
            AIActivityHandler.Environment = enviornment;
        }

        public void TrackEvent(string eventToken,
            Dictionary<string, string> callbackParameters)
        {
        //    string paramString = Util.GetBase64EncodedParameters(callbackParameters);

        //    var parameters = new Dictionary<string, string> {
        //        { "app_id", AdjustIo.appId },
        //        //TODO change app_id to app_token. Ver ios app
        //        { "mac", AdjustIo.deviceId },
        //        { "id", EventToken },
        //        { "params", paramString}
        //    };

            var packageBuilder = GetDefaultPackageBuilder();

            packageBuilder.EventToken = eventToken;
            packageBuilder.CallBackParameters = callbackParameters;

        //    //event specific attributes
        //    packageBuilder.EventCount = 0;

            //TODO use PackageHandler in the future
            requestHandler.SendPackage(
                packageBuilder.BuildEventPackage()
            );

        }

        //public void TrackRevenue(double amountInCents, string evenToken, Dictionary<string, string> parameters)
        //{
        //    int amountInMillis = (int)Math.Round(10 * amountInCents);
        //    string amount = amountInMillis.ToString();
        //    string paramString = Util.GetBase64EncodedParameters(parameters);

        //}

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
            packageBuilder.FillDefaults();
            return packageBuilder;
        }

        #region ActivityStateIO
        
        private void WriteActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

            using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.OpenOrCreate))
            {
                stream.Seek(0, SeekOrigin.Begin);
                AIActivityState.SerializeToStream(stream, activityState);
            }
        }

        private void ReadActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.Open))
                {
                    activityState = AIActivityState.DeserializeFromStream(stream);
                }
                AILogger.Verbose("Restored activity state {0}", activityState);
            }
            catch (IsolatedStorageException ise)
            {
                AILogger.Verbose("Activity state file not found");
            }
            catch(FileNotFoundException fnfe)
            {
                AILogger.Verbose("Activity state file not found");
            }
            catch (Exception e)
            {
                AILogger.Error("Failed to read activity state ({0})", e);
            }
            
            //start with a fresh activity state in case of any exception
            activityState = null;
        }

        #endregion

        private void Start()
        {
            if (IsAppTokenNull(AppToken)) return;

            //TODO package Handler start package
            //TODO start timer

            double nowInSeconds = Util.ConvertToUnixTimestamp(DateTime.Now);

            //if firsts Session
            if (activityState == null)
            {
                //Create fresh activity state
                activityState = new AIActivityState();
                activityState.SessionCount = 1; //first session
                activityState.CreatedAt = nowInSeconds;

                TransferSessionPackage();

                activityState.ResetSessionAttributes(nowInSeconds);
                WriteActivityState();

                AILogger.Info("First session");
                return;
            }

            double lastInterval = nowInSeconds - activityState.LastActivity;
            if (lastInterval < 0)
            {
                AILogger.Error("Time Travel!");
                activityState.LastActivity = nowInSeconds;
                WriteActivityState();
                return;
            }

            //new session
            if (lastInterval > SessionInterval)
            {
                activityState.SessionCount++;
                activityState.CreatedAt = nowInSeconds;
                activityState.LastInterval = lastInterval;

                TransferSessionPackage();
                activityState.ResetSessionAttributes(nowInSeconds);
                WriteActivityState();

                AILogger.Debug("Session {0}", activityState.SessionCount);
                return;
            }

            //new subsession
            if (lastInterval > SubSessionInterval)
            {
                activityState.SubSessionCount++;
                activityState.SessionLenght += lastInterval;
                activityState.LastActivity = nowInSeconds;

                WriteActivityState();
                AILogger.Info("Processed Subsession {0} of Session {1}",
                    activityState.SubSessionCount, activityState.SessionCount);
                return;
            }
        }

        private void TransferSessionPackage()
        {
            //Build Session Package
            var sessionBuilder = GetDefaultPackageBuilder();
            activityState.InjectSessionAttributes(sessionBuilder);
            var sessionPackage = sessionBuilder.BuildSessionPackage();

            //Send Session Package
            requestHandler.SendPackage(
                sessionPackage
            );
        }

        private bool IsAppTokenNull(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                AILogger.Error("Missing App Token");
                return true;
            }
            return false;
        }
    }
}
