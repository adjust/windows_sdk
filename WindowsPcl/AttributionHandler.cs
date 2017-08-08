using System;
using System.Collections.Generic;
using static AdjustSdk.Pcl.Constants;

namespace AdjustSdk.Pcl
{
    public class AttributionHandler : IAttributionHandler
    {
        private readonly ILogger _logger = AdjustFactory.Logger;
        private readonly ActionQueue _actionQueue = new ActionQueue("adjust.AttributionHandler");

        private IActivityHandler _activityHandler;
        private readonly TimerOnce _timer;
        private ActivityPackage _attributionPackage;
        private bool _paused;
        private readonly string _urlQuery;

        public AttributionHandler(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused)
        {
            Init(activityHandler: activityHandler,
                attributionPackage: attributionPackage,
                startPaused: startPaused);

            _urlQuery = BuildUrlQuery();
            _timer = new TimerOnce(actionQueue: _actionQueue, action: SendAttributionRequestI);
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused)
        {
            _activityHandler = activityHandler;
            _attributionPackage = attributionPackage;
            _paused = startPaused;
        }

        public void Teardown()
        {
            _timer.Teardown();
            _actionQueue.Teardown();
            _attributionPackage = null;
        }

        public void CheckSessionResponse(SessionResponseData responseData)
        {
            _actionQueue.Enqueue(() => CheckSessionResponseI(responseData));
        }

        public void CheckSdkClickResponse(SdkClickResponseData sdkClickResponseData)
        {
            _actionQueue.Enqueue(() => CheckSdkClickResponseI(sdkClickResponseData));
        }

        public void GetAttribution()
        {
            _actionQueue.Enqueue(() => GetAttributionI(TimeSpan.Zero));
        }

        public void PauseSending()
        {
            _paused = true;
        }

        public void ResumeSending()
        {
            _paused = false;
        }

        private void GetAttributionI(TimeSpan askIn)
        {
            // don't reset if new time is shorter than the last one
            if (_timer.FireIn > askIn) { return; }

            if (askIn.Milliseconds > 0)
            {
                _logger.Debug("Waiting to query attribution in {0} milliseconds", askIn.Milliseconds);
            }

            // set the new time the timer will fire in
            _timer.StartIn(askIn);
        }

        private void CheckAttributionI(ResponseData responseData)
        {
            if (responseData.JsonResponse == null) { return; }

            var askInMilliseconds = Util.GetDictionaryInt(responseData.JsonResponse, "ask_in");

            // with ask_in
            if (askInMilliseconds.HasValue)
            {
                _activityHandler.SetAskingAttribution(true);

                GetAttributionI(TimeSpan.FromMilliseconds(askInMilliseconds.Value));
                return;
            }

            // without ask_in
            _activityHandler.SetAskingAttribution(false);

            var attributionString = Util.GetDictionaryString(responseData.JsonResponse, "attribution");
            responseData.Attribution = AdjustAttribution.FromJsonString(attributionString, responseData.Adid);
        }

        private void CheckSessionResponseI(SessionResponseData sessionResponseData)
        {
            CheckAttributionI(sessionResponseData);

            _activityHandler.LaunchSessionResponseTasks(sessionResponseData);
        }

        private void CheckSdkClickResponseI(SdkClickResponseData sdkClickResponseData)
        {
            CheckAttributionI(sdkClickResponseData);

            _activityHandler.LaunchSdkClickResponseTasks(sdkClickResponseData);
        }

        private void CheckAttributionResponseI(AttributionResponseData attributionResponseData)
        {
            CheckAttributionI(attributionResponseData);

            CheckDeeplink(attributionResponseData);

            _activityHandler.LaunchAttributionResponseTasks(attributionResponseData);
        }

        private void CheckDeeplink(AttributionResponseData attributionResponseData)
        {
            if (attributionResponseData.Attribution?.Json == null) { return; }

            var deeplink = Util.GetDictionaryString(attributionResponseData.Attribution.Json, "deeplink");
            
            if (deeplink == null) { return; }

            if (!Uri.IsWellFormedUriString(deeplink, UriKind.Absolute))
            {
                _logger.Error("Malformed deffered deeplink '{0}'", deeplink);
                return;
            }

            attributionResponseData.Deeplink = new Uri(deeplink);
        }

        private void SendAttributionRequestI()
        {
            if (_paused)
            {
                _logger.Debug("Attribution handler is paused");
                return;
            }

            _logger.Verbose("{0}", _attributionPackage.GetExtendedString());

            try
            {
                ResponseData responseData;
                using (var httpResponseMessage = Util.SendGetRequest(_attributionPackage, _urlQuery))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage, _attributionPackage);
                }
                if (responseData is AttributionResponseData)
                {
                    CheckAttributionResponseI(responseData as AttributionResponseData);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get attribution ({0})", Util.ExtractExceptionMessage(ex));
            }
        }

        private string BuildUrlQuery()
        {
            var queryList = new List<string>(_attributionPackage.Parameters.Count);

            foreach (var entry in _attributionPackage.Parameters)
            {
                if (entry.Key == null) { continue; }
                if (entry.Key == APP_SECRET) { continue; }

                var keyEscaped = Uri.EscapeDataString(entry.Key);

                if (entry.Value == null) { continue; }
                var valueEscaped = Uri.EscapeDataString(entry.Value);

                var queryParameter = $"{keyEscaped}={valueEscaped}";

                queryList.Add(queryParameter);
            }

            var query = string.Join("&", queryList);

            return query;
        }
    }
}