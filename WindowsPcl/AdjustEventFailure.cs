using System.Collections.Generic;

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
