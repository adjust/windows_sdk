using AdjustSdk.Pcl;
using System;

namespace AdjustSdk
{
    public class AdjustConfig
    {
        public const string EnvironmentSandbox = "sandbox";
        public const string EnvironmentProduction = "production";

        internal string AppToken { get; private set; }

        internal string Environment { get; private set; }

        public string SdkPrefix { get; set; }

        public bool EventBufferingEnabled { get; set; }

        public string DefaultTracker { get; set; }

        public Action<AdjustAttribution> AttributionChanged { get; set; }

        public bool HasDelegate { get { return AttributionChanged != null; } }

        private ILogger Logger { get; set; }

        public AdjustConfig(string appToken, string environment)
        {
            Logger = AdjustFactory.Logger;

            if (!IsValid(appToken, environment)) { return; }

            AppToken = appToken;
            Environment = environment;

            // default values
            EventBufferingEnabled = false;
        }

        public bool IsValid()
        {
            return AppToken != null;
        }

        private bool IsValid(string appToken, string environment)
        {
            if (!CheckAppToken(appToken)) { return false; }
            if (!CheckEnvironment(environment)) { return false; }

            return true;
        }

        private bool CheckAppToken(string appToken)
        {
            if (string.IsNullOrEmpty(appToken))
            {
                Logger.Error("Missing App Token");
                return false;
            }

            if (appToken.Length != 12)
            {
                Logger.Error("Malformed App Token '{0}'", appToken);
                return false;
            }

            return true;
        }

        private bool CheckEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                Logger.Error("Missing environment");
                return false;
            }

            if (environment.Equals(EnvironmentSandbox))
            {
                Logger.Assert("SANDBOX: Adjust is running in Sandbox mode. " +
                   "Use this setting for testing. " +
                   "Don't forget to set the environment to `production` before publishing!");

                return true;
            }
            else if (environment.Equals(EnvironmentProduction))
            {
                Logger.Assert("PRODUCTION: Adjust is running in Production mode. " +
                           "Use this setting only for the build that you want to publish. " +
                           "Set the environment to `sandbox` if you want to test your app!");
                return true;
            }

            Logger.Error("Unknown environment '{0}'", environment);
            return false;
        }
    }
}
/*
- (void)testAppWillOpenUrl
{
    //  reseting to make the test order independent
    [self reset];

    // create the config to start the session
    ADJConfig * config = [ADJConfig configWithAppToken:@"123456789012" environment:ADJEnvironmentSandbox];

    // set log level
    config.logLevel = ADJLogLevelError;

    // start activity handler with config
    id<ADJActivityHandler> activityHandler = [ADJActivityHandler handlerWithConfig:config];

    // it's necessary to sleep the activity for a while after each handler call
    //  to let the internal queue act
    [NSThread sleepForTimeInterval:2.0];

    // test init values
    [self checkInit:ADJEnvironmentSandbox logLevel:@"5"];

    // test first session start
    [self checkFirstSession];

    NSURL* attributions = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_tracker=trackerValue&other=stuff&adjust_campaign=campaignValue&adjust_adgroup=adgroupValue&adjust_creative=creativeValue"];
    NSURL* extraParams = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_foo=bar&other=stuff&adjust_key=value"];
    NSURL* mixed = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_foo=bar&other=stuff&adjust_campaign=campaignValue&adjust_adgroup=adgroupValue&adjust_creative=creativeValue"];
    NSURL* emptyQueryString = [NSURL URLWithString:@"AdjustTests://"];
    NSURL* emptyString = [NSURL URLWithString:@""];
    NSURL* nilString = [NSURL URLWithString:nil];
    NSURL* nilUrl = nil;
    NSURL* single = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_foo"];
    NSURL* prefix = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_=bar"];
    NSURL* incomplete = [NSURL URLWithString:@"AdjustTests://example.com/path/inApp?adjust_foo="];

    [activityHandler appWillOpenUrl:attributions];
    [activityHandler appWillOpenUrl:extraParams];
    [activityHandler appWillOpenUrl:mixed];
    [activityHandler appWillOpenUrl:emptyQueryString];
    [activityHandler appWillOpenUrl:emptyString];
    [activityHandler appWillOpenUrl:nilString];
    [activityHandler appWillOpenUrl:nilUrl];
    [activityHandler appWillOpenUrl:single];
    [activityHandler appWillOpenUrl:prefix];
    [activityHandler appWillOpenUrl:incomplete];

    [NSThread sleepForTimeInterval:2];

    // three click packages: attributions, extraParams and mixed
    for (int i = 3; i > 0; i--) {
        aTest(@"PackageHandler addPackage");
    }

    // checking the default values of the first session package
    // 1 session + 3 click
    aiEquals(4, (int)[self.packageHandlerMock.packageQueue count]);

    // get the click package
    ADJActivityPackage * attributionClickPackage = (ADJActivityPackage *) self.packageHandlerMock.packageQueue[1];

    // create activity package test
    ADJPackageFields * attributionClickFields = [ADJPackageFields fields];

    // create the attribution
    ADJAttribution * firstAttribution = [[ADJAttribution alloc] init];
    firstAttribution.trackerName = @"trackerValue";
    firstAttribution.campaign = @"campaignValue";
    firstAttribution.adgroup = @"adgroupValue";
    firstAttribution.creative = @"creativeValue";

    // and set it
    attributionClickFields.attribution = firstAttribution;

    // test the first deeplink
    [self testClickPackage:attributionClickPackage fields:attributionClickFields source:@"deeplink"];

    // get the click package
    ADJActivityPackage * extraParamsClickPackage = (ADJActivityPackage *) self.packageHandlerMock.packageQueue[2];

    // create activity package test
    ADJPackageFields * extraParamsClickFields = [ADJPackageFields fields];

    // other deep link parameters
    extraParamsClickFields.deepLinkParameters = @"{\"key\":\"value\",\"foo\":\"bar\"}";

    // test the second deeplink
    [self testClickPackage:extraParamsClickPackage fields:extraParamsClickFields source:@"deeplink"];

    // get the click package
    ADJActivityPackage * mixedClickPackage = (ADJActivityPackage *) self.packageHandlerMock.packageQueue[3];

    // create activity package test
    ADJPackageFields * mixedClickFields = [ADJPackageFields fields];

    // create the attribution
    ADJAttribution * secondAttribution = [[ADJAttribution alloc] init];
    secondAttribution.campaign = @"campaignValue";
    secondAttribution.adgroup = @"adgroupValue";
    secondAttribution.creative = @"creativeValue";

    // and set it
    mixedClickFields.attribution = secondAttribution;

    // other deep link parameters
    mixedClickFields.deepLinkParameters = @"{\"foo\":\"bar\"}";

    // test the third deeplink
    [self testClickPackage:mixedClickPackage fields:mixedClickFields source:@"deeplink"];
}
*/