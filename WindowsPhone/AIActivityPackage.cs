using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class AIActivityPackage
    {
        //data
        internal string Path { get; set; }
        internal string UserAgent { get; set; }
        internal string ClientSdk { get; set; }
        internal Dictionary<string, string> Parameters { get; set; }

        //logs
        internal string Kind { get; set; }
        internal string Suffix { get; set; }
    }
}
