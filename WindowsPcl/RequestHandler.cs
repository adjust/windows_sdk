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
        private ILogger _Logger = AdjustFactory.Logger;
        private IPackageHandler _PackageHandler;

        public RequestHandler(IPackageHandler packageHandler)
        {
            Init(packageHandler);
        }

        public void Init(IPackageHandler packageHandler)
        {
            _PackageHandler = packageHandler;
        }

        public void SendPackage(ActivityPackage activityPackage)
        {
            Task.Run(() => SendI(activityPackage))
                // continuation used to prevent unhandled exceptions in SendI
                // not signaling the WaitHandle in PackageHandler and preventing deadlocks
                .ContinueWith((responseData) => PackageSent(responseData, activityPackage));
        }

        private ResponseData SendI(ActivityPackage activityPackage)
        {
            ResponseData responseData = null;

            try
            {
                using (var httpResponseMessage = Util.SendPostRequest(activityPackage))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage);
                }
                //PackageSent(responseData, activityPackage);
            }
            catch (HttpRequestException hre)
            {
                var we = hre.InnerException as WebException;
                if (we == null)
                {
                    responseData = ProcessException(hre);
                } else
                {
                    responseData = ProcessWebException(we);
                }
            }
            catch (WebException we) { responseData = ProcessWebException(we); }
            catch (Exception ex) { responseData = ProcessException(ex); }

            return responseData;
        }

        private ResponseData ProcessWebException(WebException webException)
        {
            using (var response = webException.Response as HttpWebResponse)
            {
                return Util.ProcessResponse(response);
                //PackageSent(responseData, activityPackage, webException);
            }
        }

        private ResponseData ProcessException(Exception exception)
        {
            return new ResponseData()
            {
                Success = false,
                WillRetry = true,
                Exception = exception,
            };
        }

        private void PackageSent(Task<ResponseData> responseDataTask, ActivityPackage activityPackage)
        {
            // status needs to be tested before reading the result.
            // section "Passing data to a continuation" of
            // http://msdn.microsoft.com/en-us/library/ee372288(v=vs.110).aspx
            if (responseDataTask.Status != TaskStatus.RanToCompletion)
            {
                var responseDataFaulted = ProcessException(responseDataTask.Exception);
                LogSendErrorI(responseDataFaulted, activityPackage);
                _PackageHandler.CloseFirstPackage(responseDataFaulted, activityPackage);
                return;
            }

            var responseData = responseDataTask.Result;

            if (!responseData.Success)
            {
                LogSendErrorI(responseData, activityPackage);
            }

            if (responseData.WillRetry)
            {
                _PackageHandler.CloseFirstPackage(responseData, activityPackage);
            }
            else
            {
                _PackageHandler.SendNextPackage(responseData);
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

            _Logger.Error("{0}", errorMessagBuilder.ToString());
        }
    }
}