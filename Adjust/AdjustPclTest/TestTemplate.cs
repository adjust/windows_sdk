using AdjustSdk.Pcl;

namespace AdjustTest.Pcl
{
    public abstract class TestTemplate
    {
        protected IAssert Assert { get; set; }
        protected MockLogger MockLogger { get; set; }
        protected DeviceUtil DeviceUtil { get; set; }

        protected TestTemplate(DeviceUtil deviceUtil, IAssert assert)
        {
            DeviceUtil = deviceUtil;
            Assert = assert;
        }

        public virtual void SetUp()
        {
            MockLogger = new MockLogger();
            Assert.MockLogger = MockLogger;
        }

        public abstract void TearDown();
    }
}