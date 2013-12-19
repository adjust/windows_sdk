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
            //IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            //if (storage.FileExists(ActivityStateFilename))
            //    storage.DeleteFile(ActivityStateFilename);

            //we can run synchronously because there is no result
            //see http://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously
            //RestoreActivityStateAsync().Wait();
            RestoreActivityState();

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
        
        public void SaveActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

            using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.OpenOrCreate))
            {
                stream.Seek(0, SeekOrigin.Begin);
                AIActivityState.SerializeToStream(stream, activityState);
            }
        }

        public void RestoreActivityState()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                using (var stream = storage.OpenFile(ActivityStateFilename, FileMode.Open))
                {
                    activityState = AIActivityState.DeserializeFromStream(stream);
                }
            }
            catch (IsolatedStorageException ise)
            {
                //The isolated store has been removed.
                //-or-
                //Isolated storage is disabled.
            }
            catch(FileNotFoundException fnfe)
            {
                //No file was found and the mode is set to Open.
            }
            catch (Exception e)
            {
            }
        }

        #endregion

        private void Start()
        {
            double nowInSeconds = Util.ConvertToUnixTimestamp(DateTime.Now);

            //if firsts Session
            //test to write on top of already existing file
            if (activityState == null)
            {
                //Create fresh activity state
                activityState = new AIActivityState();
                activityState.SessionCount = 1; //first session
                activityState.CreatedAt = nowInSeconds;

                //Build Session Package
                var sessionBuilder = GetDefaultPackageBuilder();
                activityState.InjectSessionAttributes(sessionBuilder);
                var sessionPackage = sessionBuilder.BuildSessionPackage();
                
                //Send Session Package
                //TODO add package handler
                requestHandler.SendPackage(
                    sessionPackage
                );

                activityState.ResetSessionAttributes(nowInSeconds);
                //SaveActivityStateAsync().Wait();
                SaveActivityState();
            }
        }
    }
}
