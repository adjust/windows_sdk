using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    internal class RequestHandler
    {
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        private PackageHandler PackageHandler;

        internal RequestHandler(PackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
        }

        internal void SendPackage(ActivityPackage package)
        {
            Task.Factory.StartNew(() => SendInternal(package))
                // continuation used to prevent unhandled exceptions in SendInternal
                // not releasing the WaitHandle in PackageHandler and preventing deadlocks
                .ContinueWith((success) => PackageSent(success));
        }

        private bool SendInternal(ActivityPackage package)
        {
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
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            Logger.Info("{0}", package.SuccessMessage());

                            return true;
                        }
                        else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   // 500
                            || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         // 501
                        {
                            Logger.Error("{0}. ({1}, {2}).",
                                package.FailureMessage(),
                                content.ReadAsStringAsync().Result,
                                httpResponseMessage.StatusCode);

                            return true;
                        }
                        else
                        {
                            Logger.Error("{0}. ({1}, {2}). Will retry later.",
                                package.FailureMessage(),
                                content.ReadAsStringAsync().Result,
                                httpResponseMessage.StatusCode);
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
                    Logger.Error("{0}. ({1}, {2}). Will retry later.",
                        package.FailureMessage(),
                        streamReader.ReadToEnd().Trim(),
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("{0}. ({1}). Will retry later", package.FailureMessage(), ex.Message);
            }

            return false;
        }

        private void PackageSent(Task<bool> SendTask)
        {
            // status needs to be tested before reading the result.
            // section "Passing data to a continuation" of
            // http://msdn.microsoft.com/en-us/library/ee372288(v=vs.110).aspx
            var successRunning =
                !SendTask.IsFaulted
                && !SendTask.IsCanceled
                && SendTask.Result;

            if (successRunning)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }
    }
}