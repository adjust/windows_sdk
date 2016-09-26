using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class ResponseData
    {
        internal bool WillRetry { get; set; }
        internal Dictionary<string, string> JsonResponse { get; set; }
        internal string Message { get; set; }
        internal string Timestamp { get; set; }
        internal string Adid { get; set; }
        internal bool Success { get; set; }
        internal int? StatusCode { get; set; }
        internal Exception Exception { get; set; }
    }
}
