using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace adeven.AdjustIo
{
    class AIActivityHandler
    {
        static AIRequestHandler requestHandler = new AIRequestHandler();

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

        private void Start()
        {
            //if firsts Session
            var sessionBuilder = GetDefaultPackageBuilder();

            requestHandler.SendPackage(
                sessionBuilder.BuildSessionPackage()
            );
        }
    }
}
