using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public class RequestHandler : IRequestHandler
    {
        private ILogger _logger = AdjustFactory.Logger;
        private WeakReference<IActivityHandler> _activityHandlerWeakReference;

        private Action<ResponseData> _successCallback;
        private Action<ResponseData, ActivityPackage> _failureCallback;
        
        public RequestHandler(
            IActivityHandler activityHandler, 
            Action<ResponseData> successCallbac, 
            Action<ResponseData, ActivityPackage> failureCallback)
        {
            Init(activityHandler, successCallbac, failureCallback);
        }

        public void Init(
            IActivityHandler activityHandler,
            Action<ResponseData> successCallbac, Action<ResponseData,
            ActivityPackage> failureCallback)
        {
            _activityHandlerWeakReference = new WeakReference<IActivityHandler>(activityHandler);
            _successCallback = successCallbac;
            _failureCallback = failureCallback;
        }

        public void SendPackage(ActivityPackage activityPackage, string basePath, int queueSize)
        {
            Task.Run(() => SendI(activityPackage, basePath, queueSize))
                // continuation used to prevent unhandled exceptions in SendI
                .ContinueWith((responseData) => PackageSent(responseData, activityPackage),
                    TaskContinuationOptions.ExecuteSynchronously); // execute on the same thread of SendI
        }

        public void SendPackageSync(ActivityPackage activityPackage, string basePath, int queueSize)
        {
            var sendTask = new Task<ResponseData>(() => SendI(activityPackage, basePath, queueSize));
            // continuation used to prevent unhandled exceptions in SendI
            sendTask.ContinueWith((responseData) => {
                PackageSent(responseData, activityPackage);
            }, TaskContinuationOptions.ExecuteSynchronously); // execute on the same thread of SendI ->

            sendTask.RunSynchronously();
        }

        private ResponseData SendI(ActivityPackage activityPackage, string basePath, int queueSize)
        {
            ResponseData responseData;
            try
            {
                using (var httpResponseMessage = Util.SendPostRequest(activityPackage, basePath, queueSize))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage, activityPackage);

                    if(responseData.TrackingState.HasValue && responseData.TrackingState == TrackingState.OPTED_OUT)
                    {
                        IActivityHandler activityHandler;
                        if (_activityHandlerWeakReference.TryGetTarget(out activityHandler))
                        {
                            // check if any package response contains information that user has opted out
                            // if yes, disable SDK and flush any potentially stored packages that happened afterwards
                            activityHandler.SetTrackingStateOptedOut();
                        }
                    }
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

        public void Teardown()
        {
            _successCallback = null;
            _failureCallback = null;
            _logger = null;
            _activityHandlerWeakReference.SetTarget(null);
            _activityHandlerWeakReference = null;
        }
    }
}