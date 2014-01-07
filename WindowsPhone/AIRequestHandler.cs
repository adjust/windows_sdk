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

            //choose implementation Native (WebRequest) or Library (Microsoft.Net.Http)
            //Worker.DoWork += new DoWorkEventHandler(SendPackageWebRequest);
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

        #region HttpClient
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
        #endregion

        #region WebRequest
        private void SendPackageWebRequest(object sender, DoWorkEventArgs eventArgs)
        {
            SendRequestPackageAsync(eventArgs).Wait();
        }

        private async Task SendRequestPackageAsync(DoWorkEventArgs eventArgs)
        {
            var package = eventArgs.Argument as AIActivityPackage;
            try
            {
                var url = Util.BaseUrl + package.Path;
                //var url = "http://www.google.com:81";
                //"http://www.google.com:81"; //timeout test
                //"invalid site"

                var request = WebRequest.Create(url);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                request.Headers["Client-SDK"] = package.ClientSdk;
                request.Headers["User-Agent"] = package.UserAgent;

                var postStreamTask = request.GetRequestStreamAsync();

                var paramString = Util.GetStringEncodedParameters(package.Parameters);

                // Convert the string into a byte array
                byte[] byteArray = Encoding.UTF8.GetBytes(paramString);

                var postStream = await postStreamTask;
                // Write to the request stream
                postStream.Write(byteArray, 0, paramString.Length);
                postStream.Dispose();

                using (var webResponse = await request.GetResponseAsync() as HttpWebResponse)
                {
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        AILogger.Info("{0}", package.SuccessMessage());
                    }
                    else
                    {
                        AILogger.Error("{0}, Status not OK: {1} and response: '{2}'", package.FailureMessage()
                                                                                , webResponse.StatusCode
                                                                                , readResponse(webResponse));
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
        #endregion

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
