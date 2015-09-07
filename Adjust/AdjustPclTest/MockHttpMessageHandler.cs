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

        public MockHttpMessageHandler(MockLogger mockLogger)
        {
            MockLogger = mockLogger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            MockLogger.Test("{0} SendAsync, responseType: {1}", prefix, ResponseType);

            HttpRequestMessage = request;

            switch (ResponseType)
            {
                case ResponseType.CLIENT_PROTOCOL_EXCEPTION:
                    throw new WebException("testResponseError");
                case ResponseType.WRONG_JSON:
                    return GetOkResponse("not a json response");
                case ResponseType.EMPTY_JSON:
                    return GetOkResponse("{ }");
                case ResponseType.INTERNAL_SERVER_ERROR:
                    return GetMockResponse("{ \"message\": \"testResponseError\"}", HttpStatusCode.InternalServerError);
                case ResponseType.MESSAGE:
                    return GetOkResponse("{ \"message\": \"response OK\"}");
                case ResponseType.ATTRIBUTION:
                    //return GetOkResponse("{ \"attribution\" : {\"nothing\" : \"somevalue\"}}");
                    return GetOkResponse("{ \"attribution\" : {" +
                            "\"tracker_token\" : \"ttValue\" , " +
                            "\"tracker_name\"  : \"tnValue\" , " +
                            "\"network\"       : \"nValue\" , " +
                            "\"campaign\"      : \"cpValue\" , " +
                            "\"adgroup\"       : \"aValue\" , " +
                            "\"creative\"      : \"ctValue\" , " +
                            "\"click_label\"   : \"clValue\" } }");
                case ResponseType.ASK_IN:
                    return GetOkResponse("{ \"ask_in\" : 4000 }");
            }

            return null;
        }

        private Task<HttpResponseMessage> GetOkResponse(string responseMessage)
        {
            return GetMockResponse(responseMessage, HttpStatusCode.OK);
        }

        private Task<HttpResponseMessage> GetMockResponse(string responseMessage, HttpStatusCode responseStatusCode)
        {
            return Task.FromResult(new HttpResponseMessage(responseStatusCode)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(responseMessage)))
            });
        }
    }

    public enum ResponseType {
        NULL, CLIENT_PROTOCOL_EXCEPTION, INTERNAL_SERVER_ERROR, WRONG_JSON, EMPTY_JSON, MESSAGE, ATTRIBUTION, ASK_IN
    }
}