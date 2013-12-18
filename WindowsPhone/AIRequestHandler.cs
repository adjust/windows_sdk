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
        public string SuccessMessage { get; set; }
        public string FailureMessage { get; set; }

        private BackgroundWorker backgroundWorker;
        private string paramString;
        private ManualResetEvent allDone;

        public AIRequestHandler()
        {
            allDone = new ManualResetEvent(false);

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(WorkSendPackage);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerSendCompleted);

        }
        internal void SendPackage(AIActivityPackage package)
        {
            if (backgroundWorker.IsBusy != true)
            {
                backgroundWorker.RunWorkerAsync(package);
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

            paramString = Util.GetStringEncodedParameters(package.Parameters);

            // start the asynchronous operation
            request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);

            // Keep the main thread from continuing while the asynchronous 
            // operation completes. 
            allDone.WaitOne();
        }

        //TODO what is necessary to comunicate to the package handler?
        private void WorkerSendCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
            }

            else if (!(e.Error == null))
            {
            }

            else
            {
            }
        }


        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            // Recover the request object
            var request = asynchronousResult.AsyncState as HttpWebRequest;
            
            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // Convert the string into a byte array
            byte[] byteArray = Encoding.UTF8.GetBytes(paramString);

            // Write to the request stream
            postStream.Write(byteArray, 0, paramString.Length);
            postStream.Dispose();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                var request = asynchronousResult.AsyncState as HttpWebRequest;
                
                // End the operation
                var response = request.EndGetResponse(asynchronousResult) as HttpWebResponse;
                var responseString = readResponse(response);

                response.Close();
                //Debug.WriteLine("[{0}] {1}", Util.LogTag, SuccessMessage);
            }
                //TODO ask welle why should we try again instead of dropping
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;
                var responseString = readResponse(response);
                response.Close();
                //Debug.WriteLine("[{0}] {1} ({2})", Util.LogTag, FailureMessage, responseString);
                //TODO put here backgroundWorker cancelation 
                //  if we need to comunicate that an error occurred to Package Handler
            }
            //catch (ArgumentException e)
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            finally
            {
                allDone.Set();
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
