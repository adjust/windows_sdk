using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class AdjustInstance
    {
        private IActivityHandler _activityHandler;
        private readonly ILogger _logger = AdjustFactory.Logger;
        private List<Action<ActivityHandler>> _sessionParametersActionsArray;
        private string _pushToken;

        public bool ApplicationLaunched => _activityHandler != null;

        public AdjustInstance()
        {
        }

        public void ApplicationLaunching(AdjustConfig adjustConfig, IDeviceUtil deviceUtil)
        {
            adjustConfig.PushToken = _pushToken;
            adjustConfig.SessionParametersActions = _sessionParametersActionsArray;
            _activityHandler = ActivityHandler.GetInstance(adjustConfig, deviceUtil);
        }

        public void TrackEvent(AdjustEvent adjustEvent)
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.TrackEvent(adjustEvent);
        }

        public void ApplicationActivated()
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.ApplicationActivated();
        }

        public void ApplicationDeactivated()
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.ApplicationDeactivated();
        }

        public void SetEnabled(bool enabled)
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.SetEnabled(enabled);
        }

        public bool IsEnabled()
        {
            if (!CheckActivityHandler()) { return false; }
            return _activityHandler.IsEnabled();
        }

        public void SetOfflineMode(bool offlineMode)
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.SetOfflineMode(offlineMode);
        }

        public void AppWillOpenUrl(Uri uri)
        {
            if (!CheckActivityHandler()) { return; }
            var clickTime = DateTime.Now;
            _activityHandler.OpenUrl(uri, clickTime);
        }

        public AdjustAttribution GetAttribution()
        {
            if (!CheckActivityHandler()) { return null; }
            return _activityHandler.GetAttribution();
        }

        private bool CheckActivityHandler()
        {
            if (_activityHandler == null)
            {
                _logger.Error("Please initialize Adjust by calling 'ApplicationLaunching' before");
                return false;
            }

            return true;
        }

        public void AddSessionCallbackParameter(string key, string value)
        {
            if (_activityHandler != null)
            {
                _activityHandler.AddSessionCallbackParameter(key, value);
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.AddSessionCallbackParameterI(key, value);
            });
        }

        public void AddSessionPartnerParameter(string key, string value)
        {
            if (_activityHandler != null)
            {
                _activityHandler.AddSessionPartnerParameter(key, value);
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.AddSessionPartnerParameterI(key, value);
            });
        }

        public void RemoveSessionCallbackParameter(string key)
        {
            if (_activityHandler != null)
            {
                _activityHandler.RemoveSessionCallbackParameter(key);
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.RemoveSessionCallbackParameterI(key);
            });
        }

        public void RemoveSessionPartnerParameter(string key)
        {
            if (_activityHandler != null)
            {
                _activityHandler.RemoveSessionPartnerParameter(key);
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.RemoveSessionPartnerParameterI(key);
            });
        }

        public void ResetSessionCallbackParameters()
        {
            if (_activityHandler != null)
            {
                _activityHandler.ResetSessionCallbackParameters();
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.ResetSessionCallbackParametersI();
            });
        }

        public void ResetSessionPartnerParameters()
        {
            if (_activityHandler != null)
            {
                _activityHandler.ResetSessionPartnerParameters();
                return;
            }

            if (_sessionParametersActionsArray == null)
            {
                _sessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _sessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.ResetSessionPartnerParametersI();
            });
        }

        public void SetPushToken(string pushToken)
        {
            if (_activityHandler != null)
            {
                _activityHandler.SetPushToken(pushToken);
                return;
            }

            _pushToken = pushToken;
        }

        public void SendFirstPackages()
        {
            if (!CheckActivityHandler()) { return; }
            _activityHandler.SendFirstPackages();
        }

        public string GetAdid()
        {
            if (!CheckActivityHandler()) { return null; }
            return _activityHandler.GetAdid();
        }
    }
}
