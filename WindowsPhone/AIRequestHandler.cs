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
        private string ParamString;
        private ManualResetEvent AllDone;
        private AIPackageHandler PackageHandler;
        private static readonly TimeSpan Timeout = new TimeSpan(0, 1, 0);

        internal string SuccessMessage { get; set; }
        internal string FailureMessage { get; set; }
        internal bool IsBusy { get { return Worker.IsBusy; } }

        private class RequestState
        {
            public AIActivityPackage package;
            public WebRequest request;
            public DoWorkEventArgs eventArgs;
        }

        internal AIRequestHandler(AIPackageHandler packageHandler)
        {
            AllDone = new ManualResetEvent(false);

            Worker = new BackgroundWorker();
            Worker.WorkerSupportsCancellation = true;
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

        private void SendPackageHttpClient(object sender, DoWorkEventArgs eventArgs)
        {
            SendPackageHttpClientAsync(eventArgs).Wait();
        }

        private async Task SendPackageHttpClientAsync(DoWorkEventArgs eventArgs)
        {
            var package = eventArgs.Argument as AIActivityPackage;
            var url = Util.BaseUrl + "x" + package.Path;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = Timeout;
                    httpClient.DefaultRequestHeaders.Add("Client-SDK", package.ClientSdk);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", package.UserAgent);

                    using (var httpResponseMessage = await httpClient.PostAsync(
                        url, new FormUrlEncodedContent(package.Parameters)))
                    using (var content = httpResponseMessage.Content)
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            AILogger.Info("{0}", package.SuccessMessage());
                        }
                        else
                        {
                            AILogger.Error("{0}, Status not OK: {1}, Response: {2}", package.FailureMessage()
                                                                        , httpResponseMessage.StatusCode
                                                                        , await content.ReadAsStringAsync());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AILogger.Error("{0}. ({1}) Will retry later", package.FailureMessage(), ex.Message);
                eventArgs.Cancel = true;
            }
        }

        private void SendPackageWebRequest(object sender, DoWorkEventArgs eventArgs)
        {
            var package = eventArgs.Argument as AIActivityPackage;

            var url = Util.BaseUrl + package.Path;

            var request = WebRequest.Create(url);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.Headers["Client-SDK"] = package.ClientSdk;
            request.Headers["User-Agent"] = package.UserAgent;

            var sendPackageTask = SendRequestPackageAsync(request, package, eventArgs);

            var completedTask = TaskEx.WhenAny(sendPackageTask, Task.Delay(Timeout));
            completedTask.Wait();

            if (sendPackageTask.Status == TaskStatus.RanToCompletion
                || sendPackageTask.Status == TaskStatus.Canceled
                || sendPackageTask.Status == TaskStatus.Faulted)
                return;
            else
            {
                request.Abort();
                eventArgs.Cancel = true;
            }
        }

        private async Task SendRequestPackageAsync(WebRequest request, AIActivityPackage package, DoWorkEventArgs eventArgs)
        {
            try
            {
                var postStreamTask = request.GetRequestStreamAsync();

                var paramString = Util.GetStringEncodedParameters(package.Parameters);

                // Convert the string into a byte array
                byte[] byteArray = Encoding.UTF8.GetBytes(paramString);

                var postStream = await postStreamTask;
                // Write to the request stream
                postStream.Write(byteArray, 0, ParamString.Length);
                postStream.Dispose();

                using (var webResponse = await request.GetResponseAsync() as HttpWebResponse)
                {
                    var responseString = readResponse(webResponse);
                    if (responseString == "OK")
                    {
                        AILogger.Info("{0}", package.SuccessMessage());
                    }
                    else
                    {
                        AILogger.Error("Status not OK {0}. ({1})", package.FailureMessage(), responseString.Trim());
                    }
                }
            }
            catch (WebException we)
            {
                using (var response = we.Response as HttpWebResponse)
                {
                    var responseString = readResponse(response);
                    AILogger.Error("WebException {0}. ({1}) Will retry later", package.FailureMessage(), responseString.Trim());
                }
                eventArgs.Cancel = true;
            }
            catch (Exception ex)
            {
                AILogger.Error("{0}. ({1}) Will retry later", package.FailureMessage(), ex.Message);
                eventArgs.Cancel = true;
            }
        }
        
        private static string readResponse(HttpWebResponse response)
        {
            var streamResponse = response.GetResponseStream();
            var streamReader = new StreamReader(streamResponse);
            var responseString = streamReader.ReadToEnd().Trim();

            // Close the stream object
            streamReader.Close();
            streamResponse.Close();

            return responseString;
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
