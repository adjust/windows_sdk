using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

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
        private bool _HasDelegate;
        private HttpClient _HttpClient;
        private string _UrlQuery;

        public AttributionHandler(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate)
        {
            Init(activityHandler: activityHandler,
                attributionPackage: attributionPackage,
                startPaused: startPaused,
                hasDelegate: hasDelegate);

            _UrlQuery = BuildUrlQuery();

            _Timer = new TimerOnce(actionQueue: _ActionQueue, action: GetAttributionI);
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate)
        {
            _ActivityHandler = activityHandler;
            _AttributionPackage = attributionPackage;
            _Paused = startPaused;
            _HasDelegate = hasDelegate;
        }

        public void CheckAttribution(Dictionary<string, string> jsonDict)
        {
            _ActionQueue.Enqueue(() => CheckAttributionI(jsonDict));
        }

        public void AskAttribution()
        {
            AskAttribution(0);
        }

        public void PauseSending()
        {
            _Paused = true;
        }

        public void ResumeSending()
        {
            _Paused = false;
        }

        private void AskAttribution(int milliSecondsDelay)
        {
            // don't reset if new time is shorter than the last one
            if (_Timer.FireIn.Milliseconds > milliSecondsDelay) { return; }

            if (milliSecondsDelay > 0)
            {
                _Logger.Debug("Waiting to query attribution in {0} milliseconds", milliSecondsDelay);
            }

            // set the new time the timer will fire in
            _Timer.StartIn(milliSecondsDelay);
        }

        private void CheckAttributionI(Dictionary<string, string> jsonDict)
        {
            if (jsonDict == null) { return; }

            var attribution = DeserializeAttributionI(jsonDict);
            var askIn = DeserializeAskInI(jsonDict);

            // without ask_in attribute
            if (!askIn.HasValue)
            {
                _ActivityHandler.UpdateAttribution(attribution);

                _ActivityHandler.SetAskingAttribution(false);

                return;
            }
            _ActivityHandler.SetAskingAttribution(true);

            AskAttribution(askIn.Value);
        }

        private void GetAttributionI()
        {
            if (!_HasDelegate) { return; }

            if (_Paused)
            {
                _Logger.Debug("Attribution handler is paused");
                return;
            }

            _Logger.Verbose("{0}", _AttributionPackage.GetExtendedString());

            ResponseData responseData;
            try
            {
                using (var httpResponseMessage = Util.SendGetRequest(_AttributionPackage, _UrlQuery))
                {
                    responseData = Util.ProcessResponse(httpResponseMessage);
                    CheckAttributionI(responseData.JsonResponse);
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

        private AdjustAttribution DeserializeAttributionI(Dictionary<string, string> jsonDict)
        {
            string attributionString = Util.GetDictionaryValue(jsonDict, "attribution");
            if (attributionString == null) { return null; }

            return AdjustAttribution.FromJsonString(attributionString);
        }

        private int? DeserializeAskInI(Dictionary<string, string> jsonDict)
        {
            var askInString = Util.GetDictionaryValue(jsonDict, "ask_in");
            if (askInString == null) { return null; }

            int askIn;
            if (!int.TryParse(askInString, out askIn)) { return null; }

            return askIn;
        }
    }
}