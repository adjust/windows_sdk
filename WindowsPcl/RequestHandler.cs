using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class RequestHandler : IRequestHandler
    {
        private IPackageHandler PackageHandler { get; set; }
        private ILogger Logger { get; set; }
        private HttpClient HttpClient { get; set; }

        private struct SendResponse
        {
            internal bool WillRetry { get; set; }

            internal Dictionary<string, string> JsonDict { get; set; }
        }

        public RequestHandler(IPackageHandler packageHandler)
        {
            Logger = AdjustFactory.Logger;

            Init(packageHandler);
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
            var httpClient = GetHttpClient(activityPackage);
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
            var jsonDict = Util.ParseJsonResponse(httpResponseMessage);
            var willRetry = jsonDict == null;

            var sendResponse = new SendResponse
            {
                WillRetry = willRetry,
                JsonDict = jsonDict,
            };

            if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)    // 501
            {
                Logger.Error("{0}. (Status code: {1}).",
                    activityPackage.FailureMessage(),
                    (int)httpResponseMessage.StatusCode);
            }
            else if (!httpResponseMessage.IsSuccessStatusCode)
            {
                sendResponse.WillRetry = true;

                Logger.Error("{0}. (Status code: {1}). Will retry later.",
                    activityPackage.FailureMessage(),
                    (int)httpResponseMessage.StatusCode);
            }

            return sendResponse;
        }

        private SendResponse ProcessException(WebException webException, ActivityPackage activityPackage)
        {
            using (var response = webException.Response as HttpWebResponse)
            {
                int? statusCode = (response == null) ? null : (int?)response.StatusCode;

                var sendResponse = new SendResponse
                {
                    WillRetry = true,
                    JsonDict = Util.ParseJsonExceptionResponse(response)
                };

                Logger.Error("{0}. ({1}, Status code: {2}). Will retry later.",
                    activityPackage.FailureMessage(),
                    Util.ExtractExceptionMessage(webException),
                    statusCode);

                return sendResponse;
            }
        }

        private SendResponse ProcessException(Exception exception, ActivityPackage activityPackage)
        {
            Logger.Error("{0}. ({1}). Will retry later", activityPackage.FailureMessage(), Util.ExtractExceptionMessage(exception));

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

            if (successRunning && SendTask.Result.JsonDict != null)
                PackageHandler.FinishedTrackingActivity(SendTask.Result.JsonDict);

            //Logger.Debug("SendTask.Result.WillRetry {0}", SendTask.Result.WillRetry);
            if (successRunning && !SendTask.Result.WillRetry)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }

        private HttpClient GetHttpClient(ActivityPackage activityPackage)
        {
            if (HttpClient == null)
            {
                HttpClient = Util.BuildHttpClient(activityPackage.ClientSdk);
            }
            return HttpClient;
        }
    }
}