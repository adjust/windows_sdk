using AdjustSdk.Pcl;
using System.Collections.Generic;

namespace AdjustSdk
{
    public class AdjustEvent
    {
        private readonly ILogger _logger = AdjustFactory.Logger;

        internal string EventToken { get; private set; }
        internal double? Revenue { get; private set; }
        internal string Currency { get; private set; }
        internal Dictionary<string, string> CallbackParameters { get; private set; }
        internal Dictionary<string, string> PartnerParameters { get; private set; }
        public string PurchaseId { get; set; }

        public AdjustEvent(string eventToken)
        {
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
            if (!Util.CheckParameter(key, "key", "Callback")) { return; }
            if (!Util.CheckParameter(value, "value", "Callback")) { return; }

            if (CallbackParameters == null)
            {
                CallbackParameters = new Dictionary<string, string>();
            }

            string previousValue;
            if (CallbackParameters.TryGetValue(key, out previousValue))
            {
                _logger.Warn("key {0} was overwritten", key);
            }
            CallbackParameters.AddSafe(key, value);
        }

        public void AddPartnerParameter(string key, string value)
        {
            if (!Util.CheckParameter(key, "key", "Partner")) { return; }
            if (!Util.CheckParameter(value, "value", "Partner")) { return; }

            if (PartnerParameters == null)
            {
                PartnerParameters = new Dictionary<string, string>();
            }

            string previousValue;
            if (PartnerParameters.TryGetValue(key, out previousValue))
            {
                _logger.Warn("key {0} was overwritten", key);
            }

            PartnerParameters.AddSafe(key, value);
        }

        public bool IsValid()
        {
            return EventToken != null;
        }

        private bool CheckEventToken(string eventToken)
        {
            if (string.IsNullOrEmpty(eventToken))
            {
                _logger.Error("Missing Event Token");
                return false;
            }

            if (eventToken.Length != 6)
            {
                _logger.Error("Malformed Event Token '{0}'", eventToken);
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
                    _logger.Error("Invalid amount {0:0.0000}", revenue);
                    return false;
                }

                if (currency == null)
                {
                    _logger.Error("Currency must be set with revenue");
                    return false;
                }

                if (string.Empty.Equals(currency))
                {
                    _logger.Error("Currency is empty");
                    return false;
                }
            }
            else if (currency != null)
            {
                _logger.Error("Revenue must be set with currency");
                return false;
            }

            return true;
        }
    }
}