using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace adeven.AdjustIo
{
    public class PostRequest
    {
        private HttpWebRequest request;
        private string paramString;
        private string successMessage;
        private string failureMessage;

        public PostRequest(string path)
        {
            string url = Util.BaseUrl + path;
            request = WebRequest.CreateHttp(url);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.Headers["Client-SDK"] = Util.ClientSdk;
        }

        public string SuccessMessage { set { successMessage = value; } }

        public string FailureMessage { set { failureMessage = value; } }

        public string UserAgent { set { request.Headers["User-Agent"] = value; } }

        public void Start(Dictionary<string, string> parameters)
        {
            paramString = Util.GetStringEncodedParameters(parameters);
            request.BeginGetRequestStream(new AsyncCallback(streamCallback), null);
        }

        private void streamCallback(IAsyncResult result)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(paramString);
            Stream postStream = request.EndGetRequestStream(result);
            postStream.Write(byteArray, 0, paramString.Length);
            postStream.Dispose();
            request.BeginGetResponse(new AsyncCallback(responseCallback), null);
        }

        private void responseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebResponse response = request.EndGetResponse(asynchronousResult) as HttpWebResponse;
                string responseString = readResponse(response);
                response.Close();
                Debug.WriteLine("[{0}] {1}", Util.LogTag, successMessage);
            }
            catch (WebException e)
            {
                HttpWebResponse response = e.Response as HttpWebResponse;
                string responseString = readResponse(response);
                response.Close();
                Debug.WriteLine("[{0}] {1} ({2})", Util.LogTag, failureMessage, responseString);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private static string readResponse(HttpWebResponse response)
        {
            Stream streamResponse = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(streamResponse);
            string responseString = streamReader.ReadToEnd().Trim();
            streamReader.Close();
            streamResponse.Close();
            return responseString;
        }
    }
}