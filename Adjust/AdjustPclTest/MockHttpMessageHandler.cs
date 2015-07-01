using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdjustTest.Pcl
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage HttpRequestMessage { get; set; }
        public ResponseType ResponseType { get; set; }

        private MockLogger MockLogger { get; set; }
        private const string prefix = "HttpMessageHandler";

        public HttpStatusCode ResponseStatusCode { get; set; }

        public MockHttpMessageHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
            ResponseStatusCode = HttpStatusCode.OK;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            MockLogger.Test("{0} SendAsync, responseType {1}", prefix, ResponseType);

            HttpRequestMessage = request;
            /*
            return Task.FromResult(new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(ResponseMessage)))
            });
             * */
            return null;
        }
    }

    public enum ResponseType {
        NULL, CLIENT_PROTOCOL_EXCEPTION, INTERNAL_SERVER_ERROR, WRONG_JSON, EMPTY_JSON, MESSAGE, ATTRIBUTION, ASK_IN
    }
}