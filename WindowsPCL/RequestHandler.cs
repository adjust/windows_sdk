using AdjustSdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdjustSdk.PCL
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

        private ResponseData SendInternal(ActivityPackage package)
        {
            ResponseData responseData = new ResponseData();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout;
                    httpClient.DefaultRequestHeaders.Add("Client-SDK", package.ClientSdk);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", package.UserAgent);

                    var url = Util.BaseUrl + package.Path;

                    var parameters = new FormUrlEncodedContent(package.Parameters);

                    using (var httpResponseMessage = httpClient.PostAsync(url, parameters).Result)
                    using (var content = httpResponseMessage.Content)
                    {
                        var responseString = content.ReadAsStringAsync();
                        Util.InjectResponseData(responseData, responseString.Result);

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            Logger.Info("{0}", package.SuccessMessage());

                            responseData.Success = true;
                        }
                        else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                            || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         // 501
                        {
                            Logger.Error("{0}. ({1}, {2}).",
                                package.FailureMessage(),
                                responseString.Result.TrimEnd('\r', '\n'),
                                (int)httpResponseMessage.StatusCode);
                        }
                        else
                        {
                            Logger.Error("{0}. ({1}). Will retry later.",
                                package.FailureMessage(),
                                (int)httpResponseMessage.StatusCode);

                            responseData.WillRetry = true;
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
                    Logger.Error("{0}. ({1}, {2}). Will retry later.",
                        package.FailureMessage(),
                        responseString.Trim(),
                        (int)response.StatusCode);

                    Util.InjectResponseError(responseData, responseString);
                    responseData.WillRetry = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("{0}. ({1}). Will retry later", package.FailureMessage(), ex.Message);

                Util.InjectResponseError(responseData, ex.Message);
                responseData.WillRetry = true;
            }

            responseData.Kind = package.Kind;
            responseData.ActivityKindString = Util.ActivityKindToString(responseData.Kind);

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