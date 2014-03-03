using AdjustSdk.Pcl;

namespace AdjustTest.Pcl
{
    public abstract class TestTemplate
    {
        protected IAssert Assert;
        protected MockLogger MockLogger;
        protected DeviceUtil DeviceUtil;

        protected TestTemplate(DeviceUtil deviceUtil, IAssert assert)
        {
            DeviceUtil = deviceUtil;
            Assert = assert;
        }

        public virtual void SetUp()
        {
            MockLogger = new MockLogger();
            AdjustFactory.Logger = MockLogger;
        }

        public abstract void TearDown();
    }
}