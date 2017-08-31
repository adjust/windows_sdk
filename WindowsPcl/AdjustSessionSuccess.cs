using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustSessionSuccess
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

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
