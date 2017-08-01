using System;
using AdjustSdk;
using AdjustSdk.Pcl;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace WindowsPclTest
{
    public class ActivityHandlerTest
    {
        private readonly Mock<IAdjustConfig> _adjustConfigMock;
        private readonly Mock<IDeviceUtil> _deviceUtilMock;
        private readonly Mock<IActionQueue> _actionQueueMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ITestOutputHelper _debugOutput;

        public ActivityHandlerTest(ITestOutputHelper debugOutput)
        {
            _debugOutput = debugOutput;
            
            _adjustConfigMock = new Mock<IAdjustConfig>();
            _adjustConfigMock.Setup(c => c.AppToken).Returns("test_app_token");
            _adjustConfigMock.Setup(c => c.Environment).Returns("sandbox");
            _adjustConfigMock.Setup(c => c.DefaultTracker).Returns("def_tracker_test");
            _adjustConfigMock.Setup(c => c.PushToken).Returns("push_token_test");

            _deviceUtilMock = new Mock<IDeviceUtil>();
            _deviceUtilMock.Setup(du => du.GetDeviceInfo()).Returns(new DeviceInfo
            {
                DeviceUniqueId = "dummy_unique_id",
                HardwareId = "dummy_hardware_id",
                NetworkAdapterId = "dummy_network_adapter_id",
                ReadWindowsAdvertisingId = () => "dummy_advertising_id"
            });

            _actionQueueMock = new Mock<IActionQueue>();
            _actionQueueMock.Setup(aq => aq.Name).Returns("adjust.ActivityHandler");

            _loggerMock = new Mock<ILogger>();
            _loggerMock.Setup(l => l.IsProductionEnvironment).Returns(true);
            _loggerMock.Setup(l => l.LogLevel).Returns(LogLevel.Warn);
        }

        [Fact]
        public void TestActivityHandlerConstruction()
        {
            _debugOutput.WriteLine("TestActivityHandlerConstructor started...");

            _actionQueueMock.Setup(aq => aq.Enqueue(It.IsAny<Action>()));
            _actionQueueMock.Setup(aq => aq.Delay(It.IsAny<TimeSpan>(), It.IsAny<Action>()));

            ActivityHandler activityHandler = ActivityHandler.GetInstance
                (null, _deviceUtilMock.Object, _actionQueueMock.Object, _loggerMock.Object);
            Assert.Null(activityHandler);

            _adjustConfigMock.Setup(c => c.IsValid()).Returns(false);
            activityHandler = ActivityHandler.GetInstance
                (_adjustConfigMock.Object, _deviceUtilMock.Object, _actionQueueMock.Object, _loggerMock.Object);
            Assert.Null(activityHandler);

            _adjustConfigMock.Setup(c => c.IsValid()).Returns(true);
            activityHandler = ActivityHandler.GetInstance
                (_adjustConfigMock.Object, _deviceUtilMock.Object, _actionQueueMock.Object, _loggerMock.Object);
            Assert.NotNull(activityHandler);
        }

        [Fact]
        public void TestActivityHandlerInitI()
        {
            _actionQueueMock.Setup(aq => aq.Enqueue(It.IsAny<Action>())).Callback((Action a) => a());
            _actionQueueMock.Setup(aq => aq.Delay(It.IsAny<TimeSpan>(), It.IsAny<Action>())).Callback(
                (TimeSpan ts, Action a) =>
                {
                    a();
                });

            _adjustConfigMock.Setup(c => c.IsValid()).Returns(true);

            ActivityHandler activityHandler = ActivityHandler.GetInstance
                (_adjustConfigMock.Object, _deviceUtilMock.Object, _actionQueueMock.Object, _loggerMock.Object);
            Assert.NotNull(activityHandler);
            Assert.True(activityHandler.IsEnabled());

            activityHandler.TrackEvent(new AdjustEvent("dummy1"));
            activityHandler.ApplicationActivated();
        }
    }
}
