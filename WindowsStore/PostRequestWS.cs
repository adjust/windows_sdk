using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace adeven.AdjustIo
{
    internal class PostRequest
    {
        private HttpClient client;
        private string path;
        private string successMessage;
        private string failureMessage;

        public PostRequest(string path)
        {
            this.path = path;
            client = new HttpClient();
            client.BaseAddress = new Uri(Util.BaseUrl);

            client.DefaultRequestHeaders.Add("Client-SDK", Util.ClientSdk);
        }

        public string SuccessMessage { set { successMessage = value; } }

        public string FailureMessage { set { failureMessage = value; } }

        public string UserAgent
        {
            set { client.DefaultRequestHeaders.Add("User-Agent", value); }
        }

        public async void Start(Dictionary<string, string> parameters)
        {
            HttpContent content = getParamContent(parameters);
            HttpResponseMessage response = await client.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine("[{0}] {1}", Util.LogTag, successMessage);
            }
            else
            {
                string responseString = await response.Content.ReadAsStringAsync();
                string trimmedResponse = responseString.Trim();
                Debug.WriteLine("[{0}] {1} ({2})", Util.LogTag, failureMessage, trimmedResponse);
            }

            client.Dispose();
        }

        private static HttpContent getParamContent(Dictionary<string, string> parameters)
        {
            MultipartFormDataContent content = new MultipartFormDataContent();
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                content.Add(new StringContent(pair.Value), pair.Key);
            }
            return content;
        }
    }
}