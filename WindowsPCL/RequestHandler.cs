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
    internal class RequestHandler
    {
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        private PackageHandler PackageHandler;
        private Action<ResponseData> ResponseDelegate;

        internal RequestHandler(PackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
        }

        internal void SetResponseDelegate(Action<ResponseData> responseDelegate)
        {
            ResponseDelegate = responseDelegate;
        }

        internal void SendPackage(ActivityPackage package)
        {
            Task.Factory.StartNew(() => SendInternal(package))
                // continuation used to prevent unhandled exceptions in SendInternal
                // not signaling the WaitHandle in PackageHandler and preventing deadlocks
                .ContinueWith((responseData) => PackageSent(responseData));
        }

        private ResponseData SendInternal(ActivityPackage activityPackage)
        {
            ResponseData responseData = new ResponseData();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout;
                    httpClient.DefaultRequestHeaders.Add("Client-SDK", activityPackage.ClientSdk);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", activityPackage.UserAgent);

                    var url = Util.BaseUrl + activityPackage.Path;

                    var parameters = new FormUrlEncodedContent(activityPackage.Parameters);

                    using (var httpResponseMessage = httpClient.PostAsync(url, parameters).Result)
                    using (var content = httpResponseMessage.Content)
                    {
                        var responseString = content.ReadAsStringAsync();
                        responseData.SetResponseData(responseString.Result);

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            responseData.Success = true;

                            Logger.Info("{0}", activityPackage.SuccessMessage());
                        }
                        else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                            || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         // 501
                        {

                            Logger.Error("{0}. ({1}, {2}).",
                                activityPackage.FailureMessage(),
                                responseString.Result.TrimEnd('\r', '\n'),
                                (int)httpResponseMessage.StatusCode);
                        }
                        else
                        {
                            responseData.WillRetry = true;

                            Logger.Error("{0}. ({1}). Will retry later.",
                                activityPackage.FailureMessage(),
                                (int)httpResponseMessage.StatusCode);
                        }
                    }
                }
            }
            catch (WebException we)
            {
                using (var response = we.Response as HttpWebResponse)
                using (var streamResponse = response.GetResponseStream())
                using (var streamReader = new StreamReader(streamResponse))
                {
                    var responseString = streamReader.ReadToEnd();

                    responseData.SetResponseData(responseString);
                    responseData.WillRetry = true;

                    Logger.Error("{0}. ({1}, {2}). Will retry later.",
                        activityPackage.FailureMessage(),
                        responseString.Trim(),
                        (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                responseData.SetResponseError(ex.Message);
                responseData.WillRetry = true;

                Logger.Error("{0}. ({1}). Will retry later", activityPackage.FailureMessage(), ex.Message);
            }

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

            if (successRunning && ResponseDelegate != null)
                Task.Factory.StartNew(() => ResponseDelegate(SendTask.Result));
            if (successRunning && !SendTask.Result.WillRetry)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }
    }
}