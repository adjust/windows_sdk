using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class AdjustInstance
    {
        private IActivityHandler _ActivityHandler;
        private ILogger _Logger = AdjustFactory.Logger;
        private List<Action<ActivityHandler>> _SessionParametersActionsArray;
        private string _PushToken;

        public bool ApplicationLaunched
        {
            get
            {
                return _ActivityHandler != null;
            }
        }

        public AdjustInstance()
        {
        }

        public void ApplicationLaunching(AdjustConfig adjustConfig, DeviceUtil deviceUtil)
        {
            adjustConfig.PushToken = _PushToken;
            adjustConfig.SessionParametersActions = _SessionParametersActionsArray;
            _ActivityHandler = ActivityHandler.GetInstance(adjustConfig, deviceUtil);
        }

        public void TrackEvent(AdjustEvent adjustEvent)
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.TrackEvent(adjustEvent);
        }

        public void ApplicationActivated()
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.ApplicationActivated();
        }

        public void ApplicationDeactivated()
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.ApplicationDeactivated();
        }

        public void SetEnabled(bool enabled)
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.SetEnabled(enabled);
        }

        public bool IsEnabled()
        {
            if (!CheckActivityHandler()) { return false; }
            return _ActivityHandler.IsEnabled();
        }

        public void SetOfflineMode(bool offlineMode)
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.SetOfflineMode(offlineMode);
        }

        public void AppWillOpenUrl(Uri uri)
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.OpenUrl(uri);
        }

        private bool CheckActivityHandler()
        {
            if (_ActivityHandler == null)
            {
                _Logger.Error("Please initialize Adjust by calling 'ApplicationLaunching' before");
                return false;
            }

            return true;
        }

        public void AddSessionCallbackParameter(string key, string value)
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.AddSessionCallbackParameter(key, value);
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.AddSessionCallbackParameterI(key, value);
            });
        }

        public void AddSessionPartnerParameter(string key, string value)
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.AddSessionPartnerParameter(key, value);
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.AddSessionPartnerParameterI(key, value);
            });
        }

        public void RemoveSessionCallbackParameter(string key)
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.RemoveSessionCallbackParameter(key);
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.RemoveSessionCallbackParameterI(key);
            });
        }

        public void RemoveSessionPartnerParameter(string key)
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.RemoveSessionPartnerParameter(key);
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.RemoveSessionPartnerParameterI(key);
            });
        }

        public void ResetSessionCallbackParameters()
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.ResetSessionCallbackParameters();
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.ResetSessionCallbackParametersI();
            });
        }

        public void ResetSessionPartnerParameters()
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.ResetSessionPartnerParameters();
                return;
            }

            if (_SessionParametersActionsArray == null)
            {
                _SessionParametersActionsArray = new List<Action<ActivityHandler>>();
            }

            _SessionParametersActionsArray.Add((activityHandler) =>
            {
                activityHandler.ResetSessionPartnerParametersI();
            });
        }

        public void SetPushToken(string pushToken)
        {
            if (_ActivityHandler != null)
            {
                _ActivityHandler.SetPushToken(pushToken);
                return;
            }

            _PushToken = pushToken;
        }

        public void SendFirstPackages()
        {
            if (!CheckActivityHandler()) { return; }
            _ActivityHandler.SendFirstPackages();
        }

        public string GetAdid()
        {
            if (!CheckActivityHandler()) { return null; }
            return _ActivityHandler.GetAdid();
        }
    }
}
