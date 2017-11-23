## Summary

This is the Windows SDK of adjust™. You can read more about adjust™ at [adjust.com](http://adjust.com).

## Table of contents

* [Example app](#example-app)
* [Basic integration](#basic-integration)
    * [1 Install the package Adjust using NuGet Package Manager](#install-adjust-package)
    * [2 Integrate adjust into your app](#integrate-adjust-package)
    * [3 Update adjust settings](#update-adjust-settings)
        * [App Token & Environment](#app-token-and-environment)
        * [Adjust Logging](#adjust-logging)
    * [4 Build your app](#build-your-app)
* [Additional features](#additional-features)
    * [Event tracking](#custom-events-tracking)
        * [Revenue tracking](#revenue-tracking)
        * [Revenue deduplication](#revenue-deduplication)
        * [Callback parameters](#callback-parameters)
        * [Partner parameters](#partner-params)
    * [Session parameters](#session-params)
        * [Session callback parameters](#session-callback-parameters)
        * [Session partner parameters](#session-partner-parameters)
        * [Delay start](#delay-start)
    * [Attribution callback](#attribution-callback)
    * [Session and event callbacks](#session-event-callbacks)
    * [Disable tracking](#disable-tracking)
    * [Offline mode](#offline-mode)
    * [Event buffering](#event-buffering)
    * [Background tracking](#background-tracking)
    * [Device IDs](#device-ids)
        * [Windows advertising identifier](#di-win-adid)
        * [Adjust device identifier](#di-adid)
    * [User attribution](#user-attribution)
    * [Push token](#push-token)
    * [Pre-installed trackers](#pre-installed-trackers)
    * [Deep linking](#deeplinking)
        * [Standard deep linking scenario](#deeplinking-standard)
        * [Deferred deep linking scenario](#deeplinking-deferred)
        * [Reattribution via deep links](#deeplinking-reattribution)
* [Troubleshooting](#troubleshooting)
    * Work in progress...
* [License](#license)

## <a id="example-app"></a>Example app

There are different example apps inside the [`Adjust` directory][example]: 
1. `AdjustUAP10Example` for Universal Windows Apps,
2. `AdjustWP81Example` for Windows Phone 8.1,
3. `AdjustWSExample` for Windows Store. 

You can use these example projects to see how the adjust SDK can be integrated into your app.

## <a id="basic-integration"></a>Basic Integration

These are the basic steps required to integrate the adjust SDK into your
Windows Phone or Windows Store project. We are going to assume that you use
Visual Studio 2015 or later, with the latest NuGet package manager installed. A previous version that supports Windows Phone 8.1 or Windows 8 should also work. The
screenshots show the integration process for a Windows Universal app, but the
procedure is very similar for both Windows Store or Phone apps. Any differences with Windows Phone 8.1
or Windows Store apps will be noted throughout the walkthrough.

### <a id="install-adjust-package"></a>1. Install the package Adjust using NuGet Package Manager

Right click on the project in the Solution Explorer, then click on `Manage NuGet Packages...`.
In the newly opened NuGet Package Manager window, click on "Browse" tab, then enter "adjust" in the search box, and press Enter.
Adjust package sould be the first search result. Click on it, and in the right pane, click on Install.

![][adjust_nuget_pm]

Another method to install Adjust package is using Package Manager Console.
In the Visual Studio menu, select `TOOLS → NuGet Package Manager → Package
Manager Console` (or, in older version of Visual Studio `TOOLS → Library Package Manager → Package
Manager Console`) to open the Package Manager Console view.

After the `PM>` prompt, enter the following line and press `<Enter>` to install
the [Adjust package][NuGet]:

```
Install-Package Adjust
```

It's also possible to install the Adjust package through the NuGet Package
Manager for your Windows Phone or Windows Store project.

### <a id="integrate-adjust-package"></a>2. Integrate adjust into your app

In the Solution Explorer, open the file `App.xaml.cs`. Add the `using
AdjustSdk;` statement at the top of the file.

Here is a snippet of the code that has to be added in `OnLaunched` method of your app.

```cs
using AdjustSdk;

sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        string appToken = "{YourAppToken}";
        string environment = AdjustConfig.EnvironmentSandbox;
        var config = new AdjustConfig(appToken, environment);
        Adjust.ApplicationLaunching(config);
        // ...
    }
}
```

### <a id="update-adjust-settings"></a>3. Update adjust settings

#### <a id="app-token-and-environment"></a>App Token & Environment

Replace the `{YourAppToken}` placeholder with your App Token, which you can find in
your [dashboard].

Depending on whether or not you build your app for testing or for production,
you will need to set the `environment` parameter with one of these values:

```cs
string environment = AdjustConfig.EnvironmentSandbox;
string environment = AdjustConfig.EnvironmentProduction;
```

**Important:** This value should be set to `AdjustConfig.EnvironmentSandbox`
if and only if you or someone else is testing your app. Make sure to set the
environment to `AdjustConfig.EnvironmentProduction` just before you publish
your app. Set it back to `AdjustConfig.EnvironmentSandbox` if you start
developing and testing it again.

We use this environment to distinguish between real traffic and test traffic
from test devices. It is very important that you keep this value meaningful at
all times, especially if you are tracking revenue.

#### <a id="adjust-logging"></a>Adjust Logging

To see the compiled logs from our library in `released` mode, it is
necessary to redirect the log output to your app while it's being tested in `debug` mode.

To do this, use the `AdjustConfig` constructor with 4 parameters, where 3rd parameter is the
delegate method which handles the logging, and 4th parameter being Log Level:

```cs
// ....
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    string appToken = "hmqwpvspxnuo";
    string environment = AdjustConfig.EnvironmentSandbox;
    var config = new AdjustConfig(appToken, environment,
        msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);
    // ...
}
// ....
```

You can increase or decrease the amount of logs you see in tests by setting the
4th argument of the `AdjustConfig` constructor, `logLevel`, with one of the following values:

```cs
logLevel: LogLevel.Verbose  // enable all logging
logLevel: LogLevel.Debug    // enable more logging
logLevel: LogLevel.Info     // the default
logLevel: LogLevel.Warn     // disable info logging
logLevel: LogLevel.Error    // disable warnings as well
logLevel: LogLevel.Assert   // disable errors as well
logLevel: LogLevel.Suppress // disable all logs
```

### <a id="build-your-app"></a>4. Build and Debug your app

From the menu, select `DEBUG → Start Debugging`. After the app launches, you
should see the Adjust debug logs in the Output view. Every Adjust specific log
starts with ```[Adjust]``` tag, like in the picture below:

![][debug_output_window]

## <a id="additional-features"></a>Additional features

Once you have integrated the adjust SDK into your project, you can take
advantage of the following features.

### <a id="custom-events-tracking"></a>Event tracking

You can use adjust to track any event in your app. Suppose you want to track
every tap of a button. You would have to create a new event token in your
[dashboard]. Let's say that event token is `abc123`. In your button's `Button_Click`
method, you can add the following lines to track the click:

```cs
var adjustEvent = new AdjustEvent("abc123");
Adjust.TrackEvent(adjustEvent);
```

### <a id="revenue-tracking"></a>Revenue tracking

If your users generate revenue by tapping on advertisements or making
in-app purchases, then you can track those revenues with events. Let's say a tap is
worth €0.01. You can then track the revenue event like this:

```cs
var adjustEvent = new AdjustEvent("abc123");
adjustEvent.SetRevenue(0.01, "EUR");
Adjust.TrackEvent(adjustEvent);
```

This can be combined with callback parameters, of course.

When you set a currency token, adjust will automatically convert the incoming revenue into the reporting revenue of your choice. Read more about [currency conversion here.][currency-conversion]

You can read more about revenue and event tracking in the [event tracking
guide.][event-tracking]

The event instance can be used to further configure before you begin tracking.

### <a id="revenue-deduplication"></a>Revenue deduplication

You can also add an optional purchase ID to avoid tracking duplicate revenues. The last ten purchase IDs are remembered, and revenue events with duplicate purchase IDs are skipped. This is especially useful for In-App Purchase tracking. You can see an example below.

If you want to track in-app purchases, please make sure to call the TrackEvent only if the purchase is finished and item is purchased. That way you can avoid tracking revenue that is not actually being generated.

```
AdjustEvent event = new AdjustEvent("abc123");

event.SetRevenue(0.01, "EUR");
event.PurchaseId = "{PurchaseId}";

Adjust.trackEvent(event);
```

### <a id="callback-parameters"></a>Callback parameters

You can register a callback URL for the events in your [dashboard]. We will send a GET request to this URL whenever an event is tracked. You can also add callback parameters to the event by calling `AddCallbackParameter` on the
event instance before tracking it. We will then append these parameters to your specified callback URL.

For example, suppose you have registered the URL
`http://www.adjust.com/callback` then track an event like this:

```cs
var adjustEvent = new AdjustEvent("abc123");

adjustEvent.AddCallbackParameter("key", "value");
adjustEvent.AddCallbackParameter("foo", "bar");

Adjust.TrackEvent(adjustEvent);
```

In that case we would track the event and send a request to:

```
http://www.adjust.com/callback?key=value&foo=bar
```

It should be mentioned that we support a variety of placeholders like
`{win_adid}` that can be used as parameter values. In the resulting callback,
this placeholder would be replaced with the Windows Advertising ID of the current device.
Also note that we don't store any of your custom parameters, but only append them to your callbacks.
If you haven't registered a callback for an event, these parameters won't even be read.

You can read more about using URL callbacks, including a full list of available
values, in our [callbacks guide][callbacks-guide].

### <a id="partner-params"></a>Partner parameters

You can also add parameters to be transmitted to network partners, which have been activated in your adjust dashboard.

This works similarly to the callback parameters mentioned above, but can be added by calling the 
`addPartnerParameter` method on your AdjustEvent instance.

```cs
var adjustEvent = new AdjustEvent("abc123");

adjustEvent.AddPartnerParameter("key", "value");
adjustEvent.AddPartnerParameter("foo", "bar");

Adjust.TrackEvent(adjustEvent);
```

You can read more about special partners and these integrations in our [guide
to special partners.][special-partners]

### <a id="session-params"></a>Set up session parameters

Some parameters are saved to be sent in every event and session of the adjust SDK. 
Once you have added any of these parameters, you don't need to add them every time, 
since they will be saved locally. If you add the same parameter twice, there will be no effect.

These session parameters can be called before the adjust SDK is launched to make sure they are sent even on install. 
If you need to send them with an install, but can only obtain the needed values after launch, it's possible to delay 
the first launch of the adjust SDK to allow this behaviour.

### <a id="session-callback-parameters"></a>Session callback parameters

The same callback parameters that are registered for [events](#callback-parameters) can be also saved to be sent 
in every event or session of the adjust SDK.

The session callback parameters have a similar interface to the event callback parameters. 
Instead of adding the key and it's value to an event, it's added through a call to 
`Adjust.AddSessionCallbackParameter(string key, string value)`:

```cs
Adjust.AddSessionCallbackParameter("foo", "bar");
```

The session callback parameters will be merged with the callback parameters added to an event. The callback parameters added to an event have precedence over the session callback parameters. Meaning that, when adding a callback parameter to an event with the same key to one added from the session, the value that prevails is the callback parameter added to the event.

It's possible to remove a specific session callback parameter by passing the desiring key to the method `Adjust.RemoveSessionCallbackParameter(string key)`.

```cs
Adjust.RemoveSessionCallbackParameter("foo");
```

If you wish to remove all keys and their corresponding values from the session callback parameters, you can reset it with the method `Adjust.ResetSessionCallbackParameters()`.

```cs
Adjust.ResetSessionCallbackParameters();
```

### <a id="session-partner-parameters">Session partner parameters

In the same way that there are [session callback parameters](#session-callback-parameters) sent in every event or 
session of the adjust SDK, there is also session partner parameters.

These will be transmitted to network partners, for the integrations that have been activated in your adjust [dashboard].

The session partner parameters have a similar interface to the event partner parameters. 
Instead of adding the key and it's value to an event, it's added through a call to 
`Adjust.AddSessionPartnerParameter(string key, string value)`:

```cs
Adjust.AddSessionPartnerParameter("foo", "bar");
```

The session partner parameters will be merged with the partner parameters added to an event. 
The partner parameters added to an event have precedence over the session partner parameters. 
Meaning that, when adding a partner parameter to an event with the same key to one added from the session, 
the value that prevails is the partner parameter added to the event.

It's possible to remove a specific session partner parameter by passing the desiring key to the method `Adjust.RemoveSessionPartnerParameter(string key)`.

```cs
Adjust.RemoveSessionPartnerParameter("foo");
```

If you wish to remove all keys and their corresponding values from the session partner parameters, you can reset it with the method `Adjust.ResetSessionPartnerParameters()`.

```cs
Adjust.RresetSessionPartnerParameters();
```

### <a id="delay-start">Delay start

Delaying the start of the adjust SDK allows your app some time to obtain session parameters, 
such as unique identifiers, to be sent on install.

Set the initial delay time in seconds with the property `DelayStart` in the `AdjustConfig` instance:

```cs
adjustConfig.DelayStart = TimeSpan.FromSeconds(5.5);
```

In this case, this will make the adjust SDK not send the initial install session and any event created for 5.5 seconds. 
After this time is expired or if you call `Adjust.SendFirstPackages()` in the meanwhile, every session parameter
will be added to the delayed install session and events and the adjust SDK will resume as usual.

The maximum delay start time of the adjust SDK is 10 seconds.

### <a id="attribution-callback"></a>Attribution callback

You can register a delegate function to be notified of tracker attribution changes. Due to the different
sources considered for attribution, this information cannot be provided synchronously.
The simplest way is to create a single anonymous delegate function.

Please make sure to consider our [applicable attribution data policies][attribution-data].

With the `AdjustConfig` instance, before starting the SDK, set the
`AttributionChanged` delegate with the `Action<AdjustAttribution>` signature.

```cs
var config = new AdjustConfig(appToken, environment);

config.AttributionChanged = (attribution) => 
    System.Diagnostics.Debug.WriteLine("attribution: " + attribution);
    
Adjust.ApplicationLaunching(config);
```

Alternatively, you could implement the `AttributionChanged` delegate interface in your 
`Application` class and set it as a delegate:

```cs
var config = new AdjustConfig(appToken, environment);
config.AttributionChanged = AdjustAttributionChanged;
Adjust.ApplicationLaunching(config);

private void AdjustAttributionChanged(AdjustAttribution attribution) 
{
    //...
}
```

The delegate function will be called when the SDK receives the final attribution
information. Within the listener function you have access to the `attribution`
parameter. Here is a quick summary of its properties:

- `string TrackerToken` the tracker token of the current install.
- `string TrackerName` the tracker name of the current install.
- `string Network` the network grouping level of the current install.
- `string Campaign` the campaign grouping level of the current install.
- `string Adgroup` the ad group grouping level of the current install.
- `string Creative` the creative grouping level of the current install.
- `string ClickLabel` the click label of the current install.
- `string Adid` the adjust device identifier.

If any value is unavailable, it will default to `null`.

### <a id="session-event-callbacks"></a>Session and event callbacks

You can register a listener to be notified when events or sessions are tracked. There are four listeners: one for tracking successful events, one for tracking failed events, one for tracking successful sessions and one for tracking failed sessions. You can add any number of listeners after creating the `AdjustConfig` object:

```cs
var config = new AdjustConfig(appToken, environment,
    msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);

// Set event success tracking delegate
config.EventTrackingSucceeded = adjustEventSuccess =>
{
    // ...
};

// Set event failure tracking delegate
config.EventTrackingFailed = adjustEventFailure =>
{
    // ...
};

// Set session success tracking delegate
config.SesssionTrackingSucceeded = adjustSessionSuccess =>
{
    // ...
};

// Set session failure tracking delegate
config.SesssionTrackingFailed = adjustSessionFailure =>
{
    // ...
};

Adjust.ApplicationLaunching(config);
```

The delegate function will be called after the SDK tries to send a package to the server.
Within the delegate function you have access to a response data object specifically for the event.
Here is a quick summary of the success session response data object fields:

- `string Message` the message from the server or the error logged by the SDK.
- `string Timestamp` timestamp from the server.
- `string Adid` a unique device identifier provided by adjust.
- `JSONObject JsonResponse` the JSON object with the reponse from the server.

Both event response data objects contain:

- `string EventToken` the event token, if the package tracked was an event.

If any value is unavailable, it will default to `null`.

And both event and session failed objects also contain:

- `bool WillRetry` indicates that will be an attempt to resend the package at a later time.

### <a id="disable-tracking"></a>Disable tracking

You can disable the adjust SDK from tracking any activities of the current device by calling `setEnabled` 
with parameter `false`. **This setting is remembered between sessions**.

```cs
Adjust.SetEnabled(false);
```

You can check if the adjust SDK is currently enabled by calling the function `isEnabled`. 
It is always possible to activatе the adjust SDK by invoking `SetEnabled` with the `enabled` parameter as `true`.

### <a id="offline-mode"></a>Offline mode

You can put the adjust SDK in offline mode to suspend transmission to our servers, while retaining tracked data
to be sent later. While in offline mode, all information is saved in a file, so be careful not to trigger too 
many events while in offline mode.

You can activate offline mode by calling `SetOfflineMode` with the parameter `true`.

```cs
Adjust.SetOfflineMode(true);
```

Conversely, you can deactivate offline mode by calling `SetOfflineMode` with `false`. When the adjust SDK
is put back in online mode, all saved information is sent to our servers with the correct time information.

Unlike disabling tracking, this setting is **not remembered** between sessions. This means that the
SDK is in online mode whenever it is started, even if the app was terminated in offline mode.

### <a id="event-buffering"></a>Event buffering

If your app makes heavy use of event tracking, you might want to delay some HTTP requests in order to send 
them in one batch every minute. You can enable event buffering with your `AdjustConfig` instance:

```cs
var config = new AdjustConfig(appToken, environment,
    msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);

config.EventBufferingEnabled = true;

Adjust.ApplicationLaunching(config);
```

### <a id="background-tracking"></a>Background tracking

The default behaviour of the adjust SDK is to pause sending HTTP requests while the app is in the background. 
You can change this in your `AdjustConfig` instance:

```cs
var config = new AdjustConfig(appToken, environment,
    msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);

config.SendInBackground = true;

Adjust.ApplicationLaunching(config);
```

### <a id="device-ids"></a>Device IDs

The adjust SDK offers you possibility to obtain some of the device identifiers.

### <a id="di-win-adid"></a>Windows advertising identifier

Retrieves a unique ID used to provide more relevant advertising on Windows platform. 
When the advertising ID feature is turned off on the device by the user, this is an empty string.

Get Windows ADID by calling `GetWindowsAdId` method:

```cs
var windowsAdid = Adjust.GetWindowsAdId();
```

### <a id="di-adid"></a>Adjust device identifier

For each device with your app installed on it, adjust backend generates unique **adjust device identifier** (**adid**). 
In order to obtain this identifier, you can make a call to following method on `Adjust` instance:

```cs
string adid = Adjust.GetAdid();
```

**Note**: You can only make this call in the Adjust SDK v4.12.0 and above.

**Note**: Information about **adid** is available after app installation has been tracked by the adjust backend.
From that moment on, adjust SDK has information about your device **adid** and you can access it with this method. 
So, **it is not possible** to access **adid** value before the SDK has been initialised and installation of your 
app was tracked successfully.

### <a id="user-attribution"></a>User attribution

Like described in [attribution callback section](#attribution-callback), this callback get triggered providing
you info about new attribution when ever it changes. In case you want to access info about your user's current 
attribution when ever you need it, you can make a call to following method of the `Adjust` instance:

```cs
AdjustAttribution attribution = Adjust.GetAttribution();
```

**Note**: You can only make this call in the Adjust SDK v4.12.0 and above.

**Note**: Information about current attribution is available after app installation has been tracked by the adjust backend and attribution callback has been initially triggered. From that moment on, adjust SDK has information about your user's attribution and you can access it with this method. So, **it is not possible** to access user's attribution value before the SDK has been initialised and attribution callback has been initially triggered.

### <a id="push-token"></a>Push token

To send us the push notification token, add the following call to Adjust once you have obtained your token
or when ever it's value is changed:

```cs
Adjust.SetPushToken(pushNotificationsToken);
```

### <a id="pre-installed-trackers">Pre-installed trackers

If you want to use the Adjust SDK to recognize users that found your app pre-installed on their device,
follow these steps.

1. Create a new tracker in your [dashboard].
2. Set the ```DefaultTracker``` property of your `AdjustConfig`:

    ```cs
        var config = new AdjustConfig(appToken, environment,
            msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);
        config.DefaultTracker = "{TrackerToken}";
        Adjust.ApplicationLaunching(config);
    ```

  Replace `{TrackerToken}` with the tracker token you created in step 2.
  Please note that the dashboard displays a tracker URL (including
  `http://app.adjust.com/`). In your source code, you should specify only the
  six-character token and not the entire URL.

3. Build and run your app. You should see a line like the following in Debug Output:

    ```
    Default tracker: 'abc123'
    ```

### <a id="deeplinking"></a>Deep linking

You can set up the adjust SDK to handle any deep links (also known as URI activation in Universal apps) used to open your app. 
We will only read adjust-specific parameters. 

If you are using the adjust tracker URL with an option to deep link into your app from the URL, there is the possibility
to get info about the deep link URL and its content. Hitting the URL can happen when the user has your app already
installed (standard deep linking scenario) or if they don't have the app on their device (deferred deep linking scenario).
In the standard deep linking scenario, Windows platform natively offers the possibility for you to get the info about the
deep link content.
Deferred deep linking scenario is something which Windows platform doesn't support out of box and for this case,
the adjust SDK will offer you the mechanism to get the info about the deep link content.

### <a id="deeplinking-standard">Standard deep linking scenario

If a user has your app installed and you want it to launch after hitting an adjust tracker URL with the deep_link parameter
in it, you need to enable deep linking in your app. This is being done by choosing a desired **unique scheme name** and 
assigning it to the specific handler method in your app which runs once the app opens after the user clicked on the link. 
This is set in the `Package.appxmanifest`, and here's how you indicate that your app handles your unique URI scheme name:

```
1. in the Solution Explorer, double-click *package.appxmanifest* to open the manifest designer,
2. select the Declarations tab and in the Available Declarations drop-down, select Protocol and then click Add,
3. choose a name for the Uri scheme (the Name must be in all lower case letters),
4. press Ctrl+S to save the change to package.appxmanifest.
```

![][unique_scheme_name_setup]

Here, we added a protocol with assigned unique scheme name of *myapp*;

Next thing you have to set up is *OnActivated* event handler which handles the activated deeplink event. 

In your App.xaml.cs file, add the following:

```cs
// ...
protected override void OnActivated(IActivatedEventArgs args)
{
    if (args.Kind == ActivationKind.Protocol)
    {
        var eventArgs = args as ProtocolActivatedEventArgs;
        if (eventArgs != null)
        {
            // to get deep link URI:
            Uri deeplink = eventArgs.Uri;
            
            // ...            
        }
    }
    base.OnActivated(args);
}
// ...
```

`You can find more info on the official Microsoft documentation:` [URI activation handling][handle-uri-activation]

With this being set, you need to use the assigned scheme name in the adjust tracker URL's `deep_link` parameter if you want
your app to launch once the tracker URL is clicked. A tracker URL without any information added to the deep link can be built
to look something like this:

```
https://app.adjust.com/abc123?deep_link=adjustExample%3A%2F%2F
```

Please, have in mind that the `deep_link` parameter value in the URL **must be URL encoded**.

After clicking this tracker URL, and with the app set as described above, your app will launch along with *OnActivated* 
event handler, inside which you will automatically be provided with the information about the `deep_link` parameter content. 
Once this content is delivered to you, it **will not be encoded**, although it was encoded in the URL.

### <a id="deeplinking-deferred">Deferred deep linking scenario

Deferred deep linking scenario happens when a user clicks on the adjust tracker URL with the `deep_link` parameter in it,
but does not have the app installed on the device at the moment of click. After that, the user will get redirected to the 
Microsoft Store to download and install your app. After opening it for the first time, the content of the `deep_link` 
parameter will be delivered to the app.

In order to get info about the `deep_link` parameter content in a deferred deep linking scenario, you should set a 
delegate method (`DeeplinkResponse`) on the `AdjustConfig` object. This will get triggered once the adjust SDK gets the 
info about the deep link content from the backend.

```cs
// ...
var config = new AdjustConfig(appToken, environment,
    msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Verbose);

config.DeeplinkResponse = deepLinkUri =>
{
    if (ShouldAdjustSdkLaunchTheDeeplink(deepLinkUri))
    {
        return true;
    }
    else
    {
        return false;    
    }
};

Adjust.ApplicationLaunching(config);
// ...
```


Once the adjust SDK receives the info about the deep link content from the backend, it will deliver you the info about its 
content in this delegate and expect the `bool` return value from you. This return value represents your decision on whether 
the adjust SDK should launch *OnActivated* event handler to which you have assigned the scheme name from the deep link 
(like in the [Standard deep linking scenario](#deeplinking-standard)) or not.

If you return `true`, we will launch it and the exact same scenario which is described in the 
[Standard deep linking scenario chapter](#deeplinking-standard) will happen. If you do not want the SDK to launch the 
*OnActivated* event handler, you can return `false` from this delegate (`DeeplinkResponse`) and based on the deep link 
content decide on your own what to do next in your app.

### <a id="deeplinking-reattribution">Reattribution via deep links

Handling of deep links (URI activation un UAP) used to open your app is essential if you are planning to 
run *retargeting* or *re-engagement* campaigns with deep links. For more information on how to do that, 
please check our [official docs][reattribution-with-deeplinks].

If you are using this feature, in order for your user to be properly reattributed, you need to make one additional 
call to the adjust SDK in your app.

Once you have received deep link content information in your app, add a call to `Adjust.AppWillOpenUrl` method. 
By making this call, the adjust SDK will try to find if there is any new attribution info inside of the deep link 
and if any, it will be sent to the adjust backend. If your user should be reattributed due to a click on the adjust 
tracker URL with deep link content in it, you will see the [attribution callback](#attribution-callback) in your app
being triggered with new attribution info for this user.

The call to `Adjust.AppWillOpenUrl` should be done in `OnActivated` method of your app, like this:

```cs
using AdjustSdk;

public partial class App : Application
{
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
        //...
    }
}
```

## <a id="troubleshooting">Troubleshooting

```Work in progress...```

[adjust.com]: http://www.adjust.com
[dashboard]: http://www.adjust.com
[nuget]: http://nuget.org/packages/Adjust
[nuget_click]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[adjust_nuget_pm]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/v4_12/adjust_nuget_pm.png
[debug_output_window]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/v4_12/debug_output_window.png
[unique_scheme_name_setup]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/v4_12/unique_scheme_name_setup.png
[attribution-data]: https://github.com/adjust/sdks/blob/master/doc/attribution-data.md

[releases]:      https://github.com/adjust/adjust_android_sdk/releases
[callbacks-guide]:      https://docs.adjust.com/en/callbacks
[event-tracking]:       https://docs.adjust.com/en/event-tracking
[special-partners]:     https://docs.adjust.com/en/special-partners
[example]:              https://github.com/adjust/windows_sdk/tree/master/Adjust
[currency-conversion]:  https://docs.adjust.com/en/event-tracking/#tracking-purchases-in-different-currencies
[reattribution-with-deeplinks]:   https://docs.adjust.com/en/deeplinking/#manually-appending-attribution-data-to-a-deep-link

[handle-uri-activation]: https://docs.microsoft.com/en-us/windows/uwp/launch-resume/handle-uri-activation

## <a id="license"></a>License

The adjust SDK is licensed under the MIT License.

Copyright (c) 2012-2017 adjust GmbH,
http://www.adjust.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.