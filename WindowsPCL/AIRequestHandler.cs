using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adeven.AdjustIo.PCL
{
    internal class AIRequestHandler
    {
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        private AIPackageHandler PackageHandler;
        private Task<bool> RunningTask;

        private bool IsRunning { get { return RunningTask.Status == TaskStatus.Running; } }
        
        internal AIRequestHandler(AIPackageHandler packageHandler)
        {
            PackageHandler = packageHandler;
            RunningTask = new Task<bool>(() => SendPackageHttpClient());
        }

        internal bool TrySendFirstPackage()
        {
            lock (RunningTask)
            { 
                if (!IsRunning)
                {
                    RunningTask = Task.Factory.StartNew(() => SendPackageHttpClient());
                    RunningTask.ContinueWith((success) => PackageSent(success));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool SendPackageHttpClient()
        {
            var package = PackageHandler.FirstPackage();
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout;
                    httpClient.DefaultRequestHeaders.Add("Client-SDK", package.ClientSdk);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", package.UserAgent);

                    var url = Util.BaseUrl + package.Path;

                    using (var httpResponseMessage = httpClient.PostAsync(
                        url, new FormUrlEncodedContent(package.Parameters)).Result)
                    using (var content = httpResponseMessage.Content)
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            AILogger.Info("{0}", package.SuccessMessage());

                            //PackageHandler.SendNextPackage();
                            return true;
                        }
                        else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   //500
                            || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         //501
                        {
                            AILogger.Error("{0}. Status {1} and response: {2}."
                                                                        , package.FailureMessage()
                                                                        , httpResponseMessage.StatusCode
                                                                        , content.ReadAsStringAsync().Result);

                            //PackageHandler.SendNextPackage();
                            return true;
                        }
                        else
                        {
                            AILogger.Error("{0}. Status {1} and response: {2}. Will try again later."
                                                                        , package.FailureMessage()
                                                                        , httpResponseMessage.StatusCode
                                                                        , content.ReadAsStringAsync().Result);
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
                    AILogger.Error("{0}. WebException with Status {1} and response: '{2}'. Will retry later"
                                    , package.FailureMessage()
                                    , response.StatusCode
                                    , streamReader.ReadToEnd().Trim());
                }
            }
            catch (Exception ex)
            {
                AILogger.Error("{0}. Exception: {1}. Will retry later", package.FailureMessage(), ex.Message);
            }

            //PackageHandler.CloseFirstPackage();
            return false;
        }

        private void PackageSent(Task<bool> WasSuccessful)
        {
            if (WasSuccessful.Result)
                PackageHandler.SendNextPackage();
            else
                PackageHandler.CloseFirstPackage();
        }
    }
}
