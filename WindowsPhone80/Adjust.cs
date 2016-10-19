using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;

namespace AdjustSdk
{
    /// <summary>
    ///  The main interface to Adjust.
    ///  Use the methods of this class to tell Adjust about the usage of your app.
    ///  See the README for details.
    /// </summary>
    public class Adjust
    {
        private static readonly DeviceUtil DeviceUtil = new UtilWP80();
        private static readonly AdjustInstance AdjustInstance = new AdjustInstance();

        private Adjust() { }

        public static void SetupLogging(Action<String> logDelegate, LogLevel? logLevel = null)
        {
            LogConfig.SetupLogging(logDelegate, logLevel);
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
            AdjustInstance.ApplicationLaunching(adjustConfig, DeviceUtil);
        }
        
        /// <summary>
        ///  Tell Adjust that the application is activated (brought to foreground).
        ///
        ///  This is used to keep track of the current session state.
        ///  Call this in the Application_Activated method of your System.Windows.Application class.
        /// </summary>
        public static void ApplicationActivated()
        {
            AdjustInstance.ApplicationActivated();
        }

        /// <summary>
        ///  Tell Adjust that the application is deactivated (sent to background).
        ///
        ///  This is used to calculate session attributes like session length and subsession count.
        ///  Call this in the Application_Deactivated method of your System.Windows.Application class.
        /// </summary>
        public static void ApplicationDeactivated()
        {
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
    }
}