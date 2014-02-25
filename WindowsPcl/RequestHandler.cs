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
    internal class RequestHandler : AdjustSdk.Pcl.IRequestHandler
    {
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        private PackageHandler PackageHandler;
        private Action<ResponseData> ResponseDelegate;
        private ILogger Logger;

        public RequestHandler(PackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
            Logger = AdjustFactory.Logger;
        }

        public void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            ResponseDelegate = responseDelegate;
        }

        public void SendPackage(ActivityPackage package)
        {
            Task.Factory.StartNew(() => SendInternal(package))
                // continuation used to prevent unhandled exceptions in SendInternal
                // not signaling the WaitHandle in PackageHandler and preventing deadlocks
                .ContinueWith((responseData) => PackageSent(responseData));
        }

        private ResponseData SendInternal(ActivityPackage activityPackage)
        {
            ResponseData responseData;
            try
            {
                using (var httpResponseMessage = ExecuteRequest(activityPackage))
                {
                    responseData = ProcessResponse(httpResponseMessage, activityPackage);
                }
            }
            catch (WebException we) { responseData = ProcessException(we, activityPackage); }
            catch (Exception ex) { responseData = ProcessException(ex, activityPackage); }

            return responseData;
        }

        private HttpResponseMessage ExecuteRequest(ActivityPackage activityPackage)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = Timeout;
                httpClient.DefaultRequestHeaders.Add("Client-SDK", activityPackage.ClientSdk);
                httpClient.DefaultRequestHeaders.Add("User-Agent", activityPackage.UserAgent);

                var url = Util.BaseUrl + activityPackage.Path;

                var parameters = new FormUrlEncodedContent(activityPackage.Parameters);

                return httpClient.PostAsync(url, parameters).Result;
            }
        }

        private ResponseData ProcessResponse(HttpResponseMessage httpResponseMessage, ActivityPackage activityPackage)
        {
            ResponseData responseData = new ResponseData();

            using (var content = httpResponseMessage.Content)
            {
                var responseString = content.ReadAsStringAsync().Result;
                responseData.SetResponseData(responseString);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    responseData.Success = true;
                    PackageHandler.FinishedTrackingActivity(activityPackage, responseData);

                    Logger.Info("{0}", activityPackage.SuccessMessage());
                }
                else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                    || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         // 501
                {
                    PackageHandler.FinishedTrackingActivity(activityPackage, responseData);

                    Logger.Error("{0}. ({1}, {2}).",
                        activityPackage.FailureMessage(),
                        responseString.TrimEnd('\r', '\n'),
                        (int)httpResponseMessage.StatusCode);
                }
                else
                {
                    responseData.SetResponseData(responseString);
                    responseData.WillRetry = true;
                    PackageHandler.FinishedTrackingActivity(activityPackage, responseData);

                    Logger.Error("{0}. ({1}). Will retry later.",
                        activityPackage.FailureMessage(),
                        (int)httpResponseMessage.StatusCode);
                }
            }

            return responseData;
        }

        private ResponseData ProcessException(WebException webException, ActivityPackage activityPackage)
        {
            ResponseData responseData = new ResponseData();

            using (var response = webException.Response as HttpWebResponse)
            using (var streamResponse = response.GetResponseStream())
            using (var streamReader = new StreamReader(streamResponse))
            {
                var responseString = streamReader.ReadToEnd();

                responseData.SetResponseData(responseString);
                responseData.WillRetry = true;
                PackageHandler.FinishedTrackingActivity(activityPackage, responseData);

                Logger.Error("{0}. ({1}, {2}). Will retry later.",
                    activityPackage.FailureMessage(),
                    responseString.Trim(),
                    (int)response.StatusCode);
            }

            return responseData;
        }

        private ResponseData ProcessException(Exception exception, ActivityPackage activityPackage)
        {
            ResponseData responseData = new ResponseData();

            responseData.SetResponseError(exception.Message);
            responseData.WillRetry = true;
            PackageHandler.FinishedTrackingActivity(activityPackage, responseData);

            Logger.Error("{0}. ({1}). Will retry later", activityPackage.FailureMessage(), exception.Message);

            return responseData;
        }

        private void PackageSent(Task<ResponseData> SendTask)
        {
            // status needs to be tested before reading the result.
            // section "Passing data to a continuation" of
            // http://msdn.microsoft.com/en-us/library/ee372288(v=vs.110).aspx
            var successRunning =
                !SendTask.IsFaulted
                && !SendTask.IsCanceled;

            if (successRunning && !SendTask.Result.WillRetry)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }
    }
}