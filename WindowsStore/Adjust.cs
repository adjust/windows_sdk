using AdjustSdk;
using AdjustSdk.Pcl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
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
        private static bool _isApplicationActive = false;

        private static AdjustInstance _adjustInstance;
        private static AdjustInstance AdjustInstance
        {
            get
            {
                if (_adjustInstance == null)
                    _adjustInstance = new AdjustInstance();
                return _adjustInstance;
            }
            set
            {
                _adjustInstance = value;
            }
        }


        private static IDeviceUtil _deviceUtil;
        private static IDeviceUtil DeviceUtil
        {
            get
            {
                if (_deviceUtil == null)
                    _deviceUtil = new UtilWS();
                return _deviceUtil;
            }
            set
            {
                _deviceUtil = value;
            }
        }

        [Obsolete("Static setup of logging is deprecated! Use AdjustConfig constructor instead.")]
        public static void SetupLogging(Action<String> logDelegate, LogLevel? logLevel = null)
        {
            LogConfig.SetupLogging(logDelegate, logLevel);
        }

        public static bool ApplicationLaunched
        {
            get { return AdjustInstance.ApplicationLaunched; }
        }

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
            if (_isApplicationActive) { return; }

            _isApplicationActive = true;
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
            if (!_isApplicationActive) { return; }

            _isApplicationActive = false;
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
            AdjustInstance.SetPushToken(pushToken, DeviceUtil);
        }

        public static string GetAdid()
        {
            return AdjustInstance.GetAdid();
        }

        public static AdjustAttribution GetAttributon()
        {
            return AdjustInstance.GetAttribution();
        }

        /// <summary>
        /// Give user the right to be forgotten in accordance with GDPR law.
        /// </summary>
        public static void GdprForgetMe()
        {
            AdjustInstance.GdprForgetMe(DeviceUtil);
        }

#if DEBUG
        public static void SetTestOptions(Pcl.IntegrationTesting.AdjustTestOptions testOptions)
        {
            if (testOptions.Teardown.HasValue && testOptions.Teardown.Value)
            {
                if (AdjustInstance != null)
                {
                    AdjustInstance.Teardown();
                }

                _isApplicationActive = false;
                DeviceUtil = null;
                AdjustInstance = null;
                AdjustFactory.Teardown();

                // check whether to delete state 
                if (testOptions.DeleteState.HasValue && testOptions.DeleteState.Value)
                {
                    ClearAllPersistedObjects();
                    ClearAllPeristedValues();
                }
            }

            if (AdjustInstance == null)
                AdjustInstance = new AdjustInstance();

            AdjustInstance.SetTestOptions(testOptions);
        }

        private static void ClearAllPersistedObjects()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            Task.Run(() =>
            {
                Debug.WriteLine("About to delete local settings. Count: {0}", localSettings.Values.Count);
                localSettings.Values.Clear();
            });
        }

        private static void ClearAllPeristedValues()
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
#endif
    }
}