using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AdjustSdk.Pcl
{
    public class ActivityPackage
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

        public static Dictionary<string, string> ToDictionary(ActivityPackage activityPackage)
        {
            var callbackParamsJson = JsonConvert.SerializeObject(activityPackage.CallbackParameters);
            var partnerParamsJson = JsonConvert.SerializeObject(activityPackage.PartnerParameters);
            var parametersJson = JsonConvert.SerializeObject(activityPackage.Parameters);

            return new Dictionary<string, string>
            {
                {"Path", activityPackage.Path},
                {"ClientSdk", activityPackage.ClientSdk},
                {"ActivityKind", ActivityKindUtil.ToString(activityPackage.ActivityKind)},
                {"Suffix", activityPackage.Suffix},
                {"Parameters", parametersJson},
                {"CallbackParameters", callbackParamsJson},
                {"PartnerParameters", partnerParamsJson}
            };
        }

        public static ActivityPackage FromDictionary(Dictionary<string, string> activityPackageObjectMap)
        {
            var activityPackage = new ActivityPackage();

            string parametersJson;
            if (activityPackageObjectMap.TryGetValue("Parameters", out parametersJson))
            {
                activityPackage.Parameters =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersJson);
            }

            string callbackParamsJson;
            if (activityPackageObjectMap.TryGetValue("CallbackParameters", out callbackParamsJson))
            {
                activityPackage.CallbackParameters =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(callbackParamsJson);
            }

            string partnerParamsJson;
            if (activityPackageObjectMap.TryGetValue("PartnerParameters", out partnerParamsJson))
            {
                activityPackage.PartnerParameters =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(partnerParamsJson);
            }

            activityPackage.Path = activityPackageObjectMap.ContainsKey("Path") ? activityPackageObjectMap["Path"] : null;
            activityPackage.ClientSdk = activityPackageObjectMap.ContainsKey("ClientSdk") ? activityPackageObjectMap["ClientSdk"] : null;
            activityPackage.Suffix = activityPackageObjectMap.ContainsKey("Suffix") ? activityPackageObjectMap["Suffix"] : null;

            string activityKindString = activityPackageObjectMap.ContainsKey("ActivityKind") ? activityPackageObjectMap["ActivityKind"] : null;
            if (activityKindString != null)
                activityPackage.ActivityKind = ActivityKindUtil.FromString(activityKindString);

            return activityPackage;
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
        
    }
}