using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdjustSdk
{
    public class AdjustSessionSuccess
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

        private const string MESSAGE = "message";
        private const string TIMESTAMP = "timestamp";
        private const string ADID = "adid";
        private const string JSON_RESPONSE = "jsonResponse";

        public static Dictionary<string, string> ToDictionary(AdjustSessionSuccess adjustEvent)
        {
            var jsonResp = JsonConvert.SerializeObject(adjustEvent.JsonResponse);
            return new Dictionary<string, string>
            {
                {MESSAGE, adjustEvent.Message},
                {TIMESTAMP, adjustEvent.Timestamp},
                {ADID, adjustEvent.Adid},
                {JSON_RESPONSE, jsonResp}
            };
        }

        public override string ToString()
        {
            return Pcl.Util.F("Session Success msg:{0} time:{1} adid:{2} json:{3}",
                Message,
                Timestamp,
                Adid,
                JsonResponse);
        }
    }
}
