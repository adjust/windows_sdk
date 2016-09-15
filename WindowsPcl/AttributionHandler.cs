using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace AdjustSdk.Pcl
{
    public class AttributionHandler : IAttributionHandler
    {
        private ILogger Logger { get; set; }

        private ActionQueue InternalQueue { get; set; }

        private IActivityHandler ActivityHandler { get; set; }

        private TimerOnce Timer { get; set; }

        private ActivityPackage AttributionPackage { get; set; }

        public bool Paused { private get; set; }

        private bool HasDelegate { get; set; }

        private HttpClient HttpClient { get; set; }

        public AttributionHandler(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate)
        {
            Logger = AdjustFactory.Logger;

            InternalQueue = new ActionQueue("adjust.AttributionHandler");

            Init(activityHandler: activityHandler,
                attributionPackage: attributionPackage,
                startPaused: startPaused,
                hasDelegate: hasDelegate);

            Timer = new TimerOnce(actionQueue: InternalQueue, action: GetAttributionI);
        }

        public void Init(IActivityHandler activityHandler, ActivityPackage attributionPackage, bool startPaused, bool hasDelegate)
        {
            ActivityHandler = activityHandler;
            AttributionPackage = attributionPackage;
            Paused = startPaused;
            HasDelegate = hasDelegate;
        }

        public void CheckAttribution(Dictionary<string, string> jsonDict)
        {
            InternalQueue.Enqueue(() => CheckAttributionI(jsonDict));
        }

        public void AskAttribution()
        {
            AskAttribution(0);
        }

        public void PauseSending()
        {
            Paused = true;
        }

        public void ResumeSending()
        {
            Paused = false;
        }

        private void AskAttribution(int milliSecondsDelay)
        {
            // don't reset if new time is shorter than the last one
            if (Timer.FireIn.Milliseconds > milliSecondsDelay) { return; }

            if (milliSecondsDelay > 0)
            {
                Logger.Debug("Waiting to query attribution in {0} milliseconds", milliSecondsDelay);
            }

            // set the new time the timer will fire in
            Timer.StartIn(milliSecondsDelay);
        }

        private void CheckAttributionI(Dictionary<string, string> jsonDict)
        {
            if (jsonDict == null) { return; }

            var attribution = DeserializeAttribution(jsonDict);
            var askIn = DeserializeAskIn(jsonDict);

            // without ask_in attribute
            if (!askIn.HasValue)
            {
                ActivityHandler.UpdateAttribution(attribution);

                ActivityHandler.SetAskingAttribution(false);

                return;
            }
            ActivityHandler.SetAskingAttribution(true);

            AskAttribution(askIn.Value);
        }

        private void GetAttributionI()
        {
            if (!HasDelegate) { return; }

            if (Paused)
            {
                Logger.Debug("Attribution handler is paused");
                return;
            }

            Logger.Verbose("{0}", AttributionPackage.GetExtendedString());

            HttpResponseMessage httpResponseMessage;
            try
            {
                var httpClient = GetHttpClient(AttributionPackage);
                var attribution = GetAttributionUrl();
                httpResponseMessage = httpClient.GetAsync(attribution).Result;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get attribution ({0})", Util.ExtractExceptionMessage(ex));
                return;
            }

            var jsonDic = Util.ParseJsonResponse(httpResponseMessage);

            CheckAttributionI(jsonDic);
        }

        private HttpClient GetHttpClient(ActivityPackage activityPackage)
        {
            if (HttpClient == null)
            {
                HttpClient = Util.BuildHttpClient(activityPackage.ClientSdk);
            }
            return HttpClient;
        }

        private string GetAttributionUrl()
        {
            var queryList = new List<string>(AttributionPackage.Parameters.Count);

            foreach (var entry in AttributionPackage.Parameters)
            {
                if (entry.Key == null) { continue; }
                var keyEscaped = Uri.EscapeDataString(entry.Key);

                if (entry.Value == null) { continue; }
                var valueEscaped = Uri.EscapeDataString(entry.Value);

                var queryParameter = string.Format("{0}={1}", keyEscaped, valueEscaped);

                queryList.Add(queryParameter);
            }

            var sNow = Uri.EscapeDataString(Util.DateFormat(DateTime.Now));
            var sentAtParameter = "sent_at=" + sNow;
            queryList.Add(sentAtParameter);

            var query = string.Join("&", queryList);

            var uriBuilder = new UriBuilder(Util.BaseUrl);
            uriBuilder.Path = AttributionPackage.Path;
            uriBuilder.Query = query;

            return uriBuilder.Uri.ToString();
        }

        private AdjustAttribution DeserializeAttribution(Dictionary<string, string> jsonDict)
        {
            string attributionString = Util.GetDictionaryValue(jsonDict, "attribution");
            if (attributionString == null) { return null; }

            return AdjustAttribution.FromJsonString(attributionString);
        }

        private int? DeserializeAskIn(Dictionary<string, string> jsonDict)
        {
            var askInString = Util.GetDictionaryValue(jsonDict, "ask_in");
            if (askInString == null) { return null; }

            int askIn;
            if (!int.TryParse(askInString, out askIn)) { return null; }

            return askIn;
        }
    }
}