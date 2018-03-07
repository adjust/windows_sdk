using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestLibrary.Networking
{
    public class UtilsNetworking
    {
        private static readonly string CLIENT_SDK = "Client-SDK";
        private static readonly string TEST_NAMES = "Test-Names";
        private static readonly string LOCAL_IP = "Local-Ip";

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(Constants.ONE_MINUTE)
        };

        public static Task<HttpResponse> SendPostI(string path, string localIp)
        {
            return SendPostI(path, null, null, localIp, null);
        }

        public static Task<HttpResponse> SendPostI(string path, string clientSdk, string localIp)
        {
            return SendPostI(path, clientSdk, null, localIp, null);
        }

        public static Task<HttpResponse> SendPostI(string path, string clientSdk, string localIp, string testNames)
        {
            return SendPostI(path, clientSdk, testNames, localIp, null);
        }

        public static Task<HttpResponse> SendPostI(string path, string clientSdk, string localIp,
            Dictionary<string, string> postBody)
        {
            return SendPostI(path, clientSdk, null, localIp, postBody);
        }

        public static async Task<HttpResponse> SendPostI(string path, string clientSdk, string testNames,
            string localIp, Dictionary<string, string> postBody)
        {
            var targetUrl = TestLibrary.BaseUrl + path;
            var response = new HttpResponse();
            try
            {
                var requestContent = postBody != null
                    ? new FormUrlEncodedContent(postBody)
                    : new FormUrlEncodedContent(new Dictionary<string, string>(0));

                if (!string.IsNullOrEmpty(clientSdk))
                    requestContent.Headers.Add(CLIENT_SDK, clientSdk);

                if (!string.IsNullOrEmpty(testNames))
                    requestContent.Headers.Add(TEST_NAMES, testNames);

                if (!string.IsNullOrEmpty(localIp))
                    requestContent.Headers.Add(LOCAL_IP, localIp);

                //TODO: SSL/TSL??

                //TODO: set cache?

                Log.Debug(nameof(UtilsNetworking), "----- EXECUTING POST REQUEST [{0}]", targetUrl);

                var httpResponse = await _httpClient.PostAsync(targetUrl, requestContent);
                response.ResponseCode = (int) httpResponse.StatusCode;
                response.HeaderFields = new Dictionary<string, List<string>>();

                Log.Debug(nameof(UtilsNetworking), "----- POST REQUEST [{0}] RECEIVED", targetUrl);

                if (httpResponse.Headers != null)
                {
                    using (var headersEnumerator = httpResponse.Headers?.GetEnumerator())
                    {
                        while (headersEnumerator.MoveNext())
                            response.HeaderFields.Add(headersEnumerator.Current.Key,
                                new List<string>(headersEnumerator.Current.Value));
                    }
                }

                response.Response = await httpResponse.Content.ReadAsStringAsync();

                return response;
            }
            catch (Exception e)
            {
                Log.Error(nameof(UtilsNetworking), "Error while executing POST request to [{0}]. {1}", targetUrl, e.ToString());
            }

            return null;
        }
    }
}