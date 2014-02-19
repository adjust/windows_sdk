using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo.CD
{
    public class ResponseData
    {
        public string Error;
        public string TrackerToken;
        public string TrackerName;

        public bool WillRetry { get; set; }

        public bool Success { get; set; }

        public ResponseData(string responseString)
        {
            var responseDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            if (responseDic == null)
            {
                Logger.Error("Failed to parse json response: {0}", responseString);
                return;
            }

            responseDic.TryGetValue("error", out Error);
            responseDic.TryGetValue("tracker_token", out TrackerToken);
            responseDic.TryGetValue("tracker_name", out TrackerName);
        }

        public ResponseData()
        {
        }

        public void SetError(string errorString = null)
        {
            Error = errorString;
            Success = false;
        }
    }
}