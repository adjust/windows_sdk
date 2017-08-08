using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class RequestHandler : IRequestHandler
    {
        private readonly ILogger _logger = AdjustFactory.Logger;

        private Action<ResponseData> _successCallback;
        private Action<ResponseData, ActivityPackage> _failureCallback;
        
        public RequestHandler(Action<ResponseData> successCallbac, Action<ResponseData, ActivityPackage> failureCallback)
        {
            Init(successCallbac, failureCallback);
        }

        public void Init(Action<ResponseData> successCallbac, Action<ResponseData, ActivityPackage> failureCallback)
        {
            _successCallback = successCallbac;
            _failureCallback = failureCallback;
        }

        public void SendPackage(ActivityPackage activityPackage, int queueSize)
        {
            Task.Run(() => SendI(activityPackage, queueSize))
                // continuation used to prevent unhandled exceptions in SendI
                .ContinueWith((responseData) => PackageSent(responseData, activityPackage),
                    TaskContinuationOptions.ExecuteSynchronously); // execute on the same thread of SendI
        }

        public void SendPackageSync(ActivityPackage activityPackage, int queueSize)
        {
            var sendTask = new Task<ResponseData>(() => SendI(activityPackage, queueSize));

            // continuation used to prevent unhandled exceptions in SendI
            sendTask.ContinueWith((responseData) => {
                PackageSent(responseData, activityPackage);
            }, TaskContinuationOptions.ExecuteSynchronously); // execute on the same thread of SendI ->

            sendTask.RunSynchronously();
        }

        public void Teardown()
        {
            _successCallback = null;
            _failureCallback = null;
        }

        private ResponseData SendI(ActivityPackage activityPackage, int queueSize)
        {
            ResponseData responseData;
            try
            {
                using (var httpResponseMessage = Util.SendPostRequest(activityPackage, queueSize))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage, activityPackage);
                }
            }
            catch (HttpRequestException hre)
            {
                var we = hre.InnerException as WebException;
                responseData = we == null ? 
                    ProcessException(hre, activityPackage) : 
                    ProcessWebException(we, activityPackage);
            }
            catch (WebException we) { responseData = ProcessWebException(we, activityPackage); }
            catch (Exception ex) { responseData = ProcessException(ex, activityPackage); }

            return responseData;
        }

        private ResponseData ProcessWebException(WebException webException, ActivityPackage activityPackage)
        {
            using (var response = webException.Response as HttpWebResponse)
            {
                return Util.ProcessResponse(response, activityPackage);
            }
        }

        private ResponseData ProcessException(Exception exception, ActivityPackage activityPackage)
        {
            var responseData = ResponseData.BuildResponseData(activityPackage);
            responseData.Success = false;
            responseData.WillRetry = true;
            responseData.Exception = exception;

            return responseData;
        }

        private void PackageSent(Task<ResponseData> responseDataTask, ActivityPackage activityPackage)
        {
            // status needs to be tested before reading the result.
            // section "Passing data to a continuation" of
            // http://msdn.microsoft.com/en-us/library/ee372288(v=vs.110).aspx
            if (responseDataTask.Status != TaskStatus.RanToCompletion)
            {
                var responseDataFaulted = ProcessException(responseDataTask.Exception, activityPackage);
                LogSendErrorI(responseDataFaulted, activityPackage);
                _failureCallback?.Invoke(responseDataFaulted, activityPackage);
                return;
            }

            var responseData = responseDataTask.Result;

            if (!responseData.Success)
            {
                LogSendErrorI(responseData, activityPackage);
            }

            if (responseData.WillRetry)
            {
                _failureCallback?.Invoke(responseData, activityPackage);
            }
            else
            {
                _successCallback?.Invoke(responseData);
            }
        }

        private void LogSendErrorI(ResponseData responseData, ActivityPackage activityPackage)
        {
            var errorMessagBuilder = new StringBuilder(activityPackage.FailureMessage());

            if (responseData.Exception != null)
            {
                errorMessagBuilder.AppendFormat(" ({0})", Util.ExtractExceptionMessage(responseData.Exception));
            }
            if (responseData.StatusCode.HasValue)
            {
                errorMessagBuilder.AppendFormat(" (Status code: {0})", responseData.StatusCode.Value);
            }
            if (responseData.WillRetry)
            {
                errorMessagBuilder.Append(" Will retry later");
            }

            _logger.Error("{0}", errorMessagBuilder.ToString());
        }
    }
}