using System.Collections.Generic;

namespace TestApp
{
    public class Command
    {
        internal string ClassName;
        internal string MethodName;
        internal Dictionary<string, List<string>> Parameters;

        public Command(string className, string methodName, Dictionary<string, List<string>> parameters)
        {
            ClassName = className;
            MethodName = methodName;
            Parameters = parameters;
        }

        public string GetFirstParameterValue(string parameterKey)
        {
            if (Parameters == null || !Parameters.ContainsKey(parameterKey))
                return null;

            var parameterValues = Parameters[parameterKey];
            return parameterValues.Count == 0 ? null : parameterValues[0];
        }

        public bool ContainsParameter(string parameterKey)
        {
            if (Parameters == null || string.IsNullOrEmpty(parameterKey))
                return false;

            return Parameters.ContainsKey(parameterKey);
        }
    }
}