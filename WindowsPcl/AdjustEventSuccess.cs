using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdjustSdk
{
    public class AdjustEventSuccess
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public string EventToken { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

        private const string MESSAGE = "message";
        private const string TIMESTAMP = "timestamp";
        private const string ADID = "adid";
        private const string EVENT_TOKEN = "eventToken";
        private const string JSON_RESPONSE = "jsonResponse";

        public static Dictionary<string, string> ToDictionary(AdjustEventSuccess adjustSession)
        {
            var jsonResp = JsonConvert.SerializeObject(adjustSession.JsonResponse);
            return new Dictionary<string, string>
            {
                {MESSAGE, adjustSession.Message},
                {TIMESTAMP, adjustSession.Timestamp},
                {ADID, adjustSession.Adid},
                {EVENT_TOKEN, adjustSession.EventToken},
                {JSON_RESPONSE, jsonResp}
            };
        }

        public override string ToString()
        {
            return Pcl.Util.F("Event Success msg:{0} time:{1} adid:{2} event:{3} json:{4}",
                Message,
                Timestamp,
                Adid,
                EventToken,
                JsonResponse);
        }
    }
}
