using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

        internal string SuccessMessage { get; set; }
        internal string FailureMessage { get; set; }
        internal bool IsBusy { get { return Worker.IsBusy; } }

        private class RequestState
        {
            public AIActivityPackage package;
            public WebRequest request;
        }

        internal AIRequestHandler(AIPackageHandler packageHandler)
        {
            AllDone = new ManualResetEvent(false);

            Worker = new BackgroundWorker();
            Worker.DoWork += new DoWorkEventHandler(WorkSendPackage);
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

        private void WorkSendPackage(object sender, DoWorkEventArgs e)
        {
            var package = e.Argument as AIActivityPackage;

            var url = Util.BaseUrl + package.Path;

            var request = WebRequest.Create(url);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.Headers["Client-SDK"] = package.ClientSdk;
            request.Headers["User-Agent"] = package.UserAgent;
            //TODO include the request timeout

            ParamString = Util.GetStringEncodedParameters(package.Parameters);

            // start the asynchronous operation
            request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback),
                new RequestState { package = package, request = request });

            // Keep the main thread from continuing while the asynchronous 
            // operation completes. 
            AllDone.WaitOne();
        }

        //TODO what is necessary to comunicate to the package handler?
        private void WorkerSendCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //if (e.Cancelled == true)
            //{
            //}
            //else 
            if (!(e.Error == null))
            {
                PackageHandler.CloseFirstPackage();
            }
            else
            {
                PackageHandler.SendNextPackage();
            }
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            // Recover the request object
            var requestState = asynchronousResult.AsyncState as RequestState;
            var request = requestState.request as HttpWebRequest;
            
            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // Convert the string into a byte array
            byte[] byteArray = Encoding.UTF8.GetBytes(ParamString);

            // Write to the request stream
            postStream.Write(byteArray, 0, ParamString.Length);
            postStream.Dispose();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), requestState);
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            // Recover the request object
            RequestState requestState = null;
            HttpWebRequest request = null;

            try
            {
                requestState = asynchronousResult.AsyncState as RequestState;
                request = requestState.request as HttpWebRequest;

                // End the operation
                using (var response = request.EndGetResponse(asynchronousResult) as HttpWebResponse)
                {
                    var responseString = readResponse(response);
                    if (responseString == "OK")
                    {
                        AILogger.Info("{0}", requestState.package.SuccessMessage());
                    } else {
                        AILogger.Error("{0}. ({1})", requestState.package.FailureMessage(), responseString.Trim());
                    }
                }
            }
            catch (WebException e)
            {
                using (var response = e.Response as HttpWebResponse)
                {
                    var responseString = readResponse(response);
                    AILogger.Error("{0}. ({1})", requestState.package.FailureMessage(), responseString.Trim());
                }

                //TODO put here backgroundWorker cancelation 
                //  if we need to comunicate that an error occurred to Package Handler
            }
            catch (Exception e)
            {
                AILogger.Error("{0}. ({1}) Will retry later", requestState.package.FailureMessage(), e.Message);
                throw;
            }
            finally
            {
                AllDone.Set();
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
    }
}
