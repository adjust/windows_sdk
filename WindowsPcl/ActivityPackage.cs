using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AdjustSdk.Pcl
{
    public class ActivityPackage : VersionedSerializable
    {
        public ActivityKind ActivityKind { get; set; }
        public string ClientSdk { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }
        public string Path { get; private set; }
        public string Suffix { get; set; }
        public int Retries { get; private set; }
        public Dictionary<string, string> CallbackParameters { get; set; }
        public Dictionary<string, string> PartnerParameters { get; set; }

        public ActivityPackage()
        { }

        public ActivityPackage(ActivityKind activityKind, string clientSdk, Dictionary<string, string> parameters)
        {
            ActivityKind = activityKind;
            ClientSdk = clientSdk;
            Parameters = parameters;
            Path = ActivityKindUtil.GetPath(ActivityKind);
            Suffix = ActivityKindUtil.GetSuffix(Parameters);
            Retries = 0;
        }

        public string FailureMessage()
        {
            return Util.f("Failed to track {0}{1}", ActivityKindUtil.ToString(ActivityKind), Suffix);
        }

        public override string ToString()
        {
            return Util.f("{0}{1}", ActivityKindUtil.ToString(ActivityKind), Suffix);
        }

        public string GetExtendedString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("Path:      {0}\n", Path);
            stringBuilder.AppendFormat("ClientSdk: {0}\n", ClientSdk);

            if (Parameters != null)
            {
                stringBuilder.AppendFormat("Parameters:");
                var sortedParameters = new SortedDictionary<string,string>(Parameters);
                foreach (var keyValuePair in sortedParameters)
                {
                    stringBuilder.AppendFormat("\n\t\t{0} {1}", keyValuePair.Key.PadRight(16, ' '), keyValuePair.Value);
                }
            }

            return stringBuilder.ToString();
        }

        public int IncreaseRetries()
        {
            Retries++;
            return Retries;
        }

        #region Serialization

        internal override Dictionary<string, Tuple<SerializableType, object>> GetSerializableFields()
        {
            var serializableFields = new Dictionary<string, Tuple<SerializableType, object>>(7);

            AddField(serializableFields, "Path", Path);
            AddField(serializableFields, "ClientSdk", ClientSdk);
            AddField(serializableFields, "ActivityKind", ActivityKindUtil.ToString(ActivityKind));
            AddField(serializableFields, "Suffix", Suffix);
            AddField(serializableFields, "Parameters", Parameters);
            AddField(serializableFields, "CallbackParameters", CallbackParameters);
            AddField(serializableFields, "PartnerParameters", PartnerParameters);

            return serializableFields;
        }

        internal override void InitWithSerializedFields(int version, Dictionary<string, object> serializedFields)
        {
            Path = GetFieldValueString(serializedFields, "Path");
            ClientSdk = GetFieldValueString(serializedFields, "ClientSdk");
            ActivityKind = ActivityKindUtil.FromString(GetFieldValueString(serializedFields, "ActivityKind"));
            Suffix = GetFieldValueString(serializedFields, "Suffix");
            Parameters = GetFieldValueDictionaryString(serializedFields, "Parameters");
            CallbackParameters = GetFieldValueDictionaryString(serializedFields, "CallbackParameters");
            PartnerParameters = GetFieldValueDictionaryString(serializedFields, "PartnerParameters");
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static ActivityPackage DeserializeFromStreamLegacy(Stream stream)
        {
            ActivityPackage activityPackage = null;
            var reader = new BinaryReader(stream);

            activityPackage = new ActivityPackage();
            activityPackage.Path = reader.ReadString();
            reader.ReadString(); //activityPackage.UserAgent
            activityPackage.ClientSdk = reader.ReadString();
            activityPackage.ActivityKind = ActivityKindUtil.FromString(reader.ReadString());
            activityPackage.Suffix = reader.ReadString();

            var parameterLength = reader.ReadInt32();
            activityPackage.Parameters = new Dictionary<string, string>(parameterLength);

            for (int i = 0; i < parameterLength; i++)
            {
                activityPackage.Parameters.AddSafe(
                    reader.ReadString(),
                    reader.ReadString()
                );
            }

            return activityPackage;
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static void SerializeListToStream(Stream stream, List<ActivityPackage> activityPackageList)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(Version);

            var activityPackageArray = activityPackageList.ToArray();
            writer.Write(activityPackageArray.Length);
            for (int i = 0; i < activityPackageArray.Length; i++)
            {
                var activityPackage = activityPackageArray[i];
                SerializeToStream(writer, activityPackage);
            }
        }

        // does not close stream received. Caller is responsible to close if it wants it
        internal static List<ActivityPackage> DeserializeListFromStream(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var version = reader.ReadInt32();

            var activityPackageLength = reader.ReadInt32();
            List<ActivityPackage> activityPackageList = new List<ActivityPackage>(activityPackageLength);

            for (int i = 0; i < activityPackageLength; i++)
            {
                activityPackageList.Add(
                    DeserializeFromStream<ActivityPackage>(reader)
                );
            }

            return activityPackageList;
        }

        internal static List<ActivityPackage> DeserializeListFromStreamLegacy(Stream stream)
        {
            List<ActivityPackage> activityPackageList = null;
            var reader = new BinaryReader(stream);

            var activityPackageLength = reader.ReadInt32();
            activityPackageList = new List<ActivityPackage>(activityPackageLength);

            for (int i = 0; i < activityPackageLength; i++)
            {
                activityPackageList.Add(
                    ActivityPackage.DeserializeFromStreamLegacy(stream)
                );
            }

            return activityPackageList;
        }

        #endregion Serialization
    }
}