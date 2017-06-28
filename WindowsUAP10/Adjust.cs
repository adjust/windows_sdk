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
        private static readonly DeviceUtil DeviceUtil = new UtilUAP10();
        private static readonly AdjustInstance AdjustInstance = new AdjustInstance();
        private static bool IsApplicationActive = false;

        public static void SetupLogging(Action<String> logDelegate, LogLevel? logLevel = null)
        {
            LogConfig.SetupLogging(logDelegate, logLevel);
        }

        public static bool ApplicationLaunched {
            get { return AdjustInstance.ApplicationLaunched; }
        }

        /// <summary>
        ///  Tell Adjust that the application was launched.
        ///
        ///  This is required to initialize Adjust. Call this in the Application_Launching
        ///  method of your Windows.UI.Xaml.Application class.
        /// </summary>
        /// <param name="adjustConfig">
        ///   The object that configures the adjust SDK. <seealso cref="AdjustConfig"/>
        /// </param>
        public static void ApplicationLaunching(AdjustConfig adjustConfig)
        {
            if (ApplicationLaunched) { return; }
            AdjustInstance.ApplicationLaunching(adjustConfig, DeviceUtil);
            RegisterLifecycleEvents();
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
                Application.Current.LeavingBackground += LeavingBackground;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Application.Current.LeavingBackground for app lifecycle, {0}", ex.Message);
            }
            try
            {
                Application.Current.Suspending += Suspending;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Not possible to register Application.Current.Suspending for app lifecycle, {0}", ex.Message);
            }
            try
            {
                Application.Current.EnteredBackground += EnteredBackground;
            }
            catch (Exception ex)
            {
                AdjustFactory.Logger.Debug("Application.Current.EnteredBackground for app lifecycle, {0}", ex.Message);
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

        private static void VisibilityChanged(bool Visible)
        {
            if (Visible)
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

        private static void LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            ApplicationActivated();
        }

        private static void EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            ApplicationDeactivated();
        }

        private static void Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            ApplicationDeactivated();
        }

        /// <summary>
        ///  Tell Adjust that the application is activated (brought to foreground).
        ///
        ///  This is used to keep track of the current session state.
        ///  This should only be used if the VisibilityChanged mechanism doesn't work
        /// </summary>
        public static void ApplicationActivated()
        {
            if (IsApplicationActive) { return; }

            IsApplicationActive = true;
            AdjustInstance.ApplicationActivated();
        }

        /// <summary>
        ///  Tell Adjust that the application is deactivated (sent to background).
        ///
        ///  This is used to calculate session attributes like session length and subsession count.
        ///  This should only be used if the VisibilityChanged mechanism doesn't work
        /// </summary>
        public static void ApplicationDeactivated()
        {
            if (!IsApplicationActive) { return; }

            IsApplicationActive = false;
            AdjustInstance.ApplicationDeactivated();
        }

        /// <summary>
        ///  Tell Adjust that a particular event has happened.
        /// </summary>
        /// <param name="adjustEvent">
        ///  The object that configures the event. <seealso cref="AdjustEvent"/>
        /// </param>
        public static void TrackEvent(AdjustEvent adjustEvent)
        {
            AdjustInstance.TrackEvent(adjustEvent);
        }

        /// <summary>
        /// Enable or disable the adjust SDK
        /// </summary>
        /// <param name="enabled">The flag to enable or disable the adjust SDK</param>
        public static void SetEnabled(bool enabled)
        {
            AdjustInstance.SetEnabled(enabled);
        }

        /// <summary>
        /// Check if the SDK is enabled or disabled
        /// </summary>
        /// <returns>true if the SDK is enabled, false otherwise</returns>
        public static bool IsEnabled()
        {
            return AdjustInstance.IsEnabled();
        }

        /// <summary>
        /// Puts the SDK in offline or online mode
        /// </summary>
        /// <param name="enabled">The flag to enable or disable the adjust SDK</param>
        public static void SetOfflineMode(bool offlineMode)
        {
            AdjustInstance.SetOfflineMode(offlineMode);
        }

        /// <summary>
        /// Read the URL that opened the application to search for
        /// an adjust deep link
        /// </summary>
        /// <param name="url">The url that open the application</param>
        public static void AppWillOpenUrl(Uri uri)
        {
            AdjustInstance.AppWillOpenUrl(uri);
        }

        /// <summary>
        /// Get the Windows Advertising Id
        /// </summary>
        public static string GetWindowsAdId()
        {
            return DeviceUtil.ReadWindowsAdvertisingId();
        }

        public static void AddSessionCallbackParameter(string key, string value)
        {
            AdjustInstance.AddSessionCallbackParameter(key, value);
        }

        public static void AddSessionPartnerParameter(string key, string value)
        {
            AdjustInstance.AddSessionPartnerParameter(key, value);
        }

        public static void RemoveSessionCallbackParameter(string key)
        {
            AdjustInstance.RemoveSessionCallbackParameter(key);
        }

        public static void RemoveSessionPartnerParameter(string key)
        {
            AdjustInstance.RemoveSessionPartnerParameter(key);
        }

        public static void ResetSessionCallbackParameters()
        {
            AdjustInstance.ResetSessionCallbackParameters();
        }

        public static void ResetSessionPartnerParameters()
        {
            AdjustInstance.ResetSessionPartnerParameters();
        }

        public static void SendFirstPackages()
        {
            AdjustInstance.SendFirstPackages();
        }

        public static void SetPushToken(string pushToken)
        {
            AdjustInstance.SetPushToken(pushToken);
        }

        public static string GetAdid()
        {
            return AdjustInstance.GetAdid();
        }

        public static AdjustAttribution GetAttributon()
        {
            return AdjustInstance.GetAttribution();
        }
    }
}