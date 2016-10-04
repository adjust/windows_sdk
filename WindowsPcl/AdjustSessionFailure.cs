using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustSessionFailure
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string Adid { get; set; }
        public bool WillRetry { get; set; }
        public Dictionary<string, string> JsonResponse { get; set; }

        public override string ToString()
        {
            return Pcl.Util.f("Session Failure msg:{0} time:{1} adid:{2} retry:{3} json:{4}",
                Message,
                Timestamp,
                Adid,
                WillRetry,
                JsonResponse);
        }
    }
}
