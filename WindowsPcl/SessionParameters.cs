using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdjustSdk.Pcl
{
    public class SessionParameters
    {
        internal Dictionary<string, string> CallbackParameters { get; set; }
        internal Dictionary<string, string> PartnerParameters { get; set; }

        private const string CALLBACK_PARAMETERS = "CallbackParameters";
        private const string PARTNER_PARAMETERS = "PartnerParameters";

        internal SessionParameters Clone()
        {
            var copy = new SessionParameters();

            if (CallbackParameters != null)
                copy.CallbackParameters = new Dictionary<string, string>(CallbackParameters);

            if (PartnerParameters != null)
                copy.PartnerParameters = new Dictionary<string, string>(PartnerParameters);
            return copy;
        }

        public static Dictionary<string, object> ToDictionary(SessionParameters sessionParameters)
        {
            var callbackParamsJson = JsonConvert.SerializeObject(sessionParameters.CallbackParameters);
            var partnerParamsJson = JsonConvert.SerializeObject(sessionParameters.PartnerParameters);

            return new Dictionary<string, object>
            {
                {CALLBACK_PARAMETERS, callbackParamsJson},
                {PARTNER_PARAMETERS, partnerParamsJson}
            };
        }

        public static SessionParameters FromDictionary(Dictionary<string, object> sessionParamsObjectMap)
        {
            var sessionParams = new SessionParameters();

            object callbackParamsJson;
            if (sessionParamsObjectMap.TryGetValue(CALLBACK_PARAMETERS, out callbackParamsJson))
                sessionParams.CallbackParameters =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(callbackParamsJson.ToString());

            object partnerParamsJson;
            if (sessionParamsObjectMap.TryGetValue(PARTNER_PARAMETERS, out partnerParamsJson))
                sessionParams.PartnerParameters =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(partnerParamsJson.ToString());

            return sessionParams;
        }
    }
}