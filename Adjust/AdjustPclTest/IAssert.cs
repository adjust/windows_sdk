using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.Pcl
{
    public interface IAssert
    {
        void IsTrue(bool condition, string message);

        void IsFalse(bool condition, string message);

        void AreEqual<T>(T expected, T actual);

        void AreEqual<T>(T expected, T actual, string message);
    }
}