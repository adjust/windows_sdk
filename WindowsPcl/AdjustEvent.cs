using System.Collections.Generic;
namespace AdjustSdk.Pcl
{
    public class AdjustEvent
    {
        internal string EventToken { get; private set; }
        internal double? Revenue { get; private set; }
        internal string Currency { get; private set; }
        internal Dictionary<string, string> CallbackParameters { get; private set; }
        internal Dictionary<string, string> PartnerParameters { get; private set; }

        private static ILogger Logger = AdjustFactory.Logger;

        public AdjustEvent(string eventToken)
        {
            if (!checkEventToken(eventToken)) { return; }

            EventToken = eventToken;
        }

        public void setRevenue(double revenue, string currency)
        {
            if (!checkRevenue(revenue, currency)) { return; }

            Revenue = revenue;
            Currency = currency;
        }

        public void addCallbackParameter(string key, string value)
        {
            if (!checkParameter(key, "key", "Callback")) { return; }
            if (!checkParameter(value, "value", "Callback")) { return; }

            if (CallbackParameters == null)
            {
                CallbackParameters = new Dictionary<string, string>();
            }

            CallbackParameters.Add(key, value);
        }

        public void addPartnerParameter(string key, string value)
        {
            if (!checkParameter(key, "key", "Partner")) { return; }
            if (!checkParameter(value, "value", "Partner")) { return; }

            if (PartnerParameters == null)
            {
                PartnerParameters = new Dictionary<string, string>();
            }

            PartnerParameters.Add(key, value);
        }

        public bool isValid()
        {
            return EventToken != null;
        }

        private bool checkEventToken(string eventToken)
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

        private bool checkRevenue(double? revenue, string currency)
        {
            if (revenue != null)
            {
                if (revenue < 0.0)
                {
                    Logger.Error("Invalid amount {0:0.0000}", revenue);
                    return false;
                }

                if (string.IsNullOrEmpty(currency))
                {
                    Logger.Error("Currency must be set with revenue");
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

        private bool checkParameter(string attribute, string attributeType, string parameterName)
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
