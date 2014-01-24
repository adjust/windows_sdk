using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace adeven.AdjustIo.PCL
{
    internal class ActivityPackage
    {
        // data
        internal string Path { get; set; }

        internal string UserAgent { get; set; }

        internal string ClientSdk { get; set; }

        internal Dictionary<string, string> Parameters { get; set; }

        // logs
        internal string Kind { get; set; }

        internal string Suffix { get; set; }

        internal string SuccessMessage()
        {
            return String.Format("Tracked {0}{1}", Kind, Suffix);
        }

        internal string FailureMessage()
        {
            return String.Format("Failed to track {0}{1}", Kind, Suffix);
        }

        public override string ToString()
        {
            return String.Format("{0}{1} {2}",
                Kind, Suffix, Path);
        }

        internal string ExtendedString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("Path:      {0}\n", Path);
            stringBuilder.AppendFormat("UserAgent: {0}\n", UserAgent);
            stringBuilder.AppendFormat("ClientSdk: {0}\n", ClientSdk);

            if (Parameters != null)
            {
                stringBuilder.AppendFormat("Parameters:");
                foreach (var keyValuePair in Parameters)
                {
                    stringBuilder.AppendFormat("\n\t\t{0} {1}", keyValuePair.Key.PadRight(16, ' '), keyValuePair.Value);
                }
            }

            return stringBuilder.ToString();
        }

        #region Serialization

        internal static void SerializeToStream(Stream stream, ActivityPackage activityPackage)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(activityPackage.Path);
                writer.Write(activityPackage.UserAgent);
                writer.Write(activityPackage.ClientSdk);
                writer.Write(activityPackage.Kind);
                writer.Write(activityPackage.Suffix);

                var parametersArray = activityPackage.Parameters.ToArray();
                writer.Write(parametersArray.Length);
                for (int i = 0; i < parametersArray.Length; i++)
                {
                    writer.Write(parametersArray[i].Key);
                    writer.Write(parametersArray[i].Value);
                }
            }
        }

        internal static ActivityPackage DeserializeFromStream(Stream stream)
        {
            ActivityPackage activityPackage = null;
            using (var reader = new BinaryReader(stream))
            {
                activityPackage = new ActivityPackage();
                activityPackage.Path = reader.ReadString();
                activityPackage.UserAgent = reader.ReadString();
                activityPackage.ClientSdk = reader.ReadString();
                activityPackage.Kind = reader.ReadString();
                activityPackage.Suffix = reader.ReadString();

                var parameterLength = reader.ReadInt32();
                activityPackage.Parameters = new Dictionary<string, string>(parameterLength);

                for (int i = 0; i < parameterLength; i++)
                {
                    activityPackage.Parameters.Add(
                        reader.ReadString(),
                        reader.ReadString()
                    );
                }
            }
            return activityPackage;
        }

        internal static void SerializeListToStream(Stream stream, List<ActivityPackage> activityPackageList)
        {
            using (var writer = new BinaryWriter(stream))
            {
                var activityPackageArray = activityPackageList.ToArray();
                writer.Write(activityPackageArray.Length);
                for (int i = 0; i < activityPackageArray.Length; i++)
                {
                    ActivityPackage.SerializeToStream(stream, activityPackageArray[i]);
                }
            }
        }

        internal static List<ActivityPackage> DeserializeListFromStream(Stream stream)
        {
            List<ActivityPackage> activityPackageList = null;
            using (var reader = new BinaryReader(stream))
            {
                var activityPackageLength = reader.ReadInt32();
                activityPackageList = new List<ActivityPackage>(activityPackageLength);

                for (int i = 0; i < activityPackageLength; i++)
                {
                    activityPackageList.Add(
                        ActivityPackage.DeserializeFromStream(stream)
                    );
                }
            }
            return activityPackageList;
        }

        #endregion Serialization
    }
}