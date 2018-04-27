using AdjustSdk;
using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace AdjustWSExample
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            // deprecated way of logging setup, use AdjustConfig constructor instead
            Adjust.SetupLogging(
                logDelegate: msg => System.Diagnostics.Debug.WriteLine(msg),
                logLevel: LogLevel.Verbose);

            // configure Adjust - without logging
            //var config = new AdjustConfig("{yourAppToken}", AdjustConfig.EnvironmentSandbox);

            // configure Adjust - with logging (Sandox env & Verbose log level)
            string appToken = "2fm9gkqubvpc";
            string environment = AdjustConfig.EnvironmentSandbox;
            var config = new AdjustConfig(appToken, environment,
                msg => Debug.WriteLine(msg), LogLevel.Verbose);

            // configure app secret (available since v4.12)
            // config.SetAppSecret(0, 1000, 3000, 4000, 5000);

            // enable event buffering
            //config.EventBufferingEnabled = true;

            // set default tracker
            //config.DefaultTracker = "{YourDefaultTracker}";

            ExampleOfVariousCallbacksSetup(config);

            // signal Adjust that the application was launched
            Adjust.ApplicationLaunching(config);

            // put the SDK in offline mode
            //Adjust.SetOfflineMode(offlineMode: true);

            // disable the SDK
            //Adjust.SetEnabled(enabled: false);
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        private void ExampleOfVariousCallbacksSetup(AdjustConfig config)
        {
            // set event success tracking delegate
            config.EventTrackingSucceeded = adjustEventSuccess =>
            {
                // ...
                //Debug.WriteLine("adjustEventSuccess: " + adjustEventSuccess);
            };

            // set event failure tracking delegate
            config.EventTrackingFailed = adjustEventFailure =>
            {
                // ...
                //Debug.WriteLine("adjustEventFailure: " + adjustEventFailure);
            };

            // set session success tracking delegate
            config.SesssionTrackingSucceeded = adjustSessionSuccess =>
            {
                // ...
                //Debug.WriteLine("adjustSessionSuccess: " + adjustSessionSuccess);
            };

            // set session failure tracking delegate
            config.SesssionTrackingFailed = adjustSessionFailure =>
            {
                // ...
                //Debug.WriteLine("adjustSessionFailure: " + adjustSessionFailure);
            };

            // set attribution delegate
            config.AttributionChanged = attribution =>
            {
                // ...
                Debug.WriteLine("attribution: " + attribution);
            };

            // deferred deep linking scenario
            config.DeeplinkResponse = deepLinkUri =>
            {
                //Debug.WriteLine("deeplink response: " + deepLinkUri);

                if (ShouldAdjustSdkLaunchTheDeeplink(deepLinkUri))
                {
                    return true;
                }

                return false;
            };
        }

        /// <summary>
        /// Example Dummy method used in Deferred deep linking - within AdjustConfig.DeeplinkResponse delegate
        /// </summary>
        /// <param name="deepLinkUri"></param>
        /// <returns></returns>
        private bool ShouldAdjustSdkLaunchTheDeeplink(Uri deepLinkUri)
        {
            // dummy-example method
            // ...
            return true;
        }

        /// <summary>
        /// The OnActivated event handler receives all activation events. 
        /// The Kind property indicates the type of activation event. 
        /// This example is set up to handle Protocol activation events.
        /// NEEDED FOR DeepLinks
        /// </summary>
        /// <param name="args"></param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                var eventArgs = args as ProtocolActivatedEventArgs;
                if (eventArgs != null)
                {
                    Adjust.AppWillOpenUrl(eventArgs.Uri);
                }
            }
            base.OnActivated(args);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}