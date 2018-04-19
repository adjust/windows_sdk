using System.Collections.Generic;

namespace TestLibrary
{
    public class TestCommand
    {
        public string ClassName { get; set; }
        public string FunctionName { get; set; }
        public Dictionary<string, List<string>> Params { get; set; }
    }
}
