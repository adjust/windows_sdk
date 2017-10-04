using AdjustSdk.Pcl;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace AdjustSdk
{
    /// <summary>
    ///  The main interface to Adjust.
    ///  Use the methods of this class to tell Adjust about the usage of your app.
    ///  See the README for details.
    /// </summary>
    public class Adjust
    {
        private static IDeviceUtil _deviceUtil;
        private static AdjustInstance _adjustInstance;
        private static bool _isApplicationActive = false;

        private Adjust() { }

        [Obsolete("Static setup of logging is deprecated! Use AdjustConfig constructor instead.")]
        public static void SetupLogging(Action<string> logDelegate, LogLevel? logLevel = null)
        {
            LogConfig.SetupLogging(logDelegate, logLevel);
        }

        public static bool ApplicationLaunched => GetAdjustInstance().ApplicationLaunched;

        public static AdjustInstance GetAdjustInstance()
        {
            if (_adjustInstance == null)
                _adjustInstance = new AdjustInstance();
            return _adjustInstance;
        }

        public static void SetAdjustInstance(AdjustInstance adjustInstance)
        {
            _adjustInstance = adjustInstance;
        }

        /// <summary>
        ///  Tell Adjust that the application was launched.
        ///
        ///  This is required to initialize Adjust. Call this in the Application_Launching
        ///  method of your System.Windows.Application class.
        /// </summary>
        /// <param name="adjustConfig">
        ///   The object that configures the adjust SDK. <seealso cref="AdjustConfig"/>
        /// </param>
        public static void ApplicationLaunching(AdjustConfig adjustConfig)
        {
            if (ApplicationLaunched) { return; }
            if (_deviceUtil == null)
                _deviceUtil = new UtilWP81();
            GetAdjustInstance().ApplicationLaunching(adjustConfig, _deviceUtil);
            RegisterLifecycleEvents();
        }

        public static void Teardown()
        {
            _isApplicationActive = false;
            _deviceUtil = null;
        }

        public static void RegisterLifecycleEvents()
        {
            try
            {
                Window.Current.VisibilityChanged += VisibilityChanged;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Window.Current.VisibilityChanged for app lifecycle, {0}", ex.Message);
            }
            try
            {
                Window.Current.CoreWindow.VisibilityChanged += VisibilityChanged;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Window.Current.CoreWindow.VisibilityChanged for app lifecycle, {0}", ex.Message);
            }
            try
            {
                Application.Current.Resuming += Resuming;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Application.Current.Resuming for app lifecycle, {0}", ex.Message);
            }
            try
            {
                Application.Current.Suspending += Suspending;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Application.Current.Suspending for app lifecycle, {0}", ex.Message);
            }
        }

        private static void VisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
        {
            VisibilityChanged(args.Visible);
        }

        private static void VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            VisibilityChanged(e.Visible);
        }

        private static void VisibilityChanged(bool visible)
        {
            if (visible)
            {
                ApplicationActivated();
            }
            else
            {
                ApplicationDeactivated();
            }
        }

        private static void Resuming(object sender, object e)
        {
            ApplicationActivated();
        }

        private static void Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            ApplicationDeactivated();
        }

        /// <summary>
        ///  Tell Adjust that the application is activated (brought to foreground).
        ///
        ///  This is used to keep track of the current session state.
        ///  Call this in the Application_Activated method of your System.Windows.Application class.
        /// </summary>
        public static void ApplicationActivated()
        {
            if (_isApplicationActive) { return; }

            _isApplicationActive = true;
            GetAdjustInstance().ApplicationActivated();
        }

        /// <summary>
        ///  Tell Adjust that the application is deactivated (sent to background).
        ///
        ///  This is used to calculate session attributes like session length and subsession count.
        ///  Call this in the Application_Deactivated method of your System.Windows.Application class.
        /// </summary>
        public static void ApplicationDeactivated()
        {
            if (!_isApplicationActive) { return; }

            _isApplicationActive = false;
            GetAdjustInstance().ApplicationDeactivated();
        }

        /// <summary>
        ///  Tell Adjust that a particular event has happened.
        /// </summary>
        /// <param name="adjustEvent">
        ///  The object that configures the event. <seealso cref="AdjustEvent"/>
        /// </param>
        public static void TrackEvent(AdjustEvent adjustEvent)
        {
            GetAdjustInstance().TrackEvent(adjustEvent);
        }

        /// <summary>
        /// Enable or disable the adjust SDK
        /// </summary>
        /// <param name="enabled">The flag to enable or disable the adjust SDK</param>
        public static void SetEnabled(bool enabled)
        {
            GetAdjustInstance().SetEnabled(enabled);
        }

        /// <summary>
        /// Check if the SDK is enabled or disabled
        /// </summary>
        /// <returns>true if the SDK is enabled, false otherwise</returns>
        public static bool IsEnabled()
        {
            return GetAdjustInstance().IsEnabled();
        }

        /// <summary>
        /// Puts the SDK in offline or online mode
        /// </summary>
        /// <param name="offlineMode">The flag to enable or disable offline mode</param>
        public static void SetOfflineMode(bool offlineMode)
        {
            GetAdjustInstance().SetOfflineMode(offlineMode);
        }

        /// <summary>
        /// Read the URL that opened the application to search for
        /// an adjust deep link
        /// </summary>
        /// <param name="url">The url that open the application</param>
        public static void AppWillOpenUrl(Uri url)
        {
            GetAdjustInstance().AppWillOpenUrl(url);
        }

        /// <summary>
        /// Get the Windows Advertising Id 
        /// </summary>
        public static string GetWindowsAdId()
        {
            return _deviceUtil.ReadWindowsAdvertisingId();
        }

        public static void AddSessionCallbackParameter(string key, string value)
        {
            GetAdjustInstance().AddSessionCallbackParameter(key, value);
        }

        public static void AddSessionPartnerParameter(string key, string value)
        {
            GetAdjustInstance().AddSessionPartnerParameter(key, value);
        }

        public static void RemoveSessionCallbackParameter(string key)
        {
            GetAdjustInstance().RemoveSessionCallbackParameter(key);
        }

        public static void RemoveSessionPartnerParameter(string key)
        {
            GetAdjustInstance().RemoveSessionPartnerParameter(key);
        }

        public static void ResetSessionCallbackParameters()
        {
            GetAdjustInstance().ResetSessionCallbackParameters();
        }

        public static void ResetSessionPartnerParameters()
        {
            GetAdjustInstance().ResetSessionPartnerParameters();
        }

        public static void SendFirstPackages()
        {
            GetAdjustInstance().SendFirstPackages();
        }

        public static void SetPushToken(string pushToken)
        {
            GetAdjustInstance().SetPushToken(pushToken, _deviceUtil);
        }

        public static string GetAdid()
        {
            return GetAdjustInstance().GetAdid();
        }

        public static AdjustAttribution GetAttributon()
        {
            return GetAdjustInstance().GetAttribution();
        }
    }
}