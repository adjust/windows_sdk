using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLibrary
{
    public interface ICommandRawJsonListener
    {
        void ExecuteCommand(string jsonCommand);
    }
}
