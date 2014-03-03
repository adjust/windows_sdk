using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdjustWPTest
{
    public class AssertTestTools : IAssert
    {
        public void IsTrue(bool condition, string message)
        {
            Assert.IsTrue(condition, message);
        }

        public void IsFalse(bool condition, string message)
        {
            Assert.IsFalse(condition, message);
        }

        public void AreEqual<T>(T expected, T actual)
        {
            Assert.AreEqual<T>(expected, actual);
        }

        public void AreEqual<T>(T expected, T actual, string message)
        {
            Assert.AreEqual<T>(expected, actual, message);
        }
    }
}