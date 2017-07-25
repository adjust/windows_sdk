using AdjustSdk.Pcl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AdjustSdk
{
    public class AdjustAttribution
    {
        public string TrackerToken { get; set; }
        public string TrackerName { get; set; }
        public string Network { get; set; }
        public string Campaign { get; set; }
        public string Adgroup { get; set; }
        public string Creative { get; set; }
        public string ClickLabel { get; set; }
        public string Adid { get; set; }

        internal Dictionary<string, string> Json { get; set; }

        public static AdjustAttribution FromJsonString(string attributionString, string adid)
        {
            if (attributionString == null) { return null; }

            try
            {
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(attributionString);
                var attribution = new AdjustAttribution
                {
                    TrackerToken = Util.GetDictionaryString(jsonDict, "tracker_token"),
                    TrackerName = Util.GetDictionaryString(jsonDict, "tracker_name"),
                    Network = Util.GetDictionaryString(jsonDict, "network"),
                    Campaign = Util.GetDictionaryString(jsonDict, "campaign"),
                    Adgroup = Util.GetDictionaryString(jsonDict, "adgroup"),
                    Creative = Util.GetDictionaryString(jsonDict, "creative"),
                    ClickLabel = Util.GetDictionaryString(jsonDict, "click_label"),
                    Adid = adid,
                    Json = jsonDict,
                };
                return attribution;
            }
            catch (Exception) { return null; }
        }

        public AdjustAttribution()
        { }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }

            var other = obj as AdjustAttribution;
            if (other == null) { return false; }

            if (!EqualString(TrackerToken, other.TrackerToken)) { return false; }
            if (!EqualString(TrackerName, other.TrackerName)) { return false; }
            if (!EqualString(Network, other.Network)) { return false; }
            if (!EqualString(Campaign, other.Campaign)) { return false; }
            if (!EqualString(Adgroup, other.Adgroup)) { return false; }
            if (!EqualString(Creative, other.Creative)) { return false; }
            if (!EqualString(ClickLabel, other.ClickLabel)) { return false; }
            if (!EqualString(Adid, other.Adid)) { return false; }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = 37 * hashCode + HashString(TrackerToken);
            hashCode = 37 * hashCode + HashString(TrackerName);
            hashCode = 37 * hashCode + HashString(Network);
            hashCode = 37 * hashCode + HashString(Campaign);
            hashCode = 37 * hashCode + HashString(Adgroup);
            hashCode = 37 * hashCode + HashString(Creative);
            hashCode = 37 * hashCode + HashString(ClickLabel);
            hashCode = 37 * hashCode + HashString(Adid);

            return hashCode;
        }

        public override string ToString()
        {
            return Util.f("tt:{0} tn:{1} net:{2} cam:{3} adg:{4} cre:{5} cl:{6} adid:{7}",
                TrackerToken, 
                TrackerName, 
                Network, 
                Campaign, 
                Adgroup, 
                Creative, 
                ClickLabel,
                Adid);
        }

        public static Dictionary<string, object> ToDictionary(AdjustAttribution attribution)
        {
            return new Dictionary<string, object>
            {
                {"TrackerName", attribution.TrackerName},
                {"TrackerToken", attribution.TrackerToken},
                {"Network", attribution.Network},
                {"Campaign", attribution.Campaign},
                {"Adgroup", attribution.Adgroup},
                {"Creative", attribution.Creative},
                {"ClickLabel", attribution.ClickLabel},
                {"Adid", attribution.Adid}
            };
        }

        public static AdjustAttribution FromDictionary(Dictionary<string, object> attributionObjectMap)
        {
            return new AdjustAttribution
            {
                TrackerName = attributionObjectMap.ContainsKey("TrackerName") ? attributionObjectMap["TrackerName"] as string : null,
                TrackerToken = attributionObjectMap.ContainsKey("TrackerToken") ? attributionObjectMap["TrackerToken"] as string : null,
                Network = attributionObjectMap.ContainsKey("Network") ? attributionObjectMap["Network"] as string : null,
                Campaign = attributionObjectMap.ContainsKey("Campaign") ? attributionObjectMap["Campaign"] as string : null,
                Adgroup = attributionObjectMap.ContainsKey("Adgroup") ? attributionObjectMap["Adgroup"] as string : null,
                Creative = attributionObjectMap.ContainsKey("Creative") ? attributionObjectMap["Creative"] as string : null,
                ClickLabel = attributionObjectMap.ContainsKey("ClickLabel") ? attributionObjectMap["ClickLabel"] as string : null,
                Adid = attributionObjectMap.ContainsKey("Adid") ? attributionObjectMap["Adid"] as string : null
            };
        }

        private bool EqualString(string first, string second)
        {
            if (first == null || second == null)
            {
                return first == null && second == null;
            }

            return first.Equals(second);
        }

        private int HashString(string value)
        {
            if (value == null) { return 0; }

            return value.GetHashCode();
        }
        
        // does not close stream received. Caller is responsible to close if it wants it
        internal static AdjustAttribution DeserializeFromStreamLegacy(Stream stream)
        {
            AdjustAttribution attribution = null;
            var reader = new BinaryReader(stream);

            attribution = new AdjustAttribution();
            attribution.TrackerToken = ReadOptionalString(reader);
            attribution.TrackerName = ReadOptionalString(reader);
            attribution.Network = ReadOptionalString(reader);
            attribution.Campaign = ReadOptionalString(reader);
            attribution.Adgroup = ReadOptionalString(reader);
            attribution.Creative = ReadOptionalString(reader);
            attribution.ClickLabel = ReadOptionalString(reader);

            return attribution;
        }

        private static string ReadOptionalString(BinaryReader reader)
        {
            var hasValue = reader.ReadBoolean();
            if (!hasValue) { return null; }

            return reader.ReadString();
        }
    }
}