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

        internal Dictionary<string, string> Json { get; set; }

        public static AdjustAttribution FromJsonString(string attributionString)
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

            return hashCode;
        }

        public override string ToString()
        {
            return Util.f("tt:{0} tn:{1} net:{2} cam:{3} adg:{4} cre:{5} cl:{6}",
                TrackerToken, 
                TrackerName, 
                Network, 
                Campaign, 
                Adgroup, 
                Creative, 
                ClickLabel);
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                {"trackerName", TrackerName},
                {"trackerToken", TrackerToken},
                {"network", Network},
                {"campaign", Campaign},
                {"adgroup", Adgroup},
                {"creative", Creative},
                {"clickLabel", ClickLabel},
            };
        }

        private void addToDic(Dictionary<string, string> dic, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                dic.Add(key, value);
            }
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

        #region Serialization

        // does not close stream received. Caller is responsible to close if it wants it
        internal static void SerializeToStream(Stream stream, AdjustAttribution attribution)
        {
            var writer = new BinaryWriter(stream);

            WriteOptionalString(writer, attribution.TrackerToken);
            WriteOptionalString(writer, attribution.TrackerName);
            WriteOptionalString(writer, attribution.Network);
            WriteOptionalString(writer, attribution.Campaign);
            WriteOptionalString(writer, attribution.Adgroup);
            WriteOptionalString(writer, attribution.Creative);
            WriteOptionalString(writer, attribution.ClickLabel);
        }

        private static void WriteOptionalString(BinaryWriter writer, string value)
        {
            var hasValue = value != null;
            writer.Write(hasValue);
            if (hasValue)
            {
                writer.Write(value);
            }
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static AdjustAttribution DeserializeFromStream(Stream stream)
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

        #endregion Serialization
    }
}