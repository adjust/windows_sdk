using AdjustSdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class RequestHandler : IRequestHandler
    {
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        private IPackageHandler PackageHandler;
        private static ILogger Logger = AdjustFactory.Logger;
        private HttpMessageHandler HttpMessageHandler;

        private struct SendResponse
        {
            internal bool WillRetry { get; set; }
            internal Dictionary<string, string> JsonDict { get; set; }
        }

        public RequestHandler(IPackageHandler packageHandler)
        {
            Init(packageHandler);
            HttpMessageHandler = AdjustFactory.GetHttpMessageHandler();
        }

        public void Init(IPackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
        }

        public void SendPackage(ActivityPackage package)
        {
            Task.Factory.StartNew(() => SendInternal(package))
                // continuation used to prevent unhandled exceptions in SendInternal
                // not signaling the WaitHandle in PackageHandler and preventing deadlocks
                .ContinueWith((sendResponse) => PackageSent(sendResponse));
        }

        public void SendClickPackage(ActivityPackage clickPackage)
        {
            Task.Factory.StartNew(() => SendInternal(clickPackage));
        }

        private SendResponse SendInternal(ActivityPackage activityPackage)
        {
            SendResponse sendResponse;
            try
            {
                using (var httpResponseMessage = ExecuteRequest(activityPackage))
                {
                    sendResponse = ProcessResponse(httpResponseMessage, activityPackage);
                }
            }
            catch (WebException we) { sendResponse = ProcessException(we, activityPackage); }
            catch (Exception ex) { sendResponse = ProcessException(ex, activityPackage); }

            return sendResponse;
        }

        private HttpResponseMessage ExecuteRequest(ActivityPackage activityPackage)
        {
            var httpClient = new HttpClient(HttpMessageHandler);

            httpClient.Timeout = Timeout;
            httpClient.DefaultRequestHeaders.Add("Client-SDK", activityPackage.ClientSdk);

            var url = Util.BaseUrl + activityPackage.Path;

            var sNow = Util.DateFormat(DateTime.Now);
            activityPackage.Parameters["sent_at"] = sNow;

            using (var parameters = new FormUrlEncodedContent(activityPackage.Parameters))
            {
                return httpClient.PostAsync(url, parameters).Result;
            }
        }

        private SendResponse ProcessResponse(HttpResponseMessage httpResponseMessage, ActivityPackage activityPackage)
        {
            var sendResponse = new SendResponse 
            {
                WillRetry = false
            };

            string sResponse = null;
            using (var content = httpResponseMessage.Content)
            {
                sResponse = content.ReadAsStringAsync().Result;
            }

            sendResponse.JsonDict = Util.BuildJsonDict(sResponse, httpResponseMessage.IsSuccessStatusCode);

            if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)    // 501
            {
                Logger.Error("{0}. ({1}).",
                    activityPackage.FailureMessage(),
                    (int)httpResponseMessage.StatusCode);
            }
            else if (!httpResponseMessage.IsSuccessStatusCode)
            {
                sendResponse.WillRetry = true;

                Logger.Error("{0}. ({1}). Will retry later.",
                    activityPackage.FailureMessage(),
                    (int)httpResponseMessage.StatusCode);
            }
            

            return sendResponse;
        }

        private SendResponse ProcessException(WebException webException, ActivityPackage activityPackage)
        {
            
            using (var response = webException.Response as HttpWebResponse)
            using (var streamResponse = response.GetResponseStream())
            using (var streamReader = new StreamReader(streamResponse))
            {
                var sResponse = streamReader.ReadToEnd();

                var sendResponse = new SendResponse
                {
                    WillRetry = true,
                    JsonDict = Util.BuildJsonDict(sResponse, false)
                };
                
                Logger.Error("{0}. ({1}). Will retry later.",
                    activityPackage.FailureMessage(),
                    (int)response.StatusCode);

                return sendResponse;
            }
        }

        private SendResponse ProcessException(Exception exception, ActivityPackage activityPackage)
        {
            Logger.Error("{0}. ({1}). Will retry later", activityPackage.FailureMessage(), exception.Message);

            return new SendResponse
            {
                WillRetry = true,
            };
        }

        private void PackageSent(Task<SendResponse> SendTask)
        {
            // status needs to be tested before reading the result.
            // section "Passing data to a continuation" of
            // http://msdn.microsoft.com/en-us/library/ee372288(v=vs.110).aspx
            var successRunning =
                !SendTask.IsFaulted
                && !SendTask.IsCanceled;

            if (successRunning)
                PackageHandler.FinishedTrackingActivity(SendTask.Result.JsonDict);

            if (successRunning && !SendTask.Result.WillRetry)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }
    }
}