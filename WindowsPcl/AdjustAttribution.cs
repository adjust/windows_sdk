using System.IO;

namespace AdjustSdk.Pcl
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

            writer.Write(attribution.TrackerToken);
            writer.Write(attribution.TrackerName);
            writer.Write(attribution.Network);
            writer.Write(attribution.Campaign);
            writer.Write(attribution.Adgroup);
            writer.Write(attribution.Creative);
            writer.Write(attribution.ClickLabel);
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static AdjustAttribution DeserializeFromStream(Stream stream)
        {
            AdjustAttribution attribution = null;
            var reader = new BinaryReader(stream);

            attribution = new AdjustAttribution();
            attribution.TrackerToken = reader.ReadString();
            attribution.TrackerName = reader.ReadString();
            attribution.Network = reader.ReadString();
            attribution.Campaign = reader.ReadString();
            attribution.Adgroup = reader.ReadString();
            attribution.Creative = reader.ReadString();
            attribution.ClickLabel = reader.ReadString();

            return attribution;
        }

        #endregion Serialization
    }
}