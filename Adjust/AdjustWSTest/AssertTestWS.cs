using AdjustSdk;
using AdjustTest.Pcl;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Globalization;

namespace AdjustTest.WS
{
    public class AssertTestWS : IAssert
    {
        public MockLogger MockLogger { get; set; }

        public void Verbose(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Verbose, message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Debug, message, parameters);
        }

        public void Info(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Info, message, parameters);
        }

        public void Warn(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Warn, message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Error, message, parameters);
        }

        public void AssertMessage(string message, params object[] parameters)
        {
            ContainsMessage(LogLevel.Assert, message, parameters);
        }

        public void Test(string message, params object[] parameters)
        {
            var formattedMessage = String.Format(CultureInfo.InvariantCulture, message, parameters);

            Assert.IsTrue(MockLogger.DeleteTestUntil(formattedMessage),
                MockLogger.ToString());
        }

        public void NotVerbose(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Verbose, message, parameters);
        }

        public void NotDebug(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Debug, message, parameters);
        }

        public void NotInfo(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Info, message, parameters);
        }

        public void NotWarn(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Warn, message, parameters);
        }

        public void NotError(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Error, message, parameters);
        }

        public void NotAssertMessage(string message, params object[] parameters)
        {
            NotContainsMessage(LogLevel.Assert, message, parameters);
        }

        public void NotTest(string message, params object[] parameters)
        {
            var formattedMessage = String.Format(CultureInfo.InvariantCulture, message, parameters);

            Assert.IsFalse(MockLogger.DeleteTestUntil(formattedMessage),
                MockLogger.ToString());
        }

        public void IsTrue(bool condition, string message)
        {
            Assert.IsTrue(condition, message);
        }

        public void IsTrue(bool condition)
        {
            Assert.IsTrue(condition, MockLogger.ToString());
        }

        public void IsFalse(bool condition, string message)
        {
            Assert.IsFalse(condition, message);
        }

        public void IsFalse(bool condition)
        {
            Assert.IsFalse(condition, MockLogger.ToString());
        }
        
        public void AreEqual<T>(T expected, T actual)
        {
            AreEqual<T>(expected, actual, MockLogger.ToString());
        }

        public void AreEqual<T>(T expected, T actual, string message)
        {
            Assert.AreEqual<T>(expected, actual, message);
        }

        public void Fail()
        {
            Fail(MockLogger.ToString());
        }

        public void Fail(string message)
        {
            Assert.Fail(message);
        }

        public void NotNull<T>(T value, string message)
        {
            Assert.IsNotNull(value, message);
        }

        public void NotNull<T>(T value)
        {
            Assert.IsNotNull(value, MockLogger.ToString());
        }

        public void Null<T>(T value, string message)
        {
            Assert.IsNull(value, message);
        }

        public void Null<T>(T value)
        {
            Assert.IsNull(value, MockLogger.ToString());
        }

        private void ContainsMessage(LogLevel logLevel, string message, object[] parameters)
        {

            var formattedMessage = String.Format(CultureInfo.InvariantCulture, message, parameters);

            IsTrue(MockLogger.DeleteLogUntil(logLevel, formattedMessage),
                MockLogger.ToString());
        }

        private void NotContainsMessage(LogLevel logLevel, string message, object[] parameters)
        {
            var formattedMessage = String.Format(CultureInfo.InvariantCulture, message, parameters);

            IsFalse(MockLogger.DeleteLogUntil(logLevel, formattedMessage),
                MockLogger.ToString());
        }
    }
}