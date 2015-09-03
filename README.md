## Summary

This is the Windows SDK of adjust™. You can read more about adjust™ at [adjust.com](http://adjust.com).

## Example app

There are different example apps inside the [`Adjust` directory][example]. `AdjustWP80Example` for Windows
Phone 8.0, `AdjustWP81Example` for Windows Phone 8.1 and `AdjustWSExample` for Windows Store. You can use
these projects to see an example on how the adjust SDK can be integrated.

## Basic Installation

These are the minimal steps required to integrate the adjust SDK into your
Windows Phone or Windows Store project. We are going to assume that you use
Visual Studio 2013 or superior with the latest NuGet package manager installed, but
previous version that support Windows Phone 8.0 or Windows 8 should work. The
screenshots show the integration process for a Windows Universal app, but the
procedure is very similar for both Windows Store or Phone apps. The differences with Windows Phone 8.0
will get pointed out.

### 1. Install the package Adjust using NuGet

In the Visual Studio menu select `TOOLS → Library Package Manager → Package
Manager Console` to open the Package Manager Console view.

![][nuget_click]

After the `PM>` prompt, enter the following line and press `<Enter>` to install
the [Adjust package][NuGet]:

```
Install-Package Adjust
```

![][nuget_install]

It's also possible to install the Adjust package through the NuGet package
manager for your Windows Phone or Windows Store project.

### 2. Add capabilities (Windows Phone 8.0 only)

In the Solution Explorer open the `Properties\WMAppManifest.xml` file, switch
to the Capabilities tab and check the `ID_CAP_IDENTITY_DEVICE` checkbox.

![][wp_capabilities]

### 3. Integrate adjust into your app

In the Solution Explorer open the file `App.xaml.cs`. Add the `using
AdjustSdk;` statement at the top of the file.

#### Windows Phone 8.0

In the `Application_Launching` method of your app, call the method
`AppDidLaunch`. This tells Adjust about the launch of your Application.

```cs
using AdjustSdk;

public partial class App : Application
{
    private void Application_Launching(object sender, LaunchingEventArgs e)
    {
        string appToken = "{YourAppToken}";
        string environment = AdjustConfig.EnvironmentSandbox;
        var config = new AdjustConfig(appToken, environment);
        Adjust.ApplicationLaunching(config);
        // ...
    }

    private void Application_Activated(object sender, ActivatedEventArgs e)
    {
        Adjust.ApplicationActivated();
        // ...
    }

    private void Application_Deactivated(object sender, DeactivatedEventArgs e)
    {
        Adjust.ApplicationDeactivated();
        // ...
    }
}
```

![][wp_app_integration]

#### Universal Apps

In the `OnLaunched` method of your app, call the method `AppDidLaunch`. This
tells adjust about the launch of your Application.

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

![][ws_app_integration]

### 4. Update adjust settings

Replace the `{YourAppToken}` placeholder with your App Token. You can find in
your [dashboard].

Depending on whether or not you build your app for testing or for production,
you must set the `environment` parameter with one of these values:

```cs
string environment = AdjustConfig.EnvironmentSandbox;
string environment = AdjustConfig.EnvironmentProduction;
```

**Important:** This value should be set to `AdjustConfig.EnvironmentSandbox`
if and only if you or someone else is testing your app. Make sure to set the
environment to `AdjustConfig.EnvironmentProduction` just before you publish
the app. Set it back to `AdjustConfig.EnvironmentSandbox` when you start
developing and testing it again.

We use this environment to distinguish between real traffic and test traffic
from test devices. It is very important that you keep this value meaningful at
all times! This is especially important if you are tracking revenue.

#### Adjust Logging

To be able to see the logs from our library that is compiled is `released` mode, it is
necessary to redirect the log output to your app while it's being tested in `debug` mode.

Call the `Adjust.SetupLogging` method before any other calls to our sdk.

```cs
Adjust.SetupLogging(logDelegate: msg => System.Diagnostics.Debug.WriteLine(msg));
// ...
var config = new AdjustConfig(appToken, environment);
Adjust.ApplicationLaunching(config);
// ...
```

You can increase or decrease the amount of logs you see in tests by setting the
second argument of `SetupLogging` method, `logLevel` with one of the following values:

```cs
logLevel: LogLevel.Verbose  // enable all logging
logLevel: LogLevel.Debug    // enable more logging
logLevel: LogLevel.Info     // the default
logLevel: LogLevel.Warn     // disable info logging
logLevel: LogLevel.Error    // disable warnings as well
logLevel: LogLevel.Assert   // disable errors as well
```

#### Windows Phone 8.0

In the `Application_Launching` method of your app, call the method
`SetupLogging` before any other calls to our sdk.

```cs
using AdjustSdk;

public partial class App : Application
{
    private void Application_Launching(object sender, LaunchingEventArgs e)
    {
        Adjust.SetupLogging(logDelegate: msg => System.Diagnostics.Debug.WriteLine(msg),
            logLevel: LogLevel.Verbose);
        // ...
        var config = new AdjustConfig(appToken, environment);
        Adjust.ApplicationLaunching(config);
        // ...
    }
    // ...
}
```

#### Universal Apps

In the `OnLaunched` method of your app, call the method `SetupLogging` 
before any other calls to our sdk.

```cs
using AdjustSdk;

sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Adjust.SetupLogging(logDelegate: msg => System.Diagnostics.Debug.WriteLine(msg),
            logLevel: LogLevel.Verbose);
        // ...
        var config = new AdjustConfig(appToken, environment);
        Adjust.ApplicationLaunching(config);
        // ...
    }
}
```

### 5. Build your app

From the menu select `DEBUG → Start Debugging`. After the app launched, you
should see the debug log `Tracked session start` in the Output view.

![][run_app]

## Additional features

Once you have integrated the adjust SDK into your project, you can take
advantage of the following features.

### 6. Add tracking of custom events

You can use adjust to track any event in your app. Suppose you want to track
every tap on a button. You would have to create a new event token in your
[dashboard]. Let's say that event token is `abc123`. In your button's `Button_Click`
method you could then add the following lines to track the click:

```cs
var adjustEvent = new AdjustEvent("abc123");
Adjust.trackEvent(adjustEvent);
```

The event instance can be used to configure the event even more before tracking
it.

### 7. Add callback parameters

You can register a callback URL for your events in your [dashboard]. We will
send a GET request to that URL whenever the event gets tracked. You can add
callback parameters to that event by calling `AddCallbackParameter` on the
event instance before tracking it. We will then append these parameters to your
callback URL.

For example, suppose you have registered the URL
`http://www.adjust.com/callback` then track an event like this:

```cs
var adjustEvent = new AdjustEvent("abc123");

adjustEvent.addCallbackParameter("key", "value");
adjustEvent.addCallbackParameter("foo", "bar");

Adjust.trackEvent(adjustEvent);
```

In that case we would track the event and send a request to:

```
http://www.adjust.com/callback?key=value&foo=bar
```

It should be mentioned that we support a variety of placeholders like
`{win_adid}` that can be used as parameter values. In the resulting callback
this placeholder would be replaced with the Windows Advertising Id of the current device.
Also note that we don't store any of your custom parameters, but only append
them to your callbacks. If you haven't registered a callback for an event,
these parameters won't even be read.

You can read more about using URL callbacks, including a full list of available
values, in our [callbacks guide][callbacks-guide].

### 8. Partner parameters

You can also add parameters to be transmitted to network partners, for the
integrations that have been activated in your adjust dashboard.

This works similarly to the callback parameters mentioned above, but can be
added by calling the `AddPartnerParameter` method on your `AdjustEvent` instance.

```cs
var adjustEvent = new AdjustEvent("abc123");

adjustEvent.addPartnerParameter("key", "value");
adjustEvent.addPartnerParameter("foo", "bar");

Adjust.trackEvent(adjustEvent);
```

You can read more about special partners and these integrations in our [guide
to special partners.][special-partners]

### 9. Add tracking of revenue

If your users can generate revenue by tapping on advertisements or making
in-app purchases you can track those revenues with events. Lets say a tap is
worth one Euro cent. You could then track the revenue event like this:

```cs
var adjustEvent = new AdjustEvent("abc123");
adjustEvent.setRevenue(0.01, "EUR");
Adjust.trackEvent(adjustEvent);
```

This can be combined with callback parameters of course.

When you set a currency token, adjust will automatically convert the incoming revenues into a reporting revenue of your choice. Read more about [currency conversion here.][currency-conversion]

You can read more about revenue and event tracking in the [event tracking
guide.][event-tracking]

### 10. Set up deep link reattributions

You can set up the adjust SDK to handle deep links that are used to open your
app, also known as URI associations in Windows Phone 8.0 and URI activation in Universal apps.
We will only read certain adjust specific parameters. This is essential if
you are planning to run retargeting or re-engagement campaigns with deep links.

#### Windows Phone 8.0

In the `MapUri` method of the `UriMapperBase` class created to handle the deep links,
call the `AppWillOpenUrl` method.

```cs
using AdjustSdk;

public class AssociationUriMapper : UriMapperBase
{
    public override Uri MapUri(Uri uri)
    {
        Adjust.AppWillOpenUrl(uri);
        //...
    }
}
```

#### Universal Apps

In the `OnActivated` method of your app, call the method `AppWillOpenUrl`.

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

### 11. Enable event buffering

If your app makes heavy use of event tracking, you might want to delay some
HTTP requests in order to send them in one batch every minute. You can enable
event buffering with your `AdjustConfig` instance:

```cs
var config = new AdjustConfig(appToken, environment);

config.setEventBufferingEnabled(true);

Adjust.ApplicationLaunching(config);
```

### 12. Set listener for attribution changes

You can register a listener to be notified of tracker attribution changes. Due
to the different sources considered for attribution, this information can not
be provided synchronously. The simplest way is to create a single anonymous
listener:

Please make sure to consider our [applicable attribution data
policies][attribution-data].

With the `AdjustConfig` instance, before starting the SDK, set the
`AttributionChanged` delegate with the `Action<AdjustAttribution>` signature.

```cs
var config = new AdjustConfig(appToken, environment);

config.AttributionChanged = (attribution) => 
    System.Diagnostics.Debug.WriteLine("attribution: " + attribution);
    
Adjust.ApplicationLaunching(config);
```

Alternatively, you could implement the `AttributionChanged` delegate
interface in your `Application` class and set it as a delegate:

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

[adjust.com]: http://www.adjust.com
[dashboard]: http://www.adjust.com
[nuget]: http://nuget.org/packages/Adjust
[nuget_click]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[nuget_install]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/02_nuget_install.png
[wp_capabilities]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/03_windows_phone_capabilities.png
[wp_app_integration]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/04_wp_app_integration.png
[ws_app_integration]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/05_ws_app_integration.png
[run_app]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/06_run_app.png
[attribution-data]: https://github.com/adjust/sdks/blob/master/doc/attribution-data.md

[dashboard]:     http://adjust.com
[releases]:      https://github.com/adjust/adjust_android_sdk/releases
[import_module]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/01_import_module.png
[select_module]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/02_select_module.png
[imported_module]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/03_imported_module.png
[gradle_adjust]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/04_gradle_adjust.png
[gradle_gps]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/05_gradle_gps.png
[manifest_gps]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/06_manifest_gps.png
[manifest_permissions]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/07_manifest_permissions.png
[proguard]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/08_proguard.png
[receiver]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/09_receiver.png
[application_class]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/11_application_class.png
[manifest_application]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/12_manifest_application.png
[application_config]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/13_application_config.png
[activity]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/14_activity.png
[log_message]: https://raw.github.com/adjust/sdks/master/Resources/android/v4/15_log_message.png

[callbacks-guide]:      https://docs.adjust.com/en/callbacks
[event-tracking]:       https://docs.adjust.com/en/event-tracking
[special-partners]:     https://docs.adjust.com/en/special-partners
[example]:              https://github.com/adjust/windows_sdk/tree/master/Adjust
[currency-conversion]:  https://docs.adjust.com/en/event-tracking/#tracking-purchases-in-different-currencies


## License

The adjust-sdk is licensed under the MIT License.

Copyright (c) 2015 adjust GmbH,
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

