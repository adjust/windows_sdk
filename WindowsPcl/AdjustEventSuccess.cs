using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustEventSuccess
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public string EventToken { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

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
