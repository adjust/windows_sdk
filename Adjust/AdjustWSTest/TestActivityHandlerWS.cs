using AdjustSdk.Pcl;
using AdjustSdk.Pcl.Test;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.WS.Test
{
    [TestClass]
    public class TestActivityHandlerWS
    {
        private MockLogger MockLogger;
        private MockPackageHandler MockPackageHandler;
        private UtilWS UtilWS;

        [TestInitialize]
        public void SetUp()
        {
            UtilWS = new UtilWS();

            MockLogger = new MockLogger();
            AdjustFactory.Logger = MockLogger;

            MockPackageHandler = new MockPackageHandler(MockLogger);
            AdjustFactory.SetPackageHandler(MockPackageHandler);
        }

        [TestCleanup]
        public void TearDown()
        {
            AdjustFactory.SetPackageHandler(null);
            AdjustFactory.SetSessionInterval(null);
            AdjustFactory.SetSubsessionInterval(null);
            AdjustFactory.Logger = null;
        }

        [TestMethod]
        public void TestFirstSessionWS()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", UtilWS);

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            UtilWS.Sleep(1000).Wait();

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

            UtilWS.Sleep(1000).Wait();
            // by this time also the timer should have fired
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler SendFirstPackage"),
                MockLogger.ToString());

            // checking the default values of the first session package
            // should only have one package
            Assert.AreEqual(1, MockPackageHandler.PackageQueue.Count);

            var firstSessionPackage = MockPackageHandler.PackageQueue[0];

            // check the Sdk verion being tested
            Assert.AreEqual("wstore3.0.0", firstSessionPackage.ClientSdk,
                firstSessionPackage.ExtendedString());

            // the activity kind shoud be session
            Assert.AreEqual("session", ActivityKindUtil.ToString(firstSessionPackage.ActivityKind),
                firstSessionPackage.ExtendedString());

            // the path of the call should be /session
            Assert.AreEqual(@"/startup", firstSessionPackage.Path,
                firstSessionPackage.ExtendedString());

            var parameters = firstSessionPackage.Parameters;

            // session atributes
            // session count should be 1 because it's the first session
            Assert.IsTrue(IsParameterEqual(1, parameters, "session_count"),
                firstSessionPackage.ExtendedString());

            // subsession count should be -1, because we didn't any subsessions yet
            // because values < 0 are added discarded, it shouldn't be present on the parameters
            Assert.IsFalse(parameters.ContainsKey("subsession_count"),
                firstSessionPackage.ExtendedString());

            // session lenght should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("session_lenght"),
                firstSessionPackage.ExtendedString());

            // time spent should be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("time_spent"),
                firstSessionPackage.ExtendedString());

            // created at TODO

            // last interval shoule be -1, same as before
            Assert.IsFalse(parameters.ContainsKey("last_interval"),
                firstSessionPackage.ExtendedString());
        }

        [TestMethod]
        public void TestSessionsWS()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // change the default session and subsession intervals for testing
            AdjustFactory.SetSessionInterval(new TimeSpan(0, 0, 2)); // 2 second session
            AdjustFactory.SetSubsessionInterval(new TimeSpan(0, 0, 0, 0, 100)); // 0.1 second subsession

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", UtilWS);

            // wait enough time to be a new subsession but not a new session
            UtilWS.Sleep(1500).Wait();
            activityHandler.TrackSubsessionStart();

            // wait enough to be a new session
            UtilWS.Sleep(4000).Wait();
            activityHandler.TrackSubsessionStart();

            // trigger a sub session end
            activityHandler.TrackSubsessionEnd();

            UtilWS.Sleep(1000).Wait();

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
                activityPackage.ExtendedString());
            Assert.IsTrue(IsParameterEqual(2, parameters, "subsession_count"),
                activityPackage.ExtendedString());

            // TODO test updated timeSpent and sessionLenght

            // check that the package handler was paused
            Assert.IsTrue(MockLogger.DeleteTestUntil("PackageHandler PauseSending"),
                MockLogger.ToString());
        }

        [TestMethod]
        public void TestEventsBufferedWS()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", UtilWS);

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

            UtilWS.Sleep(1000).Wait();

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
                eventPackage.ExtendedString());

            // check the event suffx
            Assert.AreEqual(" 'eve123'", eventPackage.Suffix,
                eventPackage.ExtendedString());

            var eventParamaters = eventPackage.Parameters;

            // check the event count in the event parameters
            Assert.IsTrue(IsParameterEqual(1, eventParamaters, "event_count"),
                eventPackage.ExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("eve123", eventParamaters, "event_token"),
                eventPackage.ExtendedString());

            // check the injected parameters
            Assert.IsTrue(IsParameterEqual("eyJrZXkiOiJ2YWx1ZSIsImZvbyI6ImJhciJ9", eventParamaters, "params"),
                eventPackage.ExtendedString());

            // check the revenue
            var revenuePackage = MockPackageHandler.PackageQueue[2];

            // check the revenue path
            Assert.AreEqual(@"/revenue", revenuePackage.Path,
                revenuePackage.ExtendedString());

            // check the revenue suffix
            // note that the amount was rounded to the decimal cents
            Assert.AreEqual(" (4.5 cent, 'rev123')", revenuePackage.Suffix,
                revenuePackage.ExtendedString());

            var revenueParameters = revenuePackage.Parameters;

            // check the event count in the revenue parameters
            Assert.IsTrue(IsParameterEqual(2, revenueParameters, "event_count"),
                revenuePackage.ExtendedString());

            // check the amount, transforming cents into rounded decimal cents
            // note that 4.45 cents ~> 45 decimal cents
            Assert.IsTrue(IsParameterEqual(45, revenueParameters, "amount"),
                revenuePackage.ExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("rev123", revenueParameters, "event_token"),
                revenuePackage.ExtendedString());

            // check the injected empty parameters
            Assert.IsTrue(IsParameterEqual("e30=", revenueParameters, "params"),
                revenuePackage.ExtendedString());
        }

        [TestMethod]
        public void TestEventsNotBuffered()
        {
            // deleting the activity state file to simulate a first session
            MockLogger.Test("Was the activity state file deleted? {0}", Util.DeleteFile("AdjustIOActivityState"));

            // create Activity handler and start first session
            var activityHandler = new ActivityHandler("123456789012", UtilWS);

            // set the activity handler to not buffer events, the default behaviour
            activityHandler.SetBufferedEvents(enabledEventBuffering: false);

            // triggers the event without parameters
            activityHandler.TrackEvent("eve123", null);

            // triggers the revenue without parameters or eventToken
            activityHandler.TrackRevenue(0, null, null);

            UtilWS.Sleep(1000).Wait();

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
                eventPackage.ExtendedString());

            // check the event suffx
            Assert.AreEqual(" 'eve123'", eventPackage.Suffix,
                eventPackage.ExtendedString());

            var eventParameters = eventPackage.Parameters;

            // check the event count in the event parameters
            Assert.IsTrue(IsParameterEqual(1, eventParameters, "event_count"),
                eventPackage.ExtendedString());

            // check the event token
            Assert.IsTrue(IsParameterEqual("eve123", eventParameters, "event_token"),
                eventPackage.ExtendedString());

            // check that the parameters were not injected
            Assert.IsFalse(eventParameters.ContainsKey("params"),
                eventPackage.ExtendedString());

            // check the revenue
            var revenuePackage = MockPackageHandler.PackageQueue[2];

            // check the revenue path
            Assert.AreEqual(@"/revenue", revenuePackage.Path,
                revenuePackage.ExtendedString());

            // check the revenue suffix
            // note that the amount was rounded to the decimal cents
            Assert.AreEqual(" (0.0 cent)", revenuePackage.Suffix,
                revenuePackage.ExtendedString());

            var revenueParameters = revenuePackage.Parameters;

            // check the event count in the revenue parameters
            Assert.IsTrue(IsParameterEqual(2, revenueParameters, "event_count"),
                revenuePackage.ExtendedString());

            // check the amount, transforming cents into rounded decimal cents
            Assert.IsTrue(IsParameterEqual(0, revenueParameters, "amount"),
                revenuePackage.ExtendedString());

            // check that the revenue parameters does not contain the eventToken
            Assert.IsFalse(revenueParameters.ContainsKey("event_token"),
                revenuePackage.ExtendedString());

            // check that the revenue parameters were not injected
            Assert.IsFalse(revenueParameters.ContainsKey("params"),
                revenuePackage.ExtendedString());

            // check event count in the written activity state after the revenue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Wrote activity state: ec:2"),
                MockLogger.ToString());

            // check event count in the logger
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Event 2 (revenue)"),
                MockLogger.ToString());
        }

        [TestMethod]
        public void TestChecks()
        {
            // activity handler with null app token
            var activityHandler = new ActivityHandler(null, UtilWS);

            activityHandler.TrackSubsessionStart();
            activityHandler.TrackSubsessionEnd();
            activityHandler.TrackEvent("eve123", null);
            activityHandler.TrackRevenue(0, null, null);

            UtilWS.Sleep(1000).Wait();

            // check missing app token for each of the calls:
            //      Constructor
            //      TrackSubsessionStart
            //      TrackSubsessionEnd
            //      TrackEvent
            //      TrackRevenue
            for (int i = 0; i < 5; i++)
                Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Missing App Token"),
                    MockLogger.ToString());

            new ActivityHandler("12345678901", UtilWS);

            UtilWS.Sleep(1000).Wait();

            // check the malformed token
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Malformed App Token '12345678901'"),
                MockLogger.ToString());

            // test invalid event and revenue from a valid activity handler
            activityHandler = new ActivityHandler("123456789012", UtilWS);

            activityHandler.TrackEvent(null, null);
            activityHandler.TrackRevenue(-0.1, null, null);
            activityHandler.TrackEvent("12345", null);

            UtilWS.Sleep(1000).Wait();

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
    }
}