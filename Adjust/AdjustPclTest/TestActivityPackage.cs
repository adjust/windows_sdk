using AdjustSdk;
using AdjustSdk.Pcl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdjustTest.Pcl
{
    public enum TargetPlatform
    {
        wstore,
        wphone80,
        wphone81,
    }

    public static class UtilTest
    {
        public static bool DictionaryEqual<TKey, TValue>(
            this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            return first.DictionaryEqual(second, null);
        }

        public static bool DictionaryEqual<TKey, TValue>(
            this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second,
            IEqualityComparer<TValue> valueComparer)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue)) return false;
                if (!valueComparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }

        public static ActivityHandler GetActivityHandler(MockLogger mocklogger, DeviceUtil deviceUtil)
        {
            MockAttributionHandler mockAttributionHandler = new MockAttributionHandler(mocklogger);
            MockPackageHandler mockPackageHandler = new MockPackageHandler(mocklogger);

            AdjustFactory.SetAttributionHandler(mockAttributionHandler);
            AdjustFactory.SetPackageHandler(mockPackageHandler);

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // start activity handler with config
            ActivityHandler activityHandler = ActivityHandler.GetInstance(config, deviceUtil);

            deviceUtil.Sleep(3000);

            return activityHandler;
        }

        public static ActivityPackage CreateClickPackage(ActivityHandler activityHandler, string suffix)
        {
            var clickPackage = activityHandler.GetDeeplinkClickPackage(null, null);
            clickPackage.Suffix = suffix;
            
            return clickPackage;
        }
    }

    public class TestActivityPackage
    {
        private ActivityPackage ActivityPackage { get; set; }
        private Dictionary<string, string> Parameters { get; set; }
        public static IAssert Assert { get; set; }
        public static TargetPlatform TargetPlatform { get; set; }
        private string ClientSdk { get { 
            switch(TargetPlatform)
            {
                case TargetPlatform.wstore: return "wstore" + Version;
                case TargetPlatform.wphone80: return "wphone80-" + Version;
                case TargetPlatform.wphone81: return "wphone81-" + Version;
                default: return null;
            }
        } }

        // config/device
        public string AppToken { get; set; }
        public string Environment { get; set; }
        public string Version { get; set; }
        public bool? NeedsAttributionData { get; set; }

        // session
        public int? SessionCount { get; set; }
        public string DefaultTracker { get; set; }
        public int? SubsessionCount { get; set; }

        // event
        public string EventToken { get; set; }
        public string EventCount { get; set; }
        public string Suffix { get; set; }
        public string RevenueString { get; set; }
        public string Currency { get; set; }
        public string CallbackParams { get; set; }
        public string PartnerParams { get; set; }
        // click
        public string DeepLinkParameters { get; set; }
        public AdjustAttribution Attribution { get; set; }
 
        public TestActivityPackage(ActivityPackage activityPackage)
        {
            ActivityPackage = activityPackage;
            Parameters = activityPackage.Parameters;
               
            // default values
            AppToken = "123456789012";
            Environment = "sandbox";
            Version = "4.0.0";
            Suffix = "";
            Attribution = new AdjustAttribution();
            NeedsAttributionData = false;
        }

        public void TestSessionPackage(int sessionCount)
        {
            // set the session count
            SessionCount = sessionCount;

            // test default package attributes
            TestDefaultAttributes("/session", ActivityKind.Session, "session");

            // check default parameters
            TestDefaultParameters();

            // session parameters
            // last_interval
            if (sessionCount == 1)
            {
                AssertParameterNull("last_interval");
            }
            else
            {
                AssertParameterNotNull("last_interval");
            }
            // default_tracker
            AssertParameterEquals("default_tracker", DefaultTracker);
        }

        public void TestEventPackage(string eventToken)
        {
            // set the event token
            EventToken = eventToken;

            // test default package attributes
            TestDefaultAttributes("/event", ActivityKind.Event, "event");

            // check default parameters
            TestDefaultParameters();

            // event parameters
            // event_count
            if (EventCount == null)
            {
                AssertParameterNotNull("event_count");
            }
            else
            {
                AssertParameterEquals("event_count", EventCount);
            }
            // event_token
            AssertParameterEquals("event_token", eventToken);
            // revenue and currency must come together
            if (GetParameter("revenue") != null &&
                    GetParameter("currency") == null)
            {
                AssertFail();
            }
            if (GetParameter("revenue") == null &&
                    GetParameter("currency") != null)
            {
                AssertFail();
            }
            // revenue
            AssertParameterEquals("revenue", RevenueString);
            // currency
            AssertParameterEquals("currency", Currency);
            // callback_params
            AssertJsonParameterEquals("callback_params", CallbackParams);
            // partner_params
            AssertJsonParameterEquals("partner_params", PartnerParams);
        }

        public void TestClickPackage(string source)
        {
            // test default package attributes
            TestDefaultAttributes("/sdk_click", ActivityKind.Click, "click");

            // check device ids parameters
            TestDeviceIdsParameters();

            // click parameters
            // source
            AssertParameterEquals("source", source);

            // params
            AssertJsonParameterEquals("params", DeepLinkParameters);

            // click_time
            // TODO add string click time to compare
            AssertParameterNotNull("click_time");

            // attributions
            if (Attribution != null)
            {
                // tracker
                AssertParameterEquals("tracker", Attribution.TrackerName);
                // campaign
                AssertParameterEquals("campaign", Attribution.Campaign);
                // adgroup
                AssertParameterEquals("adgroup", Attribution.Adgroup);
                // creative
                AssertParameterEquals("creative", Attribution.Creative);
            }
        }

        public void TestAttributionPackage()
        {
            // test default package attributes
            TestDefaultAttributes("/attribution", ActivityKind.Attribution, "attribution");

            TestDeviceIdsParameters();
        }


        private void TestDefaultParameters()
        {
            TestDeviceInfo();
            TestConfig();
            TestActivityState();
            TestCreatedAt();
        }

        private void TestDeviceIdsParameters()
        {
            TestDeviceInfoIds();
            TestConfig();
            TestCreatedAt();
        }

        private void TestCreatedAt()
        {
            // created_at
            AssertParameterNotNull("created_at");
        }

        private void TestDeviceInfo()
        {
            TestDeviceInfoIds();
            
            // app_display_name
            //AssertParameterNotNull("app_display_name");
            // app_name
            // app_author
            // device_name
            // win_adid
            if (TargetPlatform == TargetPlatform.wphone80)
            {
                AssertParameterNotNull("app_name");
                AssertParameterNotNull("app_author");
                AssertParameterNotNull("device_name");                
                AssertParameterNull("architecture");
            }
            else
            {
                AssertParameterNull("app_name");
                AssertParameterNull("app_author");
                AssertParameterNull("device_name");
                AssertParameterNotNull("architecture");
            }
            // app_version
            AssertParameterNotNull("app_version");
            // app_publisher
            AssertParameterNotNull("app_publisher");
            // device_type
            AssertParameterNotNull("device_type");
            // device_manufacturer
            AssertParameterNotNull("device_manufacturer");
            // os_name
            AssertParameterNotNull("os_name");
            // os_version
            AssertParameterNotNull("os_version");
            // language
            AssertParameterNotNull("language");
            // country
            AssertParameterNotNull("country");
        }

        private void TestDeviceInfoIds()
        {
            if (TargetPlatform  == TargetPlatform.wphone80)
            {
                AssertParameterNotNull("win_udid");
                AssertParameterNull("win_hwid");
                AssertParameterNull("win_naid");
                AssertParameterNull("win_adid");
            } 
            else 
            {
                AssertParameterNull("win_udid");
                AssertParameterNotNull("win_hwid");
                AssertParameterNotNull("win_naid");
                AssertParameterNotNull("win_adid");
            }
        }

        private void TestConfig()
        {
            // app_token
            AssertParameterEquals("app_token", AppToken);
            // environment
            AssertParameterEquals("environment", Environment);
            // needs_attribution_data
            TestParameterBoolean("needs_attribution_data", NeedsAttributionData);
        }

        private void TestActivityState()
        {
            // session_count
            if (!SessionCount.HasValue)
            {
                AssertParameterNotNull("session_count");
            }
            else
            {
                AssertParameterEquals("session_count", SessionCount.Value);
            }

            // subsession_count
            // session_length
            // time_spent

            // win_uuid
            AssertParameterNotNull("win_uuid");

            // first session
            if (SessionCount == 1)
            {
                // subsession_count
                AssertParameterNull("subsession_count");
                // session_length
                AssertParameterNull("session_length");
                // time_spent
                AssertParameterNull("time_spent");
            }
            else
            {
                // subsession_count
                if (!SubsessionCount.HasValue)
                {
                    AssertParameterNotNull("subsession_count");
                }
                else
                {
                    AssertParameterEquals("subsession_count", SubsessionCount.Value);
                }
                // session_length
                AssertParameterNotNull("session_length");
                // time_spent
                AssertParameterNotNull("time_spent");
            }
        }
                
        private void TestDefaultAttributes(string path, ActivityKind activityKind, string activityKindString)
        {
            // check the Sdk version is being tested
            AssertEquals(ActivityPackage.ClientSdk, ClientSdk);
            // check the path
            AssertEquals(ActivityPackage.Path, path);
            // test activity kind
            // check the activity kind
            AssertEquals(ActivityPackage.ActivityKind, activityKind);
            // the conversion from activity kind to String
            AssertEquals(ActivityKindUtil.ToString(ActivityPackage.ActivityKind), activityKindString);
            // the conversion from String to activity kind
            AssertEquals(ActivityPackage.ActivityKind, ActivityKindUtil.FromString(activityKindString));
            // test suffix
            AssertEquals(ActivityPackage.Suffix, Suffix);
        }

        private void AssertParameterNotNull(string parameterName)
        {
            Assert.IsTrue(Parameters.TryGetValue(parameterName, out parameterName), 
                ActivityPackage.GetExtendedString());
        }

        private void AssertParameterNull(string parameterName)
        {
            Assert.IsFalse(Parameters.TryGetValue(parameterName, out parameterName),
                ActivityPackage.GetExtendedString());
        }

        private void AssertParameterEquals(string parameterName, string value)
        {
            if (value == null)
            {
                AssertParameterNull(parameterName);
                return;
            }
            AssertEquals(value, GetParameter(parameterName));
        }

        private void AssertParameterEquals(string parameterName, int value)
        {
            AssertEquals(value.ToString() , GetParameter(parameterName));
        }

        private void AssertEquals<T>(T expected, T actual)
        {
            Assert.AreEqual(expected, actual, 
                ActivityPackage.GetExtendedString());
        }

        private void AssertFail()
        {
            Assert.Fail(ActivityPackage.GetExtendedString());
        }

        private void AssertTrue(bool condition)
        {
            Assert.IsTrue(condition,
                ActivityPackage.GetExtendedString());
        }

        private void AssertJsonParameterEquals(string parameterName, string value)
        {
            if (value == null)
            {
                AssertParameterNull(parameterName);
                return;
            }

            try
            {
                var paramJsonString = GetParameter(parameterName);
                var paramJsonDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(paramJsonString);
                var valueJsonDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);

                AssertTrue(paramJsonDic.DictionaryEqual(valueJsonDic));
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }                        
        }

        private string GetParameter(string parameterName)
        {
            string parameterValue;
            if (Parameters.TryGetValue(parameterName, out parameterValue))
            {
                return parameterValue;
            }
            return null;
        }

        private void TestParameterBoolean(string parameterName, bool? value)
        {
            if (!value.HasValue)
            {
                AssertParameterNull(parameterName);
            }
            else if (value.Value)
            {
                AssertParameterEquals(parameterName, "1");
            }
            else
            {
                AssertParameterEquals(parameterName, "0");
            }
        }
    }
}
