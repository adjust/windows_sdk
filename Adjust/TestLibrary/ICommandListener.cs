using System.Collections.Generic;

namespace TestLibrary
{
    public interface ICommandListener
    {
        void ExecuteCommand(string className, string methodName, Dictionary<string, List<string>> parameters);
    }
}