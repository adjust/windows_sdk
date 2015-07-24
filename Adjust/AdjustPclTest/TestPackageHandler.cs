using AdjustSdk;
using AdjustSdk.Pcl;

namespace AdjustTest.Pcl
{
    public class TestPackageHandler : TestTemplate
    {
        private MockRequestHandler MockRequestHandler { get; set; }
        private MockActivityHandler MockActivityHandler { get; set; }
        private PackageHandler PackageHandler { get; set; }
        private ActivityHandler ActivityHandler { get; set; }

        private enum SendFirstState
        {
            EMPTY_QUEUE, PAUSED, IS_SENDING, SEND
        }

        public TestPackageHandler(DeviceUtil deviceUtil, IAssert assert)
            : base(deviceUtil, assert)
        { }

        public override void SetUp()
        {
            base.SetUp();

            MockRequestHandler = new MockRequestHandler(MockLogger);
            MockActivityHandler = new MockActivityHandler(MockLogger);

            AdjustFactory.Logger = MockLogger;
            AdjustFactory.SetRequestHandler(MockRequestHandler);
            AdjustFactory.SetActivityHandler(MockActivityHandler);

            ActivityHandler = GetActivityHandler();

            PackageHandler = StartPackageHandler();
        }

        public override void TearDown()
        {
            AdjustFactory.SetRequestHandler(null);
            AdjustFactory.SetActivityHandler(null);
            AdjustFactory.Logger = null;
        }

        private PackageHandler StartPackageHandler()
        {
            // delete package queue for fresh start
            var packageQueueDeleted = Util.DeleteFile("AdjustIOPackageQueue");

            MockLogger.Test("Was PackageQueue deleted? " + packageQueueDeleted);

            PackageHandler packageHandler = new PackageHandler(
                activityHandler:MockActivityHandler,
                startPaused: false);

            DeviceUtil.Sleep(1000);

            Assert.Verbose("Package queue file not found");

            return packageHandler;
        }
        
        public void TestAddPackage()
        {
            ActivityPackage firstClickPackage = UtilTest.CreateClickPackage(ActivityHandler, "FirstPackage");

            PackageHandler.AddPackage(firstClickPackage);

            DeviceUtil.Sleep(1000);

            AddPackageTests(packageNumber: 1, packageString: "clickFirstPackage");

            PackageHandler secondPackageHandler = AddSecondPackageTest(null);

            ActivityPackage secondClickPackage = UtilTest.CreateClickPackage(ActivityHandler, "ThirdPackage");

            secondPackageHandler.AddPackage(secondClickPackage);

            DeviceUtil.Sleep(1000);

            AddPackageTests(packageNumber: 3, packageString: "clickThirdPackage");

            // send the first click package/ first package
            secondPackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            Assert.Test("RequestHandler SendPackage, clickFirstPackage");

            // send the second click package/ third package
            secondPackageHandler.SendNextPackage();
            DeviceUtil.Sleep(1000);

            Assert.Test("RequestHandler SendPackage, clickThirdPackage");

            // send the unknow package/ second package
            secondPackageHandler.SendNextPackage();
            DeviceUtil.Sleep(1000);

            Assert.Test("RequestHandler SendPackage, unknownSecondPackage");
        }

        public void TestSendFirst()
        {
            PackageHandler.SendFirstPackage();

            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.EMPTY_QUEUE);

            AddAndSendFirstPackageTest(PackageHandler);

            // try to send when it is still sending
            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.IS_SENDING);

            // try to send paused
            PackageHandler.PauseSending();
            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.PAUSED);

            // unpause, it's still sending
            PackageHandler.ResumeSending();
            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.IS_SENDING);

            // verify that both paused and isSending are reset with a new session
            PackageHandler secondSessionPackageHandler = new PackageHandler(
                activityHandler: MockActivityHandler,
                startPaused: false);

            secondSessionPackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            // send the package to request handler
            SendFirstTests(SendFirstState.SEND, "unknownFirstPackage");
        }

        public void TestSendNext()
        {
            // add and send the first package
            AddAndSendFirstPackageTest(PackageHandler);

            // try to send when it is still sending
            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.IS_SENDING);

            // add a second package
            AddSecondPackageTest(PackageHandler);

            //send next package
            PackageHandler.SendNextPackage();
            DeviceUtil.Sleep(2000);

            Assert.Debug("Package handler wrote 1 packages");

            // try to send the second package
            SendFirstTests(SendFirstState.SEND, "unknownSecondPackage");
        }

        public void TestCloseFirstPackage()
        {
            AddAndSendFirstPackageTest(PackageHandler);

            // try to send when it is still sending
            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(1000);

            SendFirstTests(SendFirstState.IS_SENDING);

            //send next package
            PackageHandler.CloseFirstPackage();
            DeviceUtil.Sleep(2000);

            Assert.NotDebug("Package handler wrote");

            PackageHandler.SendFirstPackage();
            DeviceUtil.Sleep(2000);

            // try to send the first package again
            SendFirstTests(SendFirstState.SEND, "unknownFirstPackage");
        }

        public void TestCalls()
        {
            // TODO test "will retry later"

            PackageHandler.FinishedTrackingActivity(null);

            Assert.Test("ActivityHandler FinishedTrackingActivity, Null");
        }

        private void SendFirstTests(SendFirstState sendFirstState, string packageString = null)
        {
            if (sendFirstState == SendFirstState.PAUSED)
            {
                Assert.Debug("Package handler is paused");
            }
            else
            {
                Assert.NotDebug("Package handler is paused");
            }

            if (sendFirstState == SendFirstState.IS_SENDING)
            {
                Assert.Verbose("Package handler is already sending");
            }
            else
            {
                Assert.NotVerbose("Package handler is already sending");
            }

            if (sendFirstState == SendFirstState.SEND)
            {
                Assert.Test("RequestHandler SendPackage, " + packageString);
            }
            else
            {
                Assert.NotTest("RequestHandler SendPackage");
            }
        }

        private void AddAndSendFirstPackageTest(PackageHandler packageHandler)
        {
            // add a package
            ActivityPackage activityPackage = CreateUnknowPackage("FirstPackage");

            // send the first package
            packageHandler.AddPackage(activityPackage);

            packageHandler.SendFirstPackage();
            DeviceUtil.Sleep(2000);

            AddPackageTests(1, "unknownFirstPackage");

            SendFirstTests(SendFirstState.SEND, "unknownFirstPackage");
        }


        private PackageHandler AddSecondPackageTest(PackageHandler packageHandler)
        {
            if (packageHandler == null)
            {
                packageHandler = new PackageHandler(
                    activityHandler: MockActivityHandler,
                    startPaused: false);

                DeviceUtil.Sleep(1000);

                // check that it can read the previously saved package
                Assert.Debug("Package handler read 1 packages");
            }

            ActivityPackage secondActivityPackage = CreateUnknowPackage("SecondPackage");

            packageHandler.AddPackage(secondActivityPackage);

            DeviceUtil.Sleep(1000);

            AddPackageTests(packageNumber: 2, packageString: "unknownSecondPackage");

            return packageHandler;
        }

        private ActivityHandler GetActivityHandler()
        {
            ActivityHandler activityHandler = UtilTest.GetActivityHandler(MockLogger, DeviceUtil);

            MockLogger.Reset();

            return activityHandler;
        }

        private ActivityPackage CreateUnknowPackage(string suffix)
        {
            var activityPackage = UtilTest.CreateClickPackage(ActivityHandler, suffix); 
            activityPackage.ActivityKind = ActivityKind.Unknown;
            return activityPackage;
        }
        
        private void AddPackageTests(int packageNumber, string packageString)
        {
            Assert.Debug("Added package {0} ({1})", packageNumber, packageString);

            Assert.Debug("Package handler wrote {0} packages", packageNumber);
        }


        /*
        public void TestFirstPackage()
        {
            // deleting previously created package queue file to make a new queue
            MockLogger.Test("Was the package queue file deleted? {0}", Util.DeleteFile("AdjustIOPackageQueue"));

            // initialize the package handler
            var packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            // set the mock request handler to respond to the package handler
            MockRequestHandler.SetPackageHandler(packageHandler);

            // enable sending packages to request handler
            packageHandler.ResumeSending();

            // build a package
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = DeviceUtil.GetUserAgent(),
                ClientSdk = DeviceUtil.ClientSdk,
            };
            var sessionPackage = packageBuilder.BuildSessionPackage();

            // and add it to the queue
            packageHandler.AddPackage(sessionPackage);

            // send the first package
            packageHandler.SendFirstPackage();

            // it's necessary to sleep the activity for a while after each handler call
            // to let the internal queue act
            DeviceUtil.Sleep(1000);

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOPackageQueue (not found)"),
                MockLogger.ToString());

            // check that added first package to a previously empty queue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Added package 1 (session)"),
                MockLogger.ToString());

            // verify that the package queue is written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 1 packages"),
                MockLogger.ToString());

            // request handler should have receive the package
            Assert.IsTrue(MockLogger.DeleteTestUntil("RequestHandler SendPackage"),
                MockLogger.ToString());

            // request should have responded back to send the next package
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 0 packages"),
                MockLogger.ToString());

            // create a package handler to simulate a new launch
            packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            DeviceUtil.Sleep(1000);

            // check that it reads the empty queue file
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler read 0 packages"),
                MockLogger.ToString());
        }

        public void TestPause()
        {
            // initialize the package handler
            var packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            // set the mock request handler to respond to the package handler
            MockRequestHandler.SetPackageHandler(packageHandler);

            // disable sending packages
            packageHandler.PauseSending();

            // build a package
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = DeviceUtil.GetUserAgent(),
                ClientSdk = DeviceUtil.ClientSdk,
            };
            var sessionPackage = packageBuilder.BuildSessionPackage();

            // and add it to the queue
            packageHandler.AddPackage(sessionPackage);

            // send the first package
            packageHandler.SendFirstPackage();

            DeviceUtil.Sleep(1000);

            // check that the package handler is paused
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler is paused"),
                MockLogger.ToString());
        }

        public void TestMultiplePackages()
        {
            // deleting previously created package queue file to make a new queue
            MockLogger.Test("Was the package queue file deleted? {0}", Util.DeleteFile("AdjustIOPackageQueue"));

            // initialize the package handler
            var packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            // enable sending packages to request handler
            packageHandler.ResumeSending();

            // build a package
            var packageBuilder = new PackageBuilder()
            {
                UserAgent = DeviceUtil.GetUserAgent(),
                ClientSdk = DeviceUtil.ClientSdk,
            };
            var sessionPackage = packageBuilder.BuildSessionPackage();

            // and add it 3 times to the queue
            packageHandler.AddPackage(sessionPackage);
            packageHandler.AddPackage(sessionPackage);
            packageHandler.AddPackage(sessionPackage);

            // send the first two packages without closing the first
            packageHandler.SendFirstPackage();
            packageHandler.SendFirstPackage();

            DeviceUtil.Sleep(1000);

            // test that the file did not exist in the first run of the application
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Error, "Failed to read file AdjustIOPackageQueue (not found)"),
                MockLogger.ToString());

            // check that added the 3 session packages
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Added package 3 (session)"),
                MockLogger.ToString());

            // verify that the 3 packages from the queue are written
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler wrote 3 packages"),
                MockLogger.ToString());

            // check that the package handler was already sending one package when a second one was tried
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Verbose, "Package handler is already sending"),
                MockLogger.ToString());

            // create a package handler to simulate a new launch
            packageHandler = new PackageHandler(new MockActivityHandler(MockLogger));

            DeviceUtil.Sleep(1000);

            // check that it reads the 3 packages from the file to the queue
            Assert.IsTrue(MockLogger.DeleteLogUntil(LogLevel.Debug, "Package handler read 3 packages"),
                MockLogger.ToString());
        }
         * */
    }

}