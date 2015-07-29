using AdjustSdk;
using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;

namespace AdjustTest.Pcl
{
    public class TestActivityHandler : TestTemplate
    {
        private MockPackageHandler MockPackageHandler { get; set; }
        private MockAttributionHandler MockAttributionHandler { get; set; }

        private TargetPlatform TargetPlatform { get; set; }

        public TestActivityHandler(DeviceUtil deviceUtil, IAssert assert, TargetPlatform targetPlatform)
            : base(deviceUtil, assert)
        {
            TargetPlatform = targetPlatform;
            TestActivityPackage.Assert = Assert;
            TestActivityPackage.TargetPlatform = TargetPlatform;
        }

        public override void SetUp()
        {
            base.SetUp();

            MockPackageHandler = new MockPackageHandler(MockLogger);
            MockAttributionHandler = new MockAttributionHandler(MockLogger);

            AdjustFactory.Logger = MockLogger;
            AdjustFactory.SetPackageHandler(MockPackageHandler);
            AdjustFactory.SetAttributionHandler(MockAttributionHandler);
            
            // deleting the activity state file to simulate a first session
            var activityStateDeleted = Util.DeleteFile("AdjustIOActivityState");
            var attributionDeleted = Util.DeleteFile("AdjustAttribution");

            MockLogger.Test("Was the activity state file deleted? {0}", activityStateDeleted);

            MockLogger.Test("Was the attribution file deleted? {0}", attributionDeleted);
        }

        public override void TearDown()
        {
            AdjustFactory.SetPackageHandler(null);
            AdjustFactory.SetAttributionHandler(null);
            AdjustFactory.SetSessionInterval(null);
            AdjustFactory.SetSubsessionInterval(null);
            AdjustFactory.SetTimerInterval(null);
            AdjustFactory.SetTimerStart(null);
            AdjustFactory.Logger = null;
        }

        public void TestFirstSession()
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig("123456789012", AdjustConfig.EnvironmentSandbox);

            config.LogDelegate = (msg => System.Diagnostics.Debug.WriteLine(msg));

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(AdjustConfig.EnvironmentSandbox, "Info", false);

            // test first session start
            CheckFirstSession();

            // checking the default values of the first session package
            // should only have one package
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            ActivityPackage activityPackage = MockPackageHandler.PackageQueue[0];

            // create activity package test
            TestActivityPackage testActivityPackage = new TestActivityPackage(activityPackage);

            // set first session
            testActivityPackage.TestSessionPackage(1);
        }

        public void TestEventsBuffered()
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig("123456789012", AdjustConfig.EnvironmentSandbox);

            config.LogDelegate = (msg => System.Diagnostics.Debug.WriteLine(msg));

            // buffer events
            config.EventBufferingEnabled = true;

            // set verbose log level
            config.LogLevel = LogLevel.Verbose;

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);
            
            // test init values
            InitTests(AdjustConfig.EnvironmentSandbox, "Verbose", true);

            // test first session start
            CheckFirstSession();

            // create the first Event
            AdjustEvent firstEvent = new AdjustEvent("event1");

            // add callback parameters
            firstEvent.AddCallbackParameter("keyCall", "valueCall");
            firstEvent.AddCallbackParameter("keyCall", "valueCall2");
            firstEvent.AddCallbackParameter("fooCall", "barCall");

            // add partner paramters
            firstEvent.AddPartnerParameter("keyPartner", "valuePartner");
            firstEvent.AddPartnerParameter("keyPartner", "valuePartner2");
            firstEvent.AddPartnerParameter("fooPartner", "barPartner");

            // add revenue
            firstEvent.SetRevenue(0.001, "EUR");

            // track event
            activityHandler.TrackEvent(firstEvent);

            // create the second Event
            AdjustEvent secondEvent = new AdjustEvent("event2");

            // add empty revenue
            secondEvent.SetRevenue(0, "USD");

            // track second event
            activityHandler.TrackEvent(secondEvent);

            // create third Event
            AdjustEvent thirdEvent = new AdjustEvent("event3");

            // track third event
            activityHandler.TrackEvent(thirdEvent);

            DeviceUtil.Sleep(3000);

            // test first event
            // check that callback parameter was overwritten
            Assert.Warn("key keyCall was overwritten");

            // check that partner parameter was overwritten
            Assert.Warn("key keyPartner was overwritten");

            // check that event package was added
            Assert.Test("PackageHandler AddPackage");

            // check that event was buffered
            Assert.Info("Buffered event (0.00100 EUR, 'event1')");

            // and not sent to package handler
            Assert.NotTest("PackageHandler SendFirstPackage");

            // after tracking the event it should write the activity state
            Assert.Debug("Wrote Activity state");

            // test second event
            // check that event package was added
            Assert.Test("PackageHandler AddPackage");

            // check that event was buffered
            Assert.Info("Buffered event (0.00000 USD, 'event2')");

            // and not sent to package handler
            Assert.NotTest("PackageHandler SendFirstPackage");

            // after tracking the event it should write the activity state
            Assert.Debug("Wrote Activity state");

            // test third event
            // check that event package was added
            Assert.Test("PackageHandler AddPackage");

            // check that event was buffered
            Assert.Info("Buffered event 'event3'");

            // and not sent to package handler
            Assert.NotTest("PackageHandler SendFirstPackage");

            // after tracking the event it should write the activity state
            Assert.Debug("Wrote Activity state");

            // check the number of activity packages
            // 1 session + 3 events
            Assert.AreEqual(4, MockPackageHandler.PackageQueue.Count);

            ActivityPackage firstSessionPackage = MockPackageHandler.PackageQueue[0];

            // create activity package test
            TestActivityPackage testFirstSessionPackage = new TestActivityPackage(firstSessionPackage);

            // set first session
            testFirstSessionPackage.TestSessionPackage(1);

            // first event
            ActivityPackage firstEventPackage = MockPackageHandler.PackageQueue[1];

            // create event package test
            TestActivityPackage testFirstEventPackage = new TestActivityPackage(firstEventPackage);

            // set event test parameters
            testFirstEventPackage.EventCount = "1";
            testFirstEventPackage.Suffix = "(0.00100 EUR, 'event1')";
            testFirstEventPackage.RevenueString = "0.00100";
            testFirstEventPackage.Currency = "EUR";
            testFirstEventPackage.CallbackParams = "{\"fooCall\":\"barCall\",\"keyCall\":\"valueCall2\"}";
            testFirstEventPackage.PartnerParams = "{\"keyPartner\":\"valuePartner2\",\"fooPartner\":\"barPartner\"}";

            // test first event
            testFirstEventPackage.TestEventPackage("event1");

            // second event
            ActivityPackage secondEventPackage = MockPackageHandler.PackageQueue[2];

            // create event package test
            TestActivityPackage testSecondEventPackage = new TestActivityPackage(secondEventPackage);

            // set event test parameters
            testSecondEventPackage.EventCount = "2";
            testSecondEventPackage.Suffix = "(0.00000 USD, 'event2')";
            testSecondEventPackage.RevenueString = "0.00000";
            testSecondEventPackage.Currency = "USD";

            // test second event
            testSecondEventPackage.TestEventPackage("event2");

            // third event
            ActivityPackage thirdEventPackage = MockPackageHandler.PackageQueue[3];

            // create event package test
            TestActivityPackage testThirdEventPackage = new TestActivityPackage(thirdEventPackage);

            // set event test parameters
            testThirdEventPackage.EventCount = "3";
            testThirdEventPackage.Suffix = "'event3'";

            // test third event
            testThirdEventPackage.TestEventPackage("event3");
        }

        public void TestEventsNotBuffered()
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig("123456789012", AdjustConfig.EnvironmentSandbox);

            config.LogDelegate = (msg => System.Diagnostics.Debug.WriteLine(msg));
            
            // set log level
            config.LogLevel = LogLevel.Debug;

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(AdjustConfig.EnvironmentSandbox, "Debug", false);

            // test first session start
            CheckFirstSession();

            // create the first Event
            AdjustEvent firstEvent = new AdjustEvent("event1");

            // track event
            activityHandler.TrackEvent(firstEvent);

            DeviceUtil.Sleep(2000);

            // check that event package was added
            Assert.Test("PackageHandler AddPackage");

            // check that event was sent to package handler
            Assert.Test("PackageHandler SendFirstPackage");

            // and not buffered
            Assert.NotInfo("Buffered event");

            // after tracking the event it should write the activity state
            Assert.Debug("Wrote Activity state");
        }

        public void testChecks() 
        {
            // config with null app token
            AdjustConfig nullAppTokenConfig = new AdjustConfig(null, AdjustConfig.EnvironmentSandbox);

            Assert.Error("Missing App Token");
            Assert.IsFalse(nullAppTokenConfig.IsValid());

            // config with wrong size app token
            AdjustConfig oversizeAppTokenConfig = new AdjustConfig("1234567890123", AdjustConfig.EnvironmentSandbox);

            Assert.Error("Malformed App Token '1234567890123'");
            Assert.IsFalse(oversizeAppTokenConfig.IsValid());

            // config with null environment
            AdjustConfig nullEnvironmentConfig = new AdjustConfig("123456789012", null);

            Assert.Error("Missing environment");
            Assert.IsFalse(nullEnvironmentConfig.IsValid());

            // config with wrong environment
            AdjustConfig wrongEnvironmentConfig = new AdjustConfig("123456789012", "Other");

            Assert.Error("Unknown environment 'Other'");
            Assert.IsFalse(wrongEnvironmentConfig.IsValid());

            // start with null config
            ActivityHandler nullConfigactivityHandler = ActivityHandler.GetInstance(null, DeviceUtil);

            Assert.Error("AdjustConfig missing");
            Assert.Null(nullConfigactivityHandler);

            ActivityHandler invalidConfigactivityHandler = ActivityHandler.GetInstance(nullAppTokenConfig, DeviceUtil);

            Assert.Error("AdjustConfig not initialized correctly");
            Assert.Null(invalidConfigactivityHandler);

            // event with null event token
            AdjustEvent nullEventToken = new AdjustEvent(null);

            Assert.Error("Missing Event Token");
            Assert.IsFalse(nullEventToken.IsValid());

            // event with wrong size
            AdjustEvent wrongEventTokenSize = new AdjustEvent("eventXX");

            Assert.Error("Malformed Event Token 'eventXX'");
            Assert.IsFalse(wrongEventTokenSize.IsValid());

            // event
            AdjustEvent adjustEvent = new AdjustEvent("event1");

            // event with negative revenue
            adjustEvent.SetRevenue(-0.001, "EUR");

            Assert.Error("Invalid amount -0.001");

            // event with null currency
            adjustEvent.SetRevenue(0, null);

            Assert.Error("Currency must be set with revenue");

            // event with empty currency
            adjustEvent.SetRevenue(0, "");

            Assert.Error("Currency is empty");

            // callback parameter null key
            adjustEvent.AddCallbackParameter(null, "callValue");

            Assert.Error("Callback parameter key is missing");

            // callback parameter empty key
            adjustEvent.AddCallbackParameter("", "callValue");

            Assert.Error("Callback parameter key is empty");

            // callback parameter null value
            adjustEvent.AddCallbackParameter("keyCall", null);

            Assert.Error("Callback parameter value is missing");

            // callback parameter empty value
            adjustEvent.AddCallbackParameter("keyCall", "");

            Assert.Error("Callback parameter value is empty");

            // partner parameter null key
            adjustEvent.AddPartnerParameter(null, "callValue");

            Assert.Error("Partner parameter key is missing");

            // partner parameter empty key
            adjustEvent.AddPartnerParameter("", "callValue");

            Assert.Error("Partner parameter key is empty");

            // partner parameter null value
            adjustEvent.AddPartnerParameter("keyCall", null);

            Assert.Error("Partner parameter value is missing");

            // partner parameter empty value
            adjustEvent.AddPartnerParameter("keyCall", "");

            Assert.Error("Partner parameter value is empty");

            // create the config to start the session
            AdjustConfig config = new AdjustConfig("123456789012", AdjustConfig.EnvironmentSandbox);

            // set the log level
            config.LogLevel = LogLevel.Warn;

            // create handler and start the first session
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(AdjustConfig.EnvironmentSandbox, "Warn", false);

            // test first session start
            CheckFirstSession();

            // track null event
            activityHandler.TrackEvent(null);
            DeviceUtil.Sleep(1000);

            Assert.Error("Event missing");

            activityHandler.TrackEvent(nullEventToken);
            DeviceUtil.Sleep(1000);

            Assert.Error("Event not initialized correctly");
        }

        public void TestSessions()
        {
            // adjust the session intervals for testing
            AdjustFactory.SetSessionInterval(new TimeSpan(0,0,0,0,4000));
            AdjustFactory.SetSubsessionInterval(new TimeSpan(0,0,0,0,1000));

            // create the config to start the session
            AdjustConfig config = new AdjustConfig("123456789012", AdjustConfig.EnvironmentSandbox);

            // set verbose log level
            config.LogLevel = LogLevel.Info;

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests("sandbox", "Info", false);

            // test first session start
            CheckFirstSession();

            // trigger a new sub session session
            activityHandler.TrackSubsessionStart();

            // and end it
            activityHandler.TrackSubsessionEnd();

            DeviceUtil.Sleep(5000);

            // test the new sub session
            CheckSubsession(
                sessionCount: 1,
                subsessionCount: 2,
                timerAlreadyStarted: true);

            // test the end of the subsession
            CheckEndSession();

            // trigger a new session
            activityHandler.TrackSubsessionStart();

            DeviceUtil.Sleep(1000);

            // new session
            CheckNewSession(
                paused:false,
                sessionCount: 2,
                eventCount: 0,
                timerAlreadyStarted: true);

            // end the session
            activityHandler.TrackSubsessionEnd();

            DeviceUtil.Sleep(1000);

            // 2 session packages
            Assert.AreEqual(2, MockPackageHandler.PackageQueue.Count);

            ActivityPackage firstSessionActivityPackage = MockPackageHandler.PackageQueue[0];

            // create activity package test
            TestActivityPackage testFirstSessionActivityPackage = new TestActivityPackage(firstSessionActivityPackage);

            // test first session
            testFirstSessionActivityPackage.TestSessionPackage(1);

            // get second session package
            ActivityPackage secondSessionActivityPackage = MockPackageHandler.PackageQueue[1];

            // create second session test package
            TestActivityPackage testSecondSessionActivityPackage = new TestActivityPackage(secondSessionActivityPackage);

            // check if it saved the second subsession in the new package
            testSecondSessionActivityPackage.SubsessionCount = 2;

            // test second session
            testSecondSessionActivityPackage.TestSessionPackage(2);
        }

        public void TestDisable()
        {
            // adjust the session intervals for testing
            AdjustFactory.SetSessionInterval(new TimeSpan(0,0,0,0,4000));
            AdjustFactory.SetSubsessionInterval(new TimeSpan(0,0,0,0,1000));

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // set log level
            config.LogLevel = LogLevel.Error;

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            // check that is true by default
            Assert.IsTrue(activityHandler.IsEnabled());

            // disable sdk
            activityHandler.SetEnabled(false);

            // check that it is disabled
            Assert.IsFalse(activityHandler.IsEnabled());

            // not writing activity state because it did not had time to start
            Assert.NotDebug("Wrote Activity state");

            // check if message the disable of the SDK
            Assert.Info("Pausing package and attribution handler to disable the SDK");

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            DeviceUtil.Sleep(2000);

            // test init values
            InitTests(environment: "sandbox", logLevel: "Error");

            // test first session start without attribution handler
            CheckFirstSession(paused: true);

            // test end session of disable
            CheckEndSession();

            // try to do activities while SDK disabled
            activityHandler.TrackSubsessionStart();
            activityHandler.TrackEvent(new AdjustEvent("event1"));

            DeviceUtil.Sleep(3000);

            // check that timer was not executed
            CheckTimerIsFired(false);

            // check that it did not resume
            Assert.NotTest("PackageHandler ResumeSending");

            // check that it did not wrote activity state from new session or subsession
            Assert.NotDebug("Wrote Activity state");

            // check that it did not add any event package
            Assert.NotTest("PackageHandler AddPackage");

            // only the first session package should be sent
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            // put in offline mode
            activityHandler.SetOfflineMode(true);

            // pausing due to offline mode
            Assert.Info("Pausing package and attribution handler to put in offline mode");

            // wait to update status
            DeviceUtil.Sleep(6000);

            // test end session of offline
            CheckEndSession(updateActivityState: false);

            // re-enable the SDK
            activityHandler.SetEnabled(true);

            // check that it is enabled
            Assert.IsTrue(activityHandler.IsEnabled());

            // check message of SDK still paused
            Assert.Info("Package and attribution handler remain paused due to the SDK is offline");

            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(1000);

            CheckNewSession(paused: true, sessionCount: 2);

            // and that the timer is not fired
            CheckTimerIsFired(false);

            // track an event
            activityHandler.TrackEvent(new AdjustEvent("event1"));

            // and that the timer is not fired
            DeviceUtil.Sleep(1000);

            // check that it did add the event package
            Assert.Test("PackageHandler AddPackage");

            // and send it
            Assert.Test("PackageHandler SendFirstPackage");

            // it should have the second session and the event
            Assert.AreEqual(3, MockPackageHandler.PackageQueue.Count);

            ActivityPackage secondSessionPackage = MockPackageHandler.PackageQueue[1];

            // create activity package test
            TestActivityPackage testSecondSessionPackage = new TestActivityPackage(secondSessionPackage);

            // set the sub sessions
            testSecondSessionPackage.SubsessionCount = 1;

            // test second session
            testSecondSessionPackage.TestSessionPackage(sessionCount: 2);

            ActivityPackage eventPackage = MockPackageHandler.PackageQueue[2];

            // create activity package test
            TestActivityPackage testEventPackage = new TestActivityPackage(eventPackage);

            testEventPackage.Suffix = "'event1'";

            // test event
            testEventPackage.TestEventPackage(eventToken: "event1");

            // put in online mode
            activityHandler.SetOfflineMode(false);

            // message that is finally resuming
            Assert.Info("Resuming package and attribution handler to put in online mode");

            DeviceUtil.Sleep(6000);

            // check status update
            Assert.Test("AttributionHandler ResumeSending");
            Assert.Test("PackageHandler ResumeSending");

            // track sub session
            activityHandler.TrackSubsessionStart();

            DeviceUtil.Sleep(1000);

            // test session not paused
            CheckNewSession(paused: false, sessionCount: 3, eventCount: 1, timerAlreadyStarted: true);
        }

        public void TestOpenUrl()
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // set log level
            config.LogLevel = LogLevel.Assert;

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(environment: "sandbox", logLevel: "Assert");

            // test first session start
            CheckFirstSession();

            var attributions = new Uri("AdjustTests://example.com/path/inApp?adjust_tracker=trackerValue&other=stuff&adjust_campaign=campaignValue&adjust_adgroup=adgroupValue&adjust_creative=creativeValue");
            var extraParams = new Uri("AdjustTests://example.com/path/inApp?adjust_foo=bar&other=stuff&adjust_key=value");
            var mixed = new Uri("AdjustTests://example.com/path/inApp?adjust_foo=bar&other=stuff&adjust_campaign=campaignValue&adjust_adgroup=adgroupValue&adjust_creative=creativeValue");
            var emptyQueryString = new Uri("AdjustTests://");
            Uri nullUri = null;
            var single = new Uri("AdjustTests://example.com/path/inApp?adjust_foo");
            var prefix = new Uri("AdjustTests://example.com/path/inApp?adjust_=bar");
            var incomplete = new Uri("AdjustTests://example.com/path/inApp?adjust_foo=");

            activityHandler.OpenUrl(attributions);
            activityHandler.OpenUrl(extraParams);
            activityHandler.OpenUrl(mixed);
            activityHandler.OpenUrl(emptyQueryString);
            activityHandler.OpenUrl(nullUri);
            activityHandler.OpenUrl(single);
            activityHandler.OpenUrl(prefix);
            activityHandler.OpenUrl(incomplete);

            DeviceUtil.Sleep(1000);

            // three click packages: attributions, extraParams and mixed
            for (int i = 3; i > 0; i--)
            {
                Assert.Test("PackageHandler AddPackage");
            }

            // checking the default values of the first session package
            // 1 session + 3 click
            Assert.AreEqual(4, MockPackageHandler.PackageQueue.Count);

            // get the click package
            ActivityPackage attributionClickPackage = MockPackageHandler.PackageQueue[1];

            // create activity package test
            TestActivityPackage testAttributionClickPackage = new TestActivityPackage(attributionClickPackage)
            {
                Attribution = new AdjustAttribution
                {
                    TrackerName = "trackerValue",
                    Campaign = "campaignValue",
                    Adgroup = "adgroupValue",
                    Creative = "creativeValue",
                },
            };

            // test the first deeplink
            testAttributionClickPackage.TestClickPackage(source: "deeplink");

            // get the click package
            ActivityPackage extraParamsClickPackage = MockPackageHandler.PackageQueue[2];

            // create activity package test
            TestActivityPackage testExtraParamsClickPackage = new TestActivityPackage(extraParamsClickPackage)
            {
                DeepLinkParameters = "{\"key\":\"value\",\"foo\":\"bar\"}",
            };

            // test the second deeplink
            testExtraParamsClickPackage.TestClickPackage(source: "deeplink");

            // get the click package
            ActivityPackage mixedClickPackage = MockPackageHandler.PackageQueue[3];

            // create activity package test
            TestActivityPackage testMixedClickPackage = new TestActivityPackage(mixedClickPackage)
            {
                Attribution = new AdjustAttribution
                {
                    Campaign = "campaignValue",
                    Adgroup = "adgroupValue",
                    Creative = "creativeValue",
                },
                DeepLinkParameters = "{\"foo\":\"bar\"}",
            };

            // test the third deeplink
            testMixedClickPackage.TestClickPackage(source: "deeplink");
        }

        public void TestFinishedTrackingActivity() 
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentProduction);

            // set verbose log level
            config.LogLevel = LogLevel.Verbose;

            config.AttributionChanged = (attribution) => MockLogger.Test("AttributionChanged: {0}", attribution);
            
            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(environment: "production" , logLevel: "Assert");

            // test first session start
            CheckFirstSession();

            Dictionary<string, string> responseNull = null;

            activityHandler.FinishedTrackingActivity(responseNull);
            DeviceUtil.Sleep(1000);

            // if the response is null
            Assert.NotError("Malformed deeplink");
            Assert.NotInfo("Open deep link");

            // set package handler to respond with a valid attribution
            var malformedDeeplinkResponse = new Dictionary<string, string> {{"deeplink" , "<malformedUrl>"}};

            activityHandler.FinishedTrackingActivity(malformedDeeplinkResponse);
            DeviceUtil.Sleep(1000);

            // check that it was unable to open the url
            Assert.Error("Malformed deeplink '<malformedUrl>'");

            // checking the default values of the first session package
            // should only have one package
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            ActivityPackage activityPackage = MockPackageHandler.PackageQueue[0];

            // create activity package test
            TestActivityPackage testActivityPackage = new TestActivityPackage(activityPackage)
            {
                NeedsAttributionData = true,
                Environment = "production",
            };

            // set first session
            testActivityPackage.TestSessionPackage(sessionCount: 1);
        }
        
        public void TestUpdateAttribution() 
        {
            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            config.LogLevel = LogLevel.Verbose;

            // start activity handler with config
            ActivityHandler firstActivityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(2000);

            // test init values
            InitTests(environment: "sandbox", logLevel: "Verbose");

            // test first session start
            CheckFirstSession();

            string nullJsonString = null;

            AdjustAttribution nullAttribution = AdjustAttribution.FromJsonString(nullJsonString);
            
            // check if Attribution wasn't built
            Assert.Null(nullAttribution);

            // check that it does not update a null attribution
            Assert.IsFalse(firstActivityHandler.UpdateAttribution(nullAttribution));
            
            // create an empty attribution
            var emptyJsonString = "{ }";
            AdjustAttribution emptyAttribution = AdjustAttribution.FromJsonString(emptyJsonString);

            // check that updates attribution
            Assert.IsTrue(firstActivityHandler.UpdateAttribution(emptyAttribution));
            Assert.Debug("Wrote Attribution: tt:Null tn:Null net:Null cam:Null adg:Null cre:Null cl:Null");
            
            emptyAttribution = AdjustAttribution.FromJsonString(emptyJsonString);

            // check that it does not update the attribution
            Assert.IsFalse(firstActivityHandler.UpdateAttribution(emptyAttribution));
            Assert.NotDebug("Wrote Attribution");
            
            // end session
            firstActivityHandler.TrackSubsessionEnd();
            DeviceUtil.Sleep(1000);

            CheckEndSession();

            config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            config.AttributionChanged = (attribution) => MockLogger.Test("onAttributionChanged: {0}", attribution);

            config.LogLevel = LogLevel.Debug;

            ActivityHandler restartActivityHandler = GetActivityHandler(config);
            
            DeviceUtil.Sleep(3000);

            // test init values
            InitTests(environment: "sandbox", logLevel: "Debug", 
                readActivityState: "ec:0 sc:1 ssc:1",
                readAttribution: "tt:Null tn:Null net:Null cam:Null adg:Null cre:Null cl:Null");
            
            CheckSubsession(subsessionCount: 2);
            
            // check that it does not update the attribution after the restart
            Assert.IsFalse(restartActivityHandler.UpdateAttribution(emptyAttribution));
            Assert.NotDebug("Wrote Attribution");

            // new attribution
            var firstAttributionJsonString = "{ " +
                        "\"tracker_token\" : \"ttValue\" , " +
                        "\"tracker_name\"  : \"tnValue\" , " +
                        "\"network\"       : \"nValue\" , " +
                        "\"campaign\"      : \"cpValue\" , " +
                        "\"adgroup\"       : \"aValue\" , " +
                        "\"creative\"      : \"ctValue\" , " +
                        "\"click_label\"   : \"clValue\" }";
            AdjustAttribution firstAttribution = AdjustAttribution.FromJsonString(firstAttributionJsonString);

            //check that it updates
            Assert.IsTrue(restartActivityHandler.UpdateAttribution(firstAttribution));
            Assert.Debug("Wrote Attribution: tt:ttValue tn:tnValue net:nValue cam:cpValue adg:aValue cre:ctValue cl:clValue");

            // check that it launch the saved attribute
            DeviceUtil.Sleep(1000);

            Assert.Test("onAttributionChanged: tt:ttValue tn:tnValue net:nValue cam:cpValue adg:aValue cre:ctValue cl:clValue");

            // check that it does not update the attribution
            Assert.IsFalse(restartActivityHandler.UpdateAttribution(firstAttribution));
            Assert.NotDebug("Wrote Attribution");

            // end session
            restartActivityHandler.TrackSubsessionEnd();
            DeviceUtil.Sleep(1000);

            CheckEndSession();

            config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            config.LogLevel = LogLevel.Info;

            config.AttributionChanged = (attribution) => MockLogger.Test("onAttributionChanged: {0}", attribution);

            ActivityHandler secondRestartActivityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(1000);

            // test init values
            InitTests(environment: "sandbox" , logLevel:"Info", 
                readActivityState: "ec:0 sc:1 ssc:2", 
                readAttribution: "tt:ttValue tn:tnValue net:nValue cam:cpValue adg:aValue cre:ctValue cl:clValue");

            CheckSubsession(subsessionCount: 3);

            // check that it does not update the attribution after the restart
            Assert.IsFalse(secondRestartActivityHandler.UpdateAttribution(firstAttribution));
            Assert.NotDebug("Wrote Attribution");

            // new attribution
            var secondAttributionJson = "{ " +
                        "\"tracker_token\" : \"ttValue2\" , " +
                        "\"tracker_name\"  : \"tnValue2\" , " +
                        "\"network\"       : \"nValue2\" , " +
                        "\"campaign\"      : \"cpValue2\" , " +
                        "\"adgroup\"       : \"aValue2\" , " +
                        "\"creative\"      : \"ctValue2\" , " +
                        "\"click_label\"   : \"clValue2\" }";

            AdjustAttribution secondAttribution = AdjustAttribution.FromJsonString(secondAttributionJson);

            //check that it updates
            Assert.IsTrue(secondRestartActivityHandler.UpdateAttribution(secondAttribution));
            Assert.Debug("Wrote Attribution: tt:ttValue2 tn:tnValue2 net:nValue2 cam:cpValue2 adg:aValue2 cre:ctValue2 cl:clValue2");

            // check that it launch the saved attribute
            DeviceUtil.Sleep(1000);

            Assert.Test("onAttributionChanged: tt:ttValue2 tn:tnValue2 net:nValue2 cam:cpValue2 adg:aValue2 cre:ctValue2 cl:clValue2");

            // check that it does not update the attribution
            Assert.IsFalse(secondRestartActivityHandler.UpdateAttribution(secondAttribution));
            Assert.NotDebug("Wrote Attribution");
        }

        public void TestOfflineMode()
        {
            // adjust the session intervals for testing
            AdjustFactory.SetSessionInterval(new TimeSpan(0,0,0,0,2000));
            AdjustFactory.SetSubsessionInterval( new TimeSpan(0,0,0,0,500));

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            // put SDK offline
            activityHandler.SetOfflineMode(true);

            DeviceUtil.Sleep(3000);

            // check if message the disable of the SDK
            Assert.Info("Pausing package and attribution handler to put in offline mode");

            // test init values
            InitTests();

            // test first session start
            CheckFirstSession(paused: true);

            // test end session logs
            CheckEndSession();

            // disable the SDK
            activityHandler.SetEnabled(false);

            // check that it is disabled
            Assert.IsFalse(activityHandler.IsEnabled());

            // writing activity state after disabling
            Assert.Debug("Wrote Activity state: ec:0 sc:1 ssc:1");

            // check if message the disable of the SDK
            Assert.Info("Pausing package and attribution handler to disable the SDK");

            DeviceUtil.Sleep(1000);

            // test end session logs
            CheckEndSession(updateActivityState: false);

            // put SDK back online
            activityHandler.SetOfflineMode(false);

            Assert.Info("Package and attribution handler remain paused because the SDK is disabled");

            DeviceUtil.Sleep(1000);

            // test the update status, still paused
            Assert.NotTest("AttributionHandler PauseSending");
            Assert.NotTest("PackageHandler PauseSending");

            // try to do activities while SDK disabled
            activityHandler.TrackSubsessionStart();
            activityHandler.TrackEvent(new AdjustEvent("event1"));

            DeviceUtil.Sleep(3000);

            // check that timer was not executed
            CheckTimerIsFired(false);

            // check that it did not wrote activity state from new session or subsession
            Assert.NotDebug("Wrote Activity state");

            // check that it did not add any package
            Assert.NotTest("PackageHandler AddPackage");

            // enable the SDK again
            activityHandler.SetEnabled(true);

            // check that is enabled
            Assert.IsTrue(activityHandler.IsEnabled());

            DeviceUtil.Sleep(1000);

            // test that is not paused anymore
            CheckNewSession(paused: false, sessionCount: 2);
        }

        public void TestCheckAttributionState() {
            
            //AdjustFactory.setTimerStart(500);
            AdjustFactory.SetSessionInterval(new TimeSpan(0,0,0,0,4000));
            /***
             * // if it's a new session
             * if (ActivityState.SubSessionCount <= 1) { return; }
             * 
             * // if there is already an attribution saved and there was no attribution being asked
             * if (Attribution != null && !ActivityState.AskingAttribution) { return; }
             * 
             * AttributionHandler.AskAttribution();
             */

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            config.AttributionChanged = (adjustAttribution) => MockLogger.Test("AttributionChanged: {0}", adjustAttribution);

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(3000);

            // test init values
            InitTests();

            // subsession count is 1
            // attribution is null,
            // askingAttribution is false by default,
            // -> Not called

            // test first session start
            CheckFirstSession();

            // test that get attribution wasn't called
            Assert.NotTest("AttributionHandler AskAttribution");

            // subsession count increased to 2
            // attribution is still null,
            // askingAttribution is still false,
            // -> Called

            // trigger a new sub session
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount:1, subsessionCount: 2,
                timerAlreadyStarted: true, askAttributionIsCalled: true);

            // subsession count increased to 3
            // attribution is still null,
            // askingAttribution is set to true,
            // -> Called

            // set asking attribution
            activityHandler.SetAskingAttribution(true);
            Assert.Debug("Wrote Activity state: ec:0 sc:1 ssc:2");

            // trigger a new session
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount: 1, subsessionCount: 3,
                timerAlreadyStarted: true, askAttributionIsCalled: true);

            // subsession is reset to 1 with new session
            // attribution is still null,
            // askingAttribution is set to true,
            // -> Not called

            DeviceUtil.Sleep(3000); // 5 seconds = 2 + 3
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount: 2, subsessionCount: 1,
                timerAlreadyStarted: true, askAttributionIsCalled: false);
            
            // subsession count increased to 2
            // attribution is set,
            // askingAttribution is set to true,
            // -> Called
            var jsonAttribution = "{ " +
                        "\"tracker_token\" : \"ttValue\" , " +
                        "\"tracker_name\"  : \"tnValue\" , " +
                        "\"network\"       : \"nValue\" , " +
                        "\"campaign\"      : \"cpValue\" , " +
                        "\"adgroup\"       : \"aValue\" , " +
                        "\"creative\"      : \"ctValue\" , " +
                        "\"click_label\"   : \"clValue\" }";

            var attribution = AdjustAttribution.FromJsonString(jsonAttribution);

            // update the attribution
            activityHandler.UpdateAttribution(attribution);

            // attribution was updated
            Assert.Debug("Wrote Attribution: tt:ttValue tn:tnValue net:nValue cam:cpValue adg:aValue cre:ctValue cl:clValue");

            // trigger a new sub session
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(2, 2, true, true);
            // subsession count is reset to 1
            // attribution is set,
            // askingAttribution is set to true,
            // -> Not called

            DeviceUtil.Sleep(3000); // 5 seconds = 2 + 3
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount: 3, subsessionCount: 1,
                timerAlreadyStarted: true, askAttributionIsCalled: false);

            // subsession increased to 2
            // attribution is set,
            // askingAttribution is set to false
            // -> Not called

            activityHandler.SetAskingAttribution(false);
            Assert.Debug("Wrote Activity state: ec:0 sc:3 ssc:1");

            // trigger a new sub session
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount: 3, subsessionCount: 2,
                timerAlreadyStarted: true, askAttributionIsCalled: false);

            // subsession is reset to 1
            // attribution is set,
            // askingAttribution is set to false
            // -> Not called

            DeviceUtil.Sleep(3000); // 5 seconds = 2 + 3
            activityHandler.TrackSubsessionStart();
            DeviceUtil.Sleep(2000);

            CheckSubsession(sessionCount: 4, subsessionCount: 1,
                timerAlreadyStarted: true, askAttributionIsCalled: false);
        }

        public void TestTimer()
        {
            AdjustFactory.SetTimerInterval(new TimeSpan(0,0,0,0,4000));
            AdjustFactory.SetTimerStart(new TimeSpan(0,0,0,0,0));

            // create the config to start the session
            AdjustConfig config = new AdjustConfig(appToken: "123456789012", environment: AdjustConfig.EnvironmentSandbox);

            // start activity handler with config
            ActivityHandler activityHandler = GetActivityHandler(config);

            DeviceUtil.Sleep(2000);

            // test init values
            InitTests();

            // test first session start
            CheckFirstSession();

            // wait enough to fire the first cycle
            DeviceUtil.Sleep(3000);

            CheckTimerIsFired(true);

            // end subsession to stop timer
            activityHandler.TrackSubsessionEnd();

            // wait enough for a new cycle
            DeviceUtil.Sleep(6000);

            activityHandler.TrackSubsessionStart();

            DeviceUtil.Sleep(1000);

            CheckTimerIsFired(false);
        }


        private void InitTests(string environment = "sandbox", string logLevel = "Info", bool eventBuffering = false,
                               string readActivityState = null, string readAttribution = null)
        {
            // check environment level
            if (environment.Equals("sandbox"))
            {
                Assert.AssertMessage("SANDBOX: Adjust is running in Sandbox mode. Use this setting for testing. Don't forget to set the environment to `production` before publishing!");
            }
            else if (environment.Equals("production"))
            {
                Assert.AssertMessage("PRODUCTION: Adjust is running in Production mode. Use this setting only for the build that you want to publish. Set the environment to `sandbox` if you want to test your app!");
            }
            else
            {
                Assert.Fail();
            }

            // check log level
            Assert.Test("MockLogger setLogLevel: " + logLevel);

            // check event buffering
            if (eventBuffering)
            {
                Assert.Info("Event buffering is enabled");
            }
            else
            {
                Assert.NotInfo("Event buffering is enabled");
            }

            ReadFiles(readActivityState, readAttribution);
        }
        
        private void CheckSubsession(int sessionCount = 1, int subsessionCount = 1,
            bool? timerAlreadyStarted = null, bool? askAttributionIsCalled = null)
        {
            // test the new sub session
            Assert.Test("PackageHandler ResumeSending");

            // save activity state
            Assert.Debug("Wrote Activity state: ec:0 sc:{0} ssc:{1}", sessionCount, subsessionCount);

            if (subsessionCount > 1)
            {
                // test the subsession message
                Assert.Info("Started subsession {0} of session {1}", subsessionCount, sessionCount);
            }
            else
            {
                // test the subsession message
                Assert.NotInfo("Started subsession ");
            }

            if (askAttributionIsCalled.HasValue)
            {
                if (askAttributionIsCalled.Value)
                {
                    Assert.Test("AttributionHandler AskAttribution");
                }
                else
                {
                    Assert.NotTest("AttributionHandler AskAttribution");
                }
            }

            if (timerAlreadyStarted.HasValue)
            {
                CheckTimerIsFired(!timerAlreadyStarted.Value);
            }
        }

        private void ReadFiles(string readActivityState, string readAttribution)
        {
            if (readAttribution == null)
            {
                //  test that the attribution file did not exist in the first run of the application
                Assert.Verbose("Attribution file not found");
            }
            else
            {
                Assert.Debug("Read Attribution: " + readAttribution);
            }

            if (readActivityState == null)
            {
                //  test that the activity state file did not exist in the first run of the application
                Assert.Verbose("Activity state file not found");
            }
            else
            {
                Assert.Debug("Read Activity state: " + readActivityState);
            }
        }
        
        private void CheckFirstSession(bool paused = false)
        {
            if (paused)
            {
                Assert.Test("PackageHandler Init, startPaused: True");
            }
            else
            {
                Assert.Test("PackageHandler Init, startPaused: False");
            }

            CheckNewSession(paused: paused, 
                sessionCount: 1,
                eventCount: 0,
                timerAlreadyStarted: false);
        }

        private void CheckNewSession(bool paused,
                                     int sessionCount,
                                     int eventCount = 0,
                                     bool timerAlreadyStarted = false)
        {
            // when a session package is being sent the attribution handler should resume sending
            if (paused)
            {
                Assert.Test("AttributionHandler PauseSending");
            }
            else
            {
                Assert.Test("AttributionHandler ResumeSending");
            }

            // when a session package is being sent the package handler should resume sending
            if (paused)
            {
                Assert.Test("PackageHandler PauseSending");
            }
            else
            {
                Assert.Test("PackageHandler ResumeSending");
            }

            // if the package was build, it was sent to the Package Handler
            Assert.Test("PackageHandler AddPackage");

            // after adding, the activity handler ping the Package handler to send the package
            Assert.Test("PackageHandler SendFirstPackage");

            // after sending a package saves the activity state
            Assert.Debug("Wrote Activity state: ec:{0} sc:{1} ssc:1", eventCount, sessionCount);

            CheckTimerIsFired(!(paused || timerAlreadyStarted));
        }

        private void CheckTimerIsFired(bool timerFired)
        {
            // timer fired
            if (timerFired)
            {
                Assert.Debug("Session timer fired");
            }
            else
            {
                Assert.NotDebug("Session timer fired");
            }
        }

        private void CheckEndSession(bool updateActivityState = true)
        {
            Assert.Test("PackageHandler PauseSending");

            Assert.Test("AttributionHandler PauseSending");

            if (updateActivityState)
            {
                Assert.Debug("Wrote Activity state");
            }
        }

        private ActivityHandler GetActivityHandler(AdjustConfig config)
        {
            ActivityHandler activityHandler = ActivityHandler.GetInstance(config, DeviceUtil);

            return activityHandler;
        }

        /*
        public void TestFirstSession(string clientSdk)
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", DeviceUtil);
            activityHandler.SetSdkPrefix("SdkWrapperX.Y.Z");

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            DeviceUtil.Sleep(1000);

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOActivityState (not found)"),
                MockLogger.ToString());

            // whenever a session starts the package handler should be called to resume sending
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler ResumeSending"),
                MockLogger.ToString());

            // if the package was built, it is sent to the package handler
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler AddPackage"),
                MockLogger.ToString());

            // after added, the package handler is signaled to send the next package on the queue
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // verify that the activity state is written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state"),
                MockLogger.ToString());

            // ending the first session
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "First session"),
                MockLogger.ToString());

            DeviceUtil.Sleep(1000);
            // by this time also the timer should have fired
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // checking the default values of the first session package
            // should only have one package
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            var firstSessionPackage = MockPackageHandler.PackageQueue[0];

            // check the Sdk verion being tested
            Assert.AreEqual("SdkWrapperX.Y.Z@" + clientSdk, firstSessionPackage.ClientSdk,
                firstSessionPackage.GetExtendedString());

            // check the server url
            Assert.AreEqual("https://app.adjust.io", Util.BaseUrl);

            // the activity kind shoud be session
            Assert.AreEqual("session", ActivityKindUtil.ToString(firstSessionPackage.ActivityKind),
                firstSessionPackage.GetExtendedString());

            // the path of the call should be /session
            Assert.AreEqual(@"/startup", firstSessionPackage.Path,
                firstSessionPackage.GetExtendedString());

            var parameters = firstSessionPackage.Parameters;

            // session atributes
            // session count should be 1 because it's the first session
            Assert.IsTrue(IsParameterEqual(1, parameters, "session_count"),
                firstSessionPackage.GetExtendedString());

            // subsession count should be -1, because we didn't any subsessions yet
            // because values < 0 are added discarded, it shouldn't be present on the parameters
            Assert.IsFalse(parameters.ContainsKey("subsession_count"),
                firstSessionPackage.GetExtendedString());

            // session lenght should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("session_lenght"),
                firstSessionPackage.GetExtendedString());

            // time spent should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("time_spent"),
                firstSessionPackage.GetExtendedString());

            // created at TODO

            // last interval shoule be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("last_interval"),
                firstSessionPackage.GetExtendedString());
        }

        public void TestUserAgent()
        {
            var delimiters = "\"(),/:;<=>?@[]{} \t\f\r\n\v\"(),/:;<=>?@[]{} \t\f\r\n\v";
            string nullString = null;
            var emptyString = "\"()/<>?@[]{} \t\f\r\n\v\"()/<>?@[]{} \t\f\r\n\v";

            var sanatizedDelimeters = Util.SanitizeUserAgent(delimiters);
            var sanatizedNullString = Util.SanitizeUserAgent(nullString);
            var sanatizedEmptyString = Util.SanitizeUserAgent(emptyString, "emptyString");

            Assert.AreEqual("..._..._", sanatizedDelimeters);
            Assert.AreEqual("unknown", sanatizedNullString);
            Assert.AreEqual("emptyString", sanatizedEmptyString);
        }

        public void TestSessions()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // change the default session and subsession intervals for testing
            AdjustFactory.SetSessionInterval(new TimeSpan(0, 0, 2)); // 2 second session
            AdjustFactory.SetSubsessionInterval(new TimeSpan(0, 0, 0, 0, 100)); // 0.1 second subsession

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", DeviceUtil);

            // wait enough time to be a new subsession but not a new session
            DeviceUtil.Sleep(1500);
            activityHandler.TrackSubsessionStart();

            // wait enough to be a new session
            DeviceUtil.Sleep(4000);
            activityHandler.TrackSubsessionStart();

            // trigger a sub session end
            activityHandler.TrackSubsessionEnd();

            DeviceUtil.Sleep(1000);

            // check that a new subsession was created
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "Processed Subsession 2 of Session 1"),
                MockLogger.ToString());

            // check that it's now on the 2nd session
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Session 2"),
                MockLogger.ToString());

            // check that 2 packages were added to the package handler
            Assert.AreEqual(2, MockPackageHandler.PackageQueue.Count);

            // get the second session package and its parameters
            var activityPackage = MockPackageHandler.PackageQueue[1];
            var parameters = activityPackage.Parameters;

            // the session and subsession count should be 2
            Assert.IsTrue(IsParameterEqual(2, parameters, "session_count"),
                activityPackage.GetExtendedString());
            Assert.IsTrue(IsParameterEqual(2, parameters, "subsession_count"),
                activityPackage.GetExtendedString());

            // TODO test updated timeSpent and sessionLenght

            // check that the package handler was paused
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler PauseSending"),
                MockLogger.ToString());
        }

        public void TestEventsBuffered()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", DeviceUtil);

            // set the activity handler to buffer events
            activityHandler.SetBufferedEvents(enabledEventBuffering: true);

            // construct the parameters of the event
            var eventCallbackParameters = new Dictionary<string, string>
            {
                {"key", "value"},
                {"foo", "bar"},
            };

            // triggers the event with parameters
            activityHandler.TrackEvent("eve123", eventCallbackParameters);

            // construct the parameters of the event
            var revenueCallbackParameters = new Dictionary<string, string>();

            // triggers the revenue with parameters
            activityHandler.TrackRevenue(4.45, "rev123", revenueCallbackParameters);

            DeviceUtil.Sleep(1000);

            // check that the event was buffered
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "Buffered event 'eve123'"),
                MockLogger.ToString());

            // check event count in the written activity state
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state: ec:1"),
                MockLogger.ToString());

            // check event count in the logger
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 1"),
                MockLogger.ToString());

            // check that the revenue was buffered
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "Buffered revenue (4.5 cent, 'rev123')"),
                MockLogger.ToString());

            // check event count in the written activity state after the revenue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state: ec:2"),
                MockLogger.ToString());

            // check event count in the logger
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 2 (revenue)"),
                MockLogger.ToString());

            // check that the package handler added the session, event and revenue package
            Assert.AreEqual(3, MockPackageHandler.PackageQueue.Count);

            // check the first event
            var eventPackage = MockPackageHandler.PackageQueue[1];

            // check the event path
            Assert.AreEqual(@"/event", eventPackage.Path,
                eventPackage.GetExtendedString());

            // check the event suffx
            Assert.AreEqual(" 'eve123'", eventPackage.Suffix,
                eventPackage.GetExtendedString());

            var eventParamaters = eventPackage.Parameters;

            // check the event count in the event parameters
            Assert.IsTrue(IsParameterEqual(1, eventParamaters, "event_count"),
                eventPackage.GetExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("eve123", eventParamaters, "event_token"),
                eventPackage.GetExtendedString());

            // check the injected parameters
            Assert.IsTrue(IsParameterEqual("eyJrZXkiOiJ2YWx1ZSIsImZvbyI6ImJhciJ9", eventParamaters, "params"),
                eventPackage.GetExtendedString());

            // check the revenue
            var revenuePackage = MockPackageHandler.PackageQueue[2];

            // check the revenue path
            Assert.AreEqual(@"/revenue", revenuePackage.Path,
                revenuePackage.GetExtendedString());

            // check the revenue suffix
            // note that the amount was rounded to the decimal cents
            Assert.AreEqual(" (4.5 cent, 'rev123')", revenuePackage.Suffix,
                revenuePackage.GetExtendedString());

            var revenueParameters = revenuePackage.Parameters;

            // check the event count in the revenue parameters
            Assert.IsTrue(IsParameterEqual(2, revenueParameters, "event_count"),
                revenuePackage.GetExtendedString());

            // check the amount, transforming cents into rounded decimal cents
            // note that 4.45 cents ~> 45 decimal cents
            Assert.IsTrue(IsParameterEqual(45, revenueParameters, "amount"),
                revenuePackage.GetExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("rev123", revenueParameters, "event_token"),
                revenuePackage.GetExtendedString());

            // check the injected empty parameters
            Assert.IsTrue(IsParameterEqual("e30=", revenueParameters, "params"),
                revenuePackage.GetExtendedString());
        }

        public void TestEventsNotBuffered()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", DeviceUtil);

            // set the activity handler to not buffer events, the default behaviour
            activityHandler.SetBufferedEvents(enabledEventBuffering: false);

            // triggers the event without parameters
            activityHandler.TrackEvent("eve123", null);

            // triggers the revenue without parameters or eventToken
            activityHandler.TrackRevenue(0, null, null);

            DeviceUtil.Sleep(1000);

            // check that the package handler was called for the first package session
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // check that the package handler was called for the second package event
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // check that the package handler was called for the third package revenue
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // check event count in the written activity state
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state: ec:1"),
                MockLogger.ToString());

            // check event count in the logger
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 1"),
                MockLogger.ToString());

            // check that the package handler added the session, event and revenue package
            Assert.AreEqual(3, MockPackageHandler.PackageQueue.Count);

            // check the first event
            var eventPackage = MockPackageHandler.PackageQueue[1];

            // check the event path
            Assert.AreEqual(@"/event", eventPackage.Path,
                eventPackage.GetExtendedString());

            // check the event suffx
            Assert.AreEqual(" 'eve123'", eventPackage.Suffix,
                eventPackage.GetExtendedString());

            var eventParameters = eventPackage.Parameters;

            // check the event count in the event parameters
            Assert.IsTrue(IsParameterEqual(1, eventParameters, "event_count"),
                eventPackage.GetExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("eve123", eventParameters, "event_token"),
                eventPackage.GetExtendedString());

            // check that the parameters were not injected
            Assert.IsFalse(eventParameters.ContainsKey("params"),
                eventPackage.GetExtendedString());

            // check the revenue
            var revenuePackage = MockPackageHandler.PackageQueue[2];

            // check the revenue path
            Assert.AreEqual(@"/revenue", revenuePackage.Path,
                revenuePackage.GetExtendedString());

            // check the revenue suffix
            // note that the amount was rounded to the decimal cents
            Assert.AreEqual(" (0.0 cent)", revenuePackage.Suffix,
                revenuePackage.GetExtendedString());

            var revenueParameters = revenuePackage.Parameters;

            // check the event count in the revenue parameters
            Assert.IsTrue(IsParameterEqual(2, revenueParameters, "event_count"),
                revenuePackage.GetExtendedString());

            // check the amount, transforming cents into rounded decimal cents
            Assert.IsTrue(IsParameterEqual(0, revenueParameters, "amount"),
                revenuePackage.GetExtendedString());

            // check that the revenue parameters does not contain the eventToken
            Assert.IsFalse(revenueParameters.ContainsKey("event_token"),
                revenuePackage.GetExtendedString());

            // check that the revenue parameters were not injected
            Assert.IsFalse(revenueParameters.ContainsKey("params"),
                revenuePackage.GetExtendedString());

            // check event count in the written activity state after the revenue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state: ec:2"),
                MockLogger.ToString());

            // check event count in the logger
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 2 (revenue)"),
                MockLogger.ToString());
        }

        public void TestChecks()
        {
            // activity handler with null app token
            var activityHandler = new ActivityHandler(null, DeviceUtil);

            activityHandler.TrackSubsessionStart();
            activityHandler.TrackSubsessionEnd();
            activityHandler.TrackEvent("eve123", null);
            activityHandler.TrackRevenue(0, null, null);

            DeviceUtil.Sleep(1000);

            // check missing app token for each of the calls:
            //      Constructor
            //      TrackSubsessionStart
            //      TrackSubsessionEnd
            //      TrackEvent
            //      TrackRevenue
            for (int i = 0; i < 5; i++)
                Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Missing App Token"),
                    MockLogger.ToString());

            new ActivityHandler("12345678901", DeviceUtil);

            DeviceUtil.Sleep(1000);

            // check the malformed token
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Malformed App Token '12345678901'"),
                MockLogger.ToString());

            // test invalid event and revenue from a valid activity handler
            activityHandler = new ActivityHandler("123456789012", DeviceUtil);

            activityHandler.TrackEvent(null, null);
            activityHandler.TrackRevenue(-0.1, null, null);
            activityHandler.TrackEvent("12345", null);

            DeviceUtil.Sleep(1000);

            // check null event token
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Missing Event Token"),
                MockLogger.ToString());

            // check invalid revenue amount
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Invalid amount -0.1"),
                MockLogger.ToString());

            // check invalid event token
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Malformed Event Token '12345'"),
                MockLogger.ToString());
        }

        public void TestDisable()
        {
            // starting from a clean slate
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // set the timer for a shorter time for testing
            AdjustFactory.SetTimerInterval(new TimeSpan(0, 0, 0, 0, 700));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("qwerty123456", DeviceUtil);

            // verify the default value, when not started
            Assert.IsTrue(activityHandler.IsEnabled(),
                MockLogger.ToString());

            activityHandler.SetEnabled(false);

            // verify the default value, when not started
            Assert.IsFalse(activityHandler.IsEnabled(),
                MockLogger.ToString());

            // start the first session
            activityHandler.TrackEvent("123456", null);
            activityHandler.TrackRevenue(0.1, null, null);
            activityHandler.TrackSubsessionEnd();
            activityHandler.TrackSubsessionStart();

            DeviceUtil.Sleep(1000);

            // verify the changed value after the activity handler is started
            Assert.IsFalse(activityHandler.IsEnabled(),
                MockLogger.ToString());

            // making sure the first session was sent
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Info, "First session"),
                MockLogger.ToString());

            // delete the first session package from the log
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // making sure the timer fired did not call the package handler
            Assert.IsFalse(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // test if the event was not triggered
            Assert.IsFalse(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 1"),
                MockLogger.ToString());

            // test if the revenue was not triggered
            Assert.IsFalse(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 1 (revenue)"),
                MockLogger.ToString());

            // verify that the application was paused
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler PauseSending"),
                MockLogger.ToString());

            // verify that it was not resumed
            Assert.IsFalse(MockLogger.DeleteTestUntil("PackageHandler ResumeSending"),
                MockLogger.ToString());

            // enable again
            activityHandler.SetEnabled(true);
            DeviceUtil.Sleep(1000);

            // verify that the timer was able to resume sending
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler ResumeSending"),
                MockLogger.ToString());

            activityHandler.TrackEvent("123456", null);
            activityHandler.TrackRevenue(0.1, null, null);
            activityHandler.TrackSubsessionEnd();
            activityHandler.TrackSubsessionStart();

            DeviceUtil.Sleep(1000);

            // verify the changed value, when the activity state is started
            Assert.IsTrue(activityHandler.IsEnabled(),
                MockLogger.ToString());

            // test that the event was triggered
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 1"),
                MockLogger.ToString());

            // test that the revenue was triggered
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 2 (revenue)"),
                MockLogger.ToString());

            // verify that the application was paused
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler PauseSending"),
                MockLogger.ToString());

            // verify that it was also resumed
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler ResumeSending"),
                MockLogger.ToString());
        }

        public void TestOpenUrl()
        {
            // starting from a clean slate
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("qwerty123456", DeviceUtil);

            var normal = new Uri("AdjustTests://example.com/path/inApp?adjust_foo=bar&other=stuff&adjust_key=value#fragment");
            var emptyQueryString = new Uri("AdjustTests://");
            Uri nullUri = null;
            var single = new Uri("AdjustTests://example.com/path/inApp?adjust_foo");
            var prefix = new Uri("AdjustTests://example.com/path/inApp?adjust_=bar");
            var incomplete = new Uri("AdjustTests://example.com/path/inApp?adjust_foo=");

            activityHandler.ReadOpenUrl(normal);
            activityHandler.ReadOpenUrl(emptyQueryString);
            activityHandler.ReadOpenUrl(nullUri);
            activityHandler.ReadOpenUrl(single);
            activityHandler.ReadOpenUrl(prefix);
            activityHandler.ReadOpenUrl(incomplete);

            DeviceUtil.Sleep(2000);

            // check that all supposed packages were sent
            // 1 session + 1 reattributions
            Assert.AreEqual(2, MockPackageHandler.PackageQueue.Count);

            // check that the normal url was parsed and sent
            var reattributionPackage = MockPackageHandler.PackageQueue[1];

            // testing the activity kind is the correct one
            var activityKind = reattributionPackage.ActivityKind;
            Assert.AreEqual(ActivityKind.Reattribution, activityKind,
                reattributionPackage.GetExtendedString());

            // testing the conversion from activity kind to string
            var activityKindString = ActivityKindUtil.ToString(activityKind);
            Assert.AreEqual("reattribution", activityKindString,
                reattributionPackage.GetExtendedString());

            // testing the conversion from string to activity kind
            activityKind = ActivityKindUtil.FromString(activityKindString);
            Assert.AreEqual(ActivityKind.Reattribution, activityKind,
                reattributionPackage.GetExtendedString());

            // package type should be reattribute
            Assert.AreEqual(@"/reattribute", reattributionPackage.Path,
                reattributionPackage.GetExtendedString());

            // suffix should be empty
            Assert.AreEqual("", reattributionPackage.Suffix,
                reattributionPackage.GetExtendedString());

            var parameters = reattributionPackage.Parameters;

            string attributesString;
            parameters.TryGetValue("deeplink_parameters", out attributesString);

            // check that deep link parameters contains the base64 with the 2 keys
            Assert.AreEqual("{\"foo\":\"bar\",\"key\":\"value\"}", attributesString);

            // check that added and set both session and reattribution package
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler AddPackage"),
                MockLogger.ToString());
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler AddPackage"),
                MockLogger.ToString());
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // check that sent the reattribution package
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Reattribution {\"foo\":\"bar\",\"key\":\"value\"}"),
                MockLogger.ToString());
        }

        private bool IsParameterEqual(int expected, Dictionary<string, string> parameter, string key)
        {
            string value;
            if (!parameter.TryGetValue(key, out value))
                return false;

            int actual;
            if (!Int32.TryParse(value, out actual))
                return false;

            return expected == actual;
        }

        private bool IsParameterEqual(string expected, Dictionary<string, string> parameter, string key)
        {
            string value;
            if (!parameter.TryGetValue(key, out value))
                return false;

            return expected == value;
        }
         */
    }
}