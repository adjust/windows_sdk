using System.Collections.Generic;

namespace TestLibrary.Networking
{
    public class HttpResponse
    {
        public string Response { get; set; }
        public int? ResponseCode { get; set; }
        public Dictionary<string, List<string>> HeaderFields { get; set; }
    }
}