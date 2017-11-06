using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdjustSdk
{
    public class AdjustSessionFailure
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public bool WillRetry { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

        private const string MESSAGE = "message";
        private const string TIMESTAMP = "timestamp";
        private const string ADID = "adid";
        private const string WILL_RETRY = "willRetry";
        private const string JSON_RESPONSE = "jsonResponse";

        public static Dictionary<string, string> ToDictionary(AdjustSessionFailure adjustSession)
        {
            var jsonResp = JsonConvert.SerializeObject(adjustSession.JsonResponse);
            return new Dictionary<string, string>
            {
                {MESSAGE, adjustSession.Message},
                {TIMESTAMP, adjustSession.Timestamp},
                {ADID, adjustSession.Adid},
                {WILL_RETRY, adjustSession.WillRetry.ToString()},
                {JSON_RESPONSE, jsonResp}
            };
        }

        public override string ToString()
        {
            return Pcl.Util.F("Session Failure msg:{0} time:{1} adid:{2} retry:{3} json:{4}",
                Message,
                Timestamp,
                Adid,
                WillRetry,
                JsonResponse);
        }
    }
}
