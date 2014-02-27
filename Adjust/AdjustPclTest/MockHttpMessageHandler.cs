using AdjustSdk.Pcl;
using AdjustSdk.Pcl.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Test.Pcl
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage HttpRequestMessage { get; set; }

        private MockLogger MockLogger;
        private const string prefix = "HttpMessageHandler";

        public string ResponseMessage { get; set; }

        public HttpStatusCode ResponseStatusCode { get; set; }

        public MockHttpMessageHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
            ResponseMessage = "";
            ResponseStatusCode = HttpStatusCode.OK;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            MockLogger.Test("{0} SendAsync", prefix);

            HttpRequestMessage = request;

            return Task.FromResult(new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(ResponseMessage)))
            });
        }
    }
}