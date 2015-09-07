using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustEvent
    {
        internal string EventToken { get; private set; }

        internal double? Revenue { get; private set; }

        internal string Currency { get; private set; }

        internal Dictionary<string, string> CallbackParameters { get; private set; }

        internal Dictionary<string, string> PartnerParameters { get; private set; }

        private ILogger Logger { get; set; }

        public AdjustEvent(string eventToken)
        {
            Logger = AdjustFactory.Logger;

            if (!CheckEventToken(eventToken)) { return; }

            EventToken = eventToken;
        }

        public void SetRevenue(double revenue, string currency)
        {
            if (!CheckRevenue(revenue, currency)) { return; }

            Revenue = revenue;
            Currency = currency;
        }

        public void AddCallbackParameter(string key, string value)
        {
            if (!CheckParameter(key, "key", "Callback")) { return; }
            if (!CheckParameter(value, "value", "Callback")) { return; }

            if (CallbackParameters == null)
            {
                CallbackParameters = new Dictionary<string, string>();
            }

            string previousValue;
            if (CallbackParameters.TryGetValue(key, out previousValue))
            {
                Logger.Warn("key {0} was overwritten", key);
                CallbackParameters.Remove(key);
            }
            CallbackParameters.Add(key, value);
        }

        public void AddPartnerParameter(string key, string value)
        {
            if (!CheckParameter(key, "key", "Partner")) { return; }
            if (!CheckParameter(value, "value", "Partner")) { return; }

            if (PartnerParameters == null)
            {
                PartnerParameters = new Dictionary<string, string>();
            }

            string previousValue;
            if (PartnerParameters.TryGetValue(key, out previousValue))
            {
                Logger.Warn("key {0} was overwritten", key);
                PartnerParameters.Remove(key);
            }

            PartnerParameters.Add(key, value);
        }

        public bool IsValid()
        {
            return EventToken != null;
        }

        private bool CheckEventToken(string eventToken)
        {
            if (string.IsNullOrEmpty(eventToken))
            {
                Logger.Error("Missing Event Token");
                return false;
            }

            if (eventToken.Length != 6)
            {
                Logger.Error("Malformed Event Token '{0}'", eventToken);
                return false;
            }

            return true;
        }

        private bool CheckRevenue(double? revenue, string currency)
        {
            if (revenue != null)
            {
                if (revenue < 0.0)
                {
                    Logger.Error("Invalid amount {0:0.0000}", revenue);
                    return false;
                }

                if (currency == null)
                {
                    Logger.Error("Currency must be set with revenue");
                    return false;
                }

                if (string.Empty.Equals(currency))
                {
                    Logger.Error("Currency is empty");
                    return false;
                }
            }
            else if (currency != null)
            {
                Logger.Error("Revenue must be set with currency");
                return false;
            }

            return true;
        }

        private bool CheckParameter(string attribute, string attributeType, string parameterName)
        {
            if (attribute == null)
            {
                Logger.Error("{0} parameter {1} is missing", parameterName, attributeType);
                return false;
            }

            if (attribute.Length == 0)
            {
                Logger.Error("{0} parameter {1} is empty", parameterName, attributeType);
                return false;
            }

            return true;
        }
    }
}