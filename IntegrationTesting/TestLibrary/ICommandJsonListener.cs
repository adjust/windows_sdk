using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLibrary
{
    public interface ICommandJsonListener
    {
        void ExecuteCommand(string className, string methodName, string jsonParameters);
    }
}
