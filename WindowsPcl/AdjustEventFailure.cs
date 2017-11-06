using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdjustSdk
{
    public class AdjustEventFailure
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public string EventToken { get; set; }
        public bool WillRetry { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

        private const string MESSAGE = "message";
        private const string TIMESTAMP = "timestamp";
        private const string ADID = "adid";
        private const string EVENT_TOKEN = "eventToken";
        private const string WILL_RETRY = "willRetry";
        private const string JSON_RESPONSE = "jsonResponse";

        public static Dictionary<string, string> ToDictionary(AdjustEventFailure adjustEvent)
        {
            var jsonResp = JsonConvert.SerializeObject(adjustEvent.JsonResponse);
            return new Dictionary<string, string>
            {
                {MESSAGE, adjustEvent.Message},
                {TIMESTAMP, adjustEvent.Timestamp},
                {ADID, adjustEvent.Adid},
                {EVENT_TOKEN, adjustEvent.EventToken},
                {WILL_RETRY, adjustEvent.WillRetry.ToString()},
                {JSON_RESPONSE, jsonResp}
            };
        }

        public override string ToString()
        {
            return Pcl.Util.F("Event Failure msg:{0} time:{1} adid:{2} event:{3} retry:{4} json:{5}",
                Message,
                Timestamp,
                Adid,
                EventToken,
                WillRetry,
                JsonResponse);
        }
    }
}
