using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class AttributionHandler : IAttributionHandler
    {
        private ILogger _Logger = AdjustFactory.Logger;
        private ActionQueue _ActionQueue = new ActionQueue("adjust.AttributionHandler");

        private IActivityHandler _ActivityHandler;
        private TimerOnce _Timer;
        private ActivityPackage _AttributionPackage;
        private bool _Paused;
        private string _UrlQuery;

        public AttributionHandler(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused)
        {
            Init(activityHandler: activityHandler,
                attributionPackage: attributionPackage,
                startPaused: startPaused);

            _UrlQuery = BuildUrlQuery();

            _Timer = new TimerOnce(actionQueue: _ActionQueue, action: SendAttributionRequestI);
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused)
        {
            _ActivityHandler = activityHandler;
            _AttributionPackage = attributionPackage;
            _Paused = startPaused;
        }

        public void CheckSessionResponse(SessionResponseData responseData)
        {
            _ActionQueue.Enqueue(() => CheckSessionResponseI(responseData));
        }

        public void GetAttribution()
        {
            _ActionQueue.Enqueue(() => GetAttributionI(TimeSpan.Zero));
        }

        public void PauseSending()
        {
            _Paused = true;
        }

        public void ResumeSending()
        {
            _Paused = false;
        }

        private void GetAttributionI(TimeSpan askIn)
        {
            // don't reset if new time is shorter than the last one
            if (_Timer.FireIn > askIn) { return; }

            if (askIn.Milliseconds > 0)
            {
                _Logger.Debug("Waiting to query attribution in {0} milliseconds", askIn.Milliseconds);
            }

            // set the new time the timer will fire in
            _Timer.StartIn(askIn);
        }

        private void CheckAttributionI(ResponseData responseData)
        {
            if (responseData.JsonResponse == null) { return; }

            var askInMilliseconds = Util.GetDictionaryInt(responseData.JsonResponse, "ask_in");

            // with ask_in
            if (askInMilliseconds.HasValue)
            {
                _ActivityHandler.SetAskingAttribution(true);

                GetAttributionI(TimeSpan.FromMilliseconds(askInMilliseconds.Value));
                return;
            }

            // without ask_in
            _ActivityHandler.SetAskingAttribution(false);

            var attributionString = Util.GetDictionaryString(responseData.JsonResponse, "attribution");
            responseData.Attribution = AdjustAttribution.FromJsonString(attributionString, responseData.Adid);
        }

        private void CheckSessionResponseI(SessionResponseData sessionResponseData)
        {
            CheckAttributionI(sessionResponseData);

            _ActivityHandler.LaunchSessionResponseTasks(sessionResponseData);
        }

        private void CheckAttributionResponseI(AttributionResponseData attributionResponseData)
        {
            CheckAttributionI(attributionResponseData);

            CheckDeeplink(attributionResponseData);

            _ActivityHandler.LaunchAttributionResponseTasks(attributionResponseData);
        }

        private void CheckDeeplink(AttributionResponseData attributionResponseData)
        {
            if (attributionResponseData.Attribution?.Json == null) { return; }

            var deeplink = Util.GetDictionaryString(attributionResponseData.Attribution.Json, "deeplink");
            
            if (deeplink == null) { return; }

            if (!Uri.IsWellFormedUriString(deeplink, UriKind.Absolute))
            {
                _Logger.Error("Malformed deffered deeplink '{0}'", deeplink);
                return;
            }

            attributionResponseData.Deeplink = new Uri(deeplink);
        }

        private void SendAttributionRequestI()
        {
            if (_Paused)
            {
                _Logger.Debug("Attribution handler is paused");
                return;
            }

            _Logger.Verbose("{0}", _AttributionPackage.GetExtendedString());

            try
            {
                ResponseData responseData;
                using (var httpResponseMessage = Util.SendGetRequest(_AttributionPackage, _UrlQuery))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage, _AttributionPackage);
                }
                if (responseData is AttributionResponseData)
                {
                    CheckAttributionResponseI(responseData as AttributionResponseData);
                }
            }
            catch (Exception ex)
            {
                _Logger.Error("Failed to get attribution ({0})", Util.ExtractExceptionMessage(ex));
                return;
            }
        }

        private string BuildUrlQuery()
        {
            var queryList = new List<string>(_AttributionPackage.Parameters.Count);

            foreach (var entry in _AttributionPackage.Parameters)
            {
                if (entry.Key == null) { continue; }
                var keyEscaped = Uri.EscapeDataString(entry.Key);

                if (entry.Value == null) { continue; }
                var valueEscaped = Uri.EscapeDataString(entry.Value);

                var queryParameter = string.Format("{0}={1}", keyEscaped, valueEscaped);

                queryList.Add(queryParameter);
            }

            var query = string.Join("&", queryList);

            return query;
        }
    }
}