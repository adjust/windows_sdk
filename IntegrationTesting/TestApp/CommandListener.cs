using System.Collections.Generic;
using TestLibrary;

namespace TestApp
{
    public class CommandListener : ICommandListener
    {
        private readonly AdjustCommandExecutor _adjustCommandExecutor;

        public CommandListener()
        {
            _adjustCommandExecutor = new AdjustCommandExecutor();
        }

        public void SetTestLibrary(TestLibrary.TestLibrary testLibrary)
        {
            _adjustCommandExecutor?.SetTestLibrary(testLibrary);
        }

        public void ExecuteCommand(string className, string methodName, Dictionary<string, List<string>> parameters)
        {
            switch (className.ToLower())
            {
                case "adjust":
                    _adjustCommandExecutor.ExecuteCommand(new Command(className, methodName, parameters));
                    break;
                default:
                    Log.Debug("Could not find {0} class to execute", className);
                    break;
            }
        }
    }
}