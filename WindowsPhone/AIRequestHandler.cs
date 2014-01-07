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

namespace adeven.AdjustIo
{
    class AIRequestHandler
    {
        private BackgroundWorker Worker;
        private AIPackageHandler PackageHandler;
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);       // 1 minute

        internal bool IsBusy { get { return Worker.IsBusy; } }

        internal AIRequestHandler(AIPackageHandler packageHandler)
        {
            Worker = new BackgroundWorker();
            Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += new DoWorkEventHandler(SendPackageHttpClient);
            Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerSendCompleted);

            PackageHandler = packageHandler;
        }

        internal void SendPackage(AIActivityPackage package)
        {
            if (Worker.IsBusy != true)
            {
                Worker.RunWorkerAsync(package);
            }
        }

        private void SendPackageHttpClient(object sender, DoWorkEventArgs eventArgs)
        {
            SendPackageHttpClientAsync(eventArgs).Wait();
        }

        private async Task SendPackageHttpClientAsync(DoWorkEventArgs eventArgs)
        {
            var package = eventArgs.Argument as AIActivityPackage;
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout;
                    httpClient.DefaultRequestHeaders.Add("Client-SDK", package.ClientSdk);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", package.UserAgent);

                    var url = Util.BaseUrl + package.Path;
                    using (var httpResponseMessage = await httpClient.PostAsync(
                        url, new FormUrlEncodedContent(package.Parameters)))
                    using (var content = httpResponseMessage.Content)
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            AILogger.Info("{0}", package.SuccessMessage());
                        }
                        else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError   //500
                            || httpResponseMessage.StatusCode == HttpStatusCode.NotImplemented)         //501
                        {
                            AILogger.Error("{0}. Status {1} and response: {2}."
                                                                        , package.FailureMessage()
                                                                        , httpResponseMessage.StatusCode
                                                                        , await content.ReadAsStringAsync());
                        }
                        else
                        {
                            AILogger.Error("{0}. Status {1} and response: {2}. Will try again later."
                                                                        , package.FailureMessage()
                                                                        , httpResponseMessage.StatusCode
                                                                        , await content.ReadAsStringAsync());
                            eventArgs.Cancel = true;
                        }
                    }
                }
            }
            catch (WebException we)
            {
                using (var response = we.Response as HttpWebResponse)
                {
                    AILogger.Error("{0}. WebException with Status {1} and response: '{2}'. Will retry later"
                                    , package.FailureMessage()
                                    , response.StatusCode
                                    , readResponse(response));
                }
                eventArgs.Cancel = true;
            }
            catch (Exception ex)
            {
                AILogger.Error("{0}. Exception: {1}. Will retry later", package.FailureMessage(), ex.Message);
                eventArgs.Cancel = true;
            }
        }
        
        private static string readResponse(HttpWebResponse response)
        {
            using (var streamResponse = response.GetResponseStream())
            using (var streamReader = new StreamReader(streamResponse))
            {
                return streamReader.ReadToEnd().Trim();
            }
        }

        private void WorkerSendCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                PackageHandler.CloseFirstPackage();
            }
            else 
            if (!(e.Error == null))
            {
                PackageHandler.CloseFirstPackage();
            }
            else
            {
                PackageHandler.SendNextPackage();
            }
        }
    }
}
