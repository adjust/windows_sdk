using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.Pcl
{
    public interface IAssert
    {
        MockLogger MockLogger { get; set; }

        void Verbose(string message, params object[] parameters);

        void Debug(string message, params object[] parameters);

        void Info(string message, params object[] parameters);

        void Warn(string message, params object[] parameters);

        void Error(string message, params object[] parameters);

        void AssertMessage(string message, params object[] parameters);

        void Test(string message, params object[] parameters);

        void NotVerbose(string message, params object[] parameters);

        void NotDebug(string message, params object[] parameters);

        void NotInfo(string message, params object[] parameters);

        void NotWarn(string message, params object[] parameters);

        void NotError(string message, params object[] parameters);

        void NotAssertMessage(string message, params object[] parameters);

        void NotTest(string message, params object[] parameters);

        void IsTrue(bool condition, string message);

        void IsTrue(bool condition);

        void IsFalse(bool condition, string message);

        void IsFalse(bool condition);

        void AreEqual<T>(T expected, T actual);

        void AreEqual<T>(T expected, T actual, string message);

        void Fail();

        void Fail(string message);

        void NotNull<T>(T value, string message);

        void NotNull<T>(T value);

        void Null<T>(T value, string message);

        void Null<T>(T value);
    }
}