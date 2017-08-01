using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdjustSdk;
using AdjustSdk.Pcl;
using Xunit;
using Assert = Xunit.Assert;

namespace WindowsPclTest
{
    public class ActivityPackageTest
    {
        private ActivityPackage GetTestActivityPackage()
        {
            var parameters = new Dictionary<string, string>
            {
                {"pk1", "param1"},
                {"pk2", "param2"},
                {"pk3", "param3"}
            };

            return new ActivityPackage(ActivityKind.Click, "test_client_sdk", parameters)
            {
                CallbackParameters = new Dictionary<string, string>
                {
                    {"pk1", "callbackParam1"}
                },
                PartnerParameters = new Dictionary<string, string>
                {
                    {"pk1", "partnerParam1"},
                    {"pk2", "partnerParam2"}
                }
            };
        }

        [Fact]
        public void ToAndFromDictionaryTest()
        {
            var activityPackage = GetTestActivityPackage();

            var activityPackageDictionary = ActivityPackage.ToDictionary(activityPackage);

            Assert.NotNull(activityPackageDictionary);

            Assert.Equal(activityPackageDictionary.Count, 7);

            Assert.Collection(activityPackageDictionary,
                item => Assert.Contains("Path", item.Key),
                item => Assert.Contains("ClientSdk", item.Key),
                item => Assert.Contains("ActivityKind", item.Key),
                item => Assert.Contains("Suffix", item.Key),
                item => Assert.Contains("Parameters", item.Key),
                item => Assert.Contains("CallbackParameters", item.Key),
                item => Assert.Contains("PartnerParameters", item.Key));

            // check every key-value pair has a non-null value
            Assert.All(activityPackageDictionary, item => Assert.NotNull(item.Value));

            // test extracted activity state - from dictionary
            var extractedActivityPackage = ActivityPackage.FromDictionary(activityPackageDictionary);

            Assert.NotNull(extractedActivityPackage);

            Assert.NotNull(extractedActivityPackage.CallbackParameters);
            Assert.NotNull(extractedActivityPackage.PartnerParameters);
            Assert.NotNull(extractedActivityPackage.Parameters);
            Assert.NotNull(extractedActivityPackage.ActivityKind);
            Assert.NotNull(extractedActivityPackage.ClientSdk);
            Assert.NotNull(extractedActivityPackage.Path);
            Assert.NotNull(extractedActivityPackage.Suffix);

            Assert.Equal(extractedActivityPackage.CallbackParameters.Count, activityPackage.CallbackParameters.Count);
            Assert.Equal(extractedActivityPackage.PartnerParameters.Count, activityPackage.PartnerParameters.Count);
            Assert.Equal(extractedActivityPackage.Parameters.Count, activityPackage.Parameters.Count);
            Assert.Equal(extractedActivityPackage.ActivityKind, activityPackage.ActivityKind);
            Assert.Equal(extractedActivityPackage.ClientSdk, activityPackage.ClientSdk);
            Assert.Equal(extractedActivityPackage.Path, activityPackage.Path);
            Assert.Equal(extractedActivityPackage.Suffix, activityPackage.Suffix);
        }

        [Fact]
        public void LegacyDeserializationTest()
        {
            // create test activity package and serialie it using legacy code
            var activityPackage = GetTestActivityPackage();
            using (MemoryStream memStream = new MemoryStream())
            {
                var writer = new BinaryWriter(memStream);
                writer.Write(activityPackage.Path);
                writer.Write("");
                writer.Write(activityPackage.ClientSdk);
                writer.Write(ActivityKindUtil.ToString(activityPackage.ActivityKind));
                writer.Write(activityPackage.Suffix);

                var parametersArray = activityPackage.Parameters.ToArray();
                writer.Write(parametersArray.Length);
                for (int i = 0; i < parametersArray.Length; i++)
                {
                    writer.Write(parametersArray[i].Key);
                    writer.Write(parametersArray[i].Value);
                }

                // deserialize
                memStream.Seek(0, SeekOrigin.Begin);
                var deserializedActivityPackage = ActivityPackage.DeserializeFromStreamLegacy(memStream);

                Assert.NotNull(deserializedActivityPackage);

                Assert.Null(deserializedActivityPackage.CallbackParameters);
                Assert.Null(deserializedActivityPackage.PartnerParameters);
                Assert.Equal(deserializedActivityPackage.Parameters.Count, activityPackage.Parameters.Count);
                Assert.Equal(deserializedActivityPackage.ActivityKind, activityPackage.ActivityKind);
                Assert.Equal(deserializedActivityPackage.ClientSdk, activityPackage.ClientSdk);
                Assert.Equal(deserializedActivityPackage.Path, activityPackage.Path);
                Assert.Equal(deserializedActivityPackage.Suffix, activityPackage.Suffix);
            }
        }
    }
}
