using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using AdjustSdk;
using AdjustSdk.Pcl;
using TestLibrary;
using static TestApp.MainPage;

namespace TestApp
{
    public class AdjustCommandExecutor
    {
        private Dictionary<int, AdjustConfig> _savedConfigs = new Dictionary<int, AdjustConfig>();
        private Dictionary<int, AdjustEvent> _savedEvents = new Dictionary<int, AdjustEvent>();

        private TestLibrary.TestLibrary _testLibrary;
        internal string BasePath;
        internal Command Command;

        public void SetTestLibrary(TestLibrary.TestLibrary testLibrary)
        {
            _testLibrary = testLibrary;
        }

        public void ExecuteCommand(Command command)
        {
            this.Command = command;
            try
            {
                Log.Debug(" \t>>> EXECUTING METHOD: {0}.{1} <<<", command.ClassName, command.MethodName);
                
                switch (command.MethodName)
                {
                    case "factory": Factory(); break;
                    case "config": Config(); break;
                    case "start": Start(); break;
                    case "event": Event(); break;
                    case "trackEvent": TrackEvent(); break;
                    case "resume": Resume(); break;
                    case "pause": Pause(); break;
                    case "setEnabled": SetEnabled(); break;
                    case "setReferrer": { Log.Debug("NO REFFERER (setReferrer) IN WIN SDK!"); /*setReferrer();*/ break; }
                    case "setOfflineMode": SetOfflineMode(); break;
                    case "sendFirstPackages": SendFirstPackages(); break;
                    case "addSessionCallbackParameter": AddSessionCallbackParameter(); break;
                    case "addSessionPartnerParameter": AddSessionPartnerParameter(); break;
                    case "removeSessionCallbackParameter": RemoveSessionCallbackParameter(); break;
                    case "removeSessionPartnerParameter": RemoveSessionPartnerParameter(); break;
                    case "resetSessionCallbackParameters": ResetSessionCallbackParameters(); break;
                    case "resetSessionPartnerParameters": ResetSessionPartnerParameters(); break;
                    case "setPushToken": SetPushToken(); break;
                    case "teardown": Teardown(); break;
                    case "openDeeplink": OpenDeeplink(); break;
                    case "sendReferrer": { Log.Debug("NO REFFERER (sendReferrer) IN WIN SDK!"); /*sendReferrer();*/ break; }
                    case "sendTestInfo": SendTestInfo(); break;
                    case "testBegin": TestBegin(); break;
                    case "testEnd": TestEnd(); break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(TAG + "{0} ---- {1}", "executeCommand: failed to parse command. Check commands' syntax", ex.ToString());
            }
        }

        private void SendTestInfo()
        {
            string testInfo = Command.GetFirstParameterValue("testInfo");
            _testLibrary.AddInfoToSend("testInfo", testInfo);
            _testLibrary.SendInfoToServer();
        }

        private void Factory()
        {
            if (Command.ContainsParameter("basePath"))
                BasePath = Command.GetFirstParameterValue("basePath");
            if (Command.ContainsParameter("timerInterval"))
            {
                var timerInterval = long.Parse(Command.GetFirstParameterValue("timerInterval"));
                AdjustFactory.SetTimerInterval(TimeSpan.FromMilliseconds(timerInterval));
            }
            if (Command.ContainsParameter("timerStart"))
            {
                var timerStart = long.Parse(Command.GetFirstParameterValue("timerStart"));
                AdjustFactory.SetTimerStart(TimeSpan.FromMilliseconds(timerStart));
            }
            if (Command.ContainsParameter("sessionInterval"))
            {
                var sessionInterval = long.Parse(Command.GetFirstParameterValue("sessionInterval"));
                AdjustFactory.SetSessionInterval(TimeSpan.FromMilliseconds(sessionInterval));
            }
            if (Command.ContainsParameter("subsessionInterval"))
            {
                var subsessionInterval = long.Parse(Command.GetFirstParameterValue("subsessionInterval"));
                AdjustFactory.SetSubsessionInterval(TimeSpan.FromMilliseconds(subsessionInterval));
            }
        }

        private void Config()
        {
            var configNumber = 0;
            if (Command.ContainsParameter("configName"))
            {
                var configName = Command.GetFirstParameterValue("configName");
                configNumber = int.Parse(configName.Substring(configName.Length - 1));
            }

            AdjustConfig adjustConfig;
            LogLevel? logLevel = null;
            if (Command.ContainsParameter("logLevel"))
            {
                var logLevelString = Command.GetFirstParameterValue("logLevel");
                switch (logLevelString)
                {
                    case "verbose":
                        logLevel = LogLevel.Verbose;
                        break;
                    case "debug":
                        logLevel = LogLevel.Debug;
                        break;
                    case "info":
                        logLevel = LogLevel.Info;
                        break;
                    case "warn":
                        logLevel = LogLevel.Warn;
                        break;
                    case "error":
                        logLevel = LogLevel.Error;
                        break;
                    case "assert":
                        logLevel = LogLevel.Assert;
                        break;
                    case "suppress":
                        logLevel = LogLevel.Suppress;
                        break;
                }

                Log.Debug("TestApp LogLevel = {0}", logLevel);
            }

            if (_savedConfigs.ContainsKey(configNumber))
            {
                adjustConfig = _savedConfigs[configNumber];
            }
            else
            {
                var environment = Command.GetFirstParameterValue("environment");
                var appToken = Command.GetFirstParameterValue("appToken");

                adjustConfig = new AdjustConfig(appToken, environment,
                    msg => Debug.WriteLine(msg),
                    logLevel);

                _savedConfigs.Add(configNumber, adjustConfig);
            }

            if (Command.ContainsParameter("sdkPrefix"))
                adjustConfig.SdkPrefix = Command.GetFirstParameterValue("sdkPrefix");

            if (Command.ContainsParameter("defaultTracker"))
                adjustConfig.DefaultTracker = Command.GetFirstParameterValue("defaultTracker");

            if (Command.ContainsParameter("delayStart"))
            {
                var delayStartStr = Command.GetFirstParameterValue("delayStart");
                var delayStart = int.Parse(delayStartStr);
                Log.Debug("delay start set to: " + delayStart);
                adjustConfig.DelayStart = TimeSpan.FromSeconds(delayStart);
            }

            if (Command.ContainsParameter("deviceKnown"))
            {
                var deviceKnownS = Command.GetFirstParameterValue("deviceKnown");
                var deviceKnown = deviceKnownS.ToLower() == "true";
                adjustConfig.SetDeviceKnown(deviceKnown);
            }

            if (Command.ContainsParameter("eventBufferingEnabled"))
            {
                var eventBufferingEnabledS = Command.GetFirstParameterValue("eventBufferingEnabled");
                var eventBufferingEnabled = eventBufferingEnabledS.ToLower() == "true";
                adjustConfig.EventBufferingEnabled = eventBufferingEnabled;
            }

            if (Command.ContainsParameter("sendInBackground"))
            {
                var sendInBackgroundS = Command.GetFirstParameterValue("sendInBackground");
                var sendInBackground = sendInBackgroundS.ToLower() == "true";
                adjustConfig.SendInBackground = sendInBackground;
            }

            if (Command.ContainsParameter("userAgent"))
            {
                var userAgent = Command.GetFirstParameterValue("userAgent");
                adjustConfig.SetUserAgent(userAgent);
            }

            if (Command.ContainsParameter("attributionCallbackSendAll"))
                adjustConfig.AttributionChanged += attribution =>
                {
                    Log.Debug(TAG, "AttributionChanged, attribution = " + attribution);

                    _testLibrary.AddInfoToSend("trackerToken", attribution.TrackerToken);
                    _testLibrary.AddInfoToSend("trackerName", attribution.TrackerName);
                    _testLibrary.AddInfoToSend("network", attribution.Network);
                    _testLibrary.AddInfoToSend("campaign", attribution.Campaign);
                    _testLibrary.AddInfoToSend("adgroup", attribution.Adgroup);
                    _testLibrary.AddInfoToSend("creative", attribution.Creative);
                    _testLibrary.AddInfoToSend("clickLabel", attribution.ClickLabel);
                    _testLibrary.AddInfoToSend("adid", attribution.Adid);
                    _testLibrary.SendInfoToServer();
                };

            if (Command.ContainsParameter("sessionCallbackSendSuccess"))
                adjustConfig.SesssionTrackingSucceeded += sessionSuccessResponseData =>
                {
                    Log.Debug(TAG,
                        "SesssionTrackingSucceeded, sessionSuccessResponseData = " + sessionSuccessResponseData);

                    _testLibrary.AddInfoToSend("message", sessionSuccessResponseData.Message);
                    _testLibrary.AddInfoToSend("timestamp", sessionSuccessResponseData.Timestamp);
                    _testLibrary.AddInfoToSend("adid", sessionSuccessResponseData.Adid);
                    if (sessionSuccessResponseData.JsonResponse != null)
                        _testLibrary.AddInfoToSend("jsonResponse", sessionSuccessResponseData.JsonResponse.ToJson());
                    _testLibrary.SendInfoToServer();
                };

            if (Command.ContainsParameter("sessionCallbackSendFailure"))
                adjustConfig.SesssionTrackingFailed += sessionFailureResponseData =>
                {
                    Log.Debug(TAG,
                        "SesssionTrackingFailed, sessionFailureResponseData = " + sessionFailureResponseData);
                    _testLibrary.AddInfoToSend("message", sessionFailureResponseData.Message);
                    _testLibrary.AddInfoToSend("timestamp", sessionFailureResponseData.Timestamp);
                    _testLibrary.AddInfoToSend("adid", sessionFailureResponseData.Adid);
                    _testLibrary.AddInfoToSend("willRetry", sessionFailureResponseData.WillRetry.ToString().ToLower());
                    if (sessionFailureResponseData.JsonResponse != null)
                        _testLibrary.AddInfoToSend("jsonResponse", sessionFailureResponseData.JsonResponse.ToJson());
                    _testLibrary.SendInfoToServer();
                };

            if (Command.ContainsParameter("eventCallbackSendSuccess"))
                adjustConfig.EventTrackingSucceeded += eventSuccessResponseData =>
                {
                    Log.Debug(TAG, "EventTrackingSucceeded, eventSuccessResponseData = " + eventSuccessResponseData);

                    _testLibrary.AddInfoToSend("message", eventSuccessResponseData.Message);
                    _testLibrary.AddInfoToSend("timestamp", eventSuccessResponseData.Timestamp);
                    _testLibrary.AddInfoToSend("adid", eventSuccessResponseData.Adid);
                    _testLibrary.AddInfoToSend("eventToken", eventSuccessResponseData.EventToken);
                    if (eventSuccessResponseData.JsonResponse != null)
                        _testLibrary.AddInfoToSend("jsonResponse", eventSuccessResponseData.JsonResponse.ToJson());
                    _testLibrary.SendInfoToServer();
                };

            if (Command.ContainsParameter("eventCallbackSendFailure"))
                adjustConfig.EventTrackingFailed += eventFailureResponseData =>
                {
                    Log.Debug(TAG, "EventTrackingFailed, eventFailureResponseData = " + eventFailureResponseData);

                    _testLibrary.AddInfoToSend("message", eventFailureResponseData.Message);
                    _testLibrary.AddInfoToSend("timestamp", eventFailureResponseData.Timestamp);
                    _testLibrary.AddInfoToSend("adid", eventFailureResponseData.Adid);
                    _testLibrary.AddInfoToSend("eventToken", eventFailureResponseData.EventToken);
                    _testLibrary.AddInfoToSend("willRetry", eventFailureResponseData.WillRetry.ToString().ToLower());
                    if (eventFailureResponseData.JsonResponse != null)
                        _testLibrary.AddInfoToSend("jsonResponse", eventFailureResponseData.JsonResponse.ToJson());
                    _testLibrary.SendInfoToServer();
                };
        }

        private void Start()
        {
            Config();

            var configNumber = 0;
            if (Command.ContainsParameter("configName"))
            {
                var configName = Command.GetFirstParameterValue("configName");
                configNumber = int.Parse(configName.Substring(configName.Length - 1));
            }

            var adjustConfig = _savedConfigs[configNumber];
            
            AdjustConfig.BasePath = BasePath;
            Adjust.ApplicationLaunching(adjustConfig);

            _savedConfigs.Remove(0);
        }

        private void Event()
        {
            var eventNumber = 0;
            if (Command.ContainsParameter("eventName"))
            {
                var eventName = Command.GetFirstParameterValue("eventName");
                eventNumber = int.Parse(eventName.Substring(eventName.Length - 1));
            }

            AdjustEvent adjustEvent = null;
            if (_savedEvents.ContainsKey(eventNumber))
            {
                adjustEvent = _savedEvents[eventNumber];
            }
            else
            {
                var eventToken = Command.GetFirstParameterValue("eventToken");
                adjustEvent = new AdjustEvent(eventToken);
                _savedEvents.Add(eventNumber, adjustEvent);
            }

            if (Command.ContainsParameter("revenue"))
            {
                var revenueParams = Command.Parameters["revenue"];
                var currency = revenueParams[0];
                var revenue = double.Parse(revenueParams[1]);
                adjustEvent.SetRevenue(revenue, currency);
            }

            if (Command.ContainsParameter("callbackParams"))
            {
                var callbackParams = Command.Parameters["callbackParams"];
                for (var i = 0; i < callbackParams.Count; i = i + 2)
                {
                    var key = callbackParams[i];
                    var value = callbackParams[i + 1];
                    adjustEvent.AddCallbackParameter(key, value);
                }
            }

            if (Command.ContainsParameter("partnerParams"))
            {
                var partnerParams = Command.Parameters["partnerParams"];
                for (var i = 0; i < partnerParams.Count; i = i + 2)
                {
                    var key = partnerParams[i];
                    var value = partnerParams[i + 1];
                    adjustEvent.AddPartnerParameter(key, value);
                }
            }

            if (Command.ContainsParameter("orderId"))
            {
                var purchaseId = Command.GetFirstParameterValue("orderId");
                adjustEvent.PurchaseId = purchaseId;
            }
        }

        private void TrackEvent()
        {
            Event();

            var eventNumber = 0;
            if (Command.ContainsParameter("eventName"))
            {
                var eventName = Command.GetFirstParameterValue("eventName");
                eventNumber = int.Parse(eventName.Substring(eventName.Length - 1));
            }

            var adjustEvent = _savedEvents[eventNumber];
            Adjust.TrackEvent(adjustEvent);

            _savedEvents.Remove(0);
        }

        private void Pause()
        {
            Adjust.ApplicationDeactivated();
        }

        private void Resume()
        {
            Adjust.ApplicationActivated();
        }

        private void SetEnabled()
        {
            var enabled = bool.Parse(Command.GetFirstParameterValue("enabled"));
            Adjust.SetEnabled(enabled);
        }

        private void SetOfflineMode()
        {
            var enabled = bool.Parse(Command.GetFirstParameterValue("enabled"));
            Adjust.SetOfflineMode(enabled);
        }

        private void SendFirstPackages()
        {
            Adjust.SendFirstPackages();
        }

        private void AddSessionCallbackParameter()
        {
            if (!Command.ContainsParameter("KeyValue")) return;

            var keyValuePairs = Command.Parameters["KeyValue"];
            for (var i = 0; i < keyValuePairs.Count; i = i + 2)
            {
                var key = keyValuePairs[i];
                var value = keyValuePairs[i + 1];
                Adjust.AddSessionCallbackParameter(key, value);
            }
        }

        private void AddSessionPartnerParameter()
        {
            if (!Command.ContainsParameter("KeyValue")) return;

            var keyValuePairs = Command.Parameters["KeyValue"];
            for (var i = 0; i < keyValuePairs.Count; i = i + 2)
            {
                var key = keyValuePairs[i];
                var value = keyValuePairs[i + 1];
                Adjust.AddSessionPartnerParameter(key, value);
            }
        }

        private void RemoveSessionCallbackParameter()
        {
            if (!Command.ContainsParameter("key")) return;

            var keys = Command.Parameters["key"];
            for (var i = 0; i < keys.Count; i = i + 1)
            {
                var key = keys[i];
                Adjust.RemoveSessionCallbackParameter(key);
            }
        }

        private void RemoveSessionPartnerParameter()
        {
            if (!Command.ContainsParameter("key")) return;

            var keys = Command.Parameters["key"];
            for (var i = 0; i < keys.Count; i = i + 1)
            {
                var key = keys[i];
                Adjust.RemoveSessionPartnerParameter(key);
            }
        }

        private void ResetSessionCallbackParameters()
        {
            Adjust.ResetSessionCallbackParameters();
        }

        private void ResetSessionPartnerParameters()
        {
            Adjust.ResetSessionPartnerParameters();
        }

        private void SetPushToken()
        {
            var token = Command.GetFirstParameterValue("pushToken");

            Adjust.SetPushToken(token);
        }

        private void Teardown()
        {
            var deleteStateString = Command.GetFirstParameterValue("deleteState");
            var deleteState = bool.Parse(deleteStateString);

            Log.Debug("TestApp {0}", "calling teardown with delete state");
            TeardownAll(deleteState);
        }

        private void OpenDeeplink()
        {
            var deeplink = Command.GetFirstParameterValue("deeplink");
            Adjust.AppWillOpenUrl(new Uri(deeplink));
        }

        private void TestBegin()
        {
            if (Command.ContainsParameter("basePath"))
                BasePath = Command.GetFirstParameterValue("basePath");

            TeardownAll(true);
            AdjustFactory.SetTimerInterval(null);
            AdjustFactory.SetTimerStart(null);
            AdjustFactory.SetSessionInterval(null);
            AdjustFactory.SetSubsessionInterval(null);
            _savedEvents = new Dictionary<int, AdjustEvent>();
            _savedConfigs = new Dictionary<int, AdjustConfig>();
        }

        private void TestEnd()
        {
            TeardownAll(true);
        }

        private void TeardownAll(bool deleteState)
        {
            Log.Debug(" --- trying to teardown all ---");

            ClearAllPersistedObjects();
            ClearAllPeristedValues();

            var adjustInstance = Adjust.GetAdjustInstance();
            adjustInstance?.Teardown(deleteState);

            Adjust.SetAdjustInstance(null);
            Adjust.Teardown();

            AdjustFactory.Teardown();
        }

        public static void ClearAllPersistedObjects()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            Task.Run(() =>
            {
                Debug.WriteLine("About to delete local settings. Count: {0}", localSettings.Values.Count);
                localSettings.Values.Clear();
            });
        }

        public static void ClearAllPeristedValues()
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            if (localFolder == null)
                return;

            Task.Run(async () =>
            {
                int filesDeletedCount = 0;
                foreach (var file in await localFolder.GetFilesAsync(CommonFileQuery.OrderByName))
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    filesDeletedCount++;
                }
                Debug.WriteLine("{0} files deleted from local folder.", filesDeletedCount);
            });
        }
    }
}