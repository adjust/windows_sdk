using System.Collections.Generic;
using System.IO;

namespace AdjustSdk.Pcl
{
    public class SessionParameters
    {
        internal Dictionary<string, string> CallbackParameters;
        internal Dictionary<string, string> PartnerParameters;

        internal static void SerializeDictionaryToStream(Stream stream, Dictionary<string, string> dictionary)
        {
            var writer = new BinaryWriter(stream);

            var dictionaryCopy = new Dictionary<string, string>(dictionary);
            writer.Write(dictionaryCopy.Count);
            foreach (KeyValuePair<string, string> kvp in dictionaryCopy)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        internal static Dictionary<string, string> DeserializeDictionaryFromStream(Stream stream)
        {
            Dictionary<string, string> dictionary = null;
            
            var reader = new BinaryReader(stream);

            var dictionaryCount = reader.ReadInt32();
            dictionary = new Dictionary<string, string>(dictionaryCount);

            for (int i = 0; i < dictionaryCount; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        internal SessionParameters Clone()
        {
            var copy = new SessionParameters();

            if (CallbackParameters != null)
            {
                copy.CallbackParameters = new Dictionary<string, string>(CallbackParameters);
            }

            if (PartnerParameters != null)
            {
                copy.PartnerParameters = new Dictionary<string, string>(PartnerParameters);
            }
            return copy;
        }
    }
}
