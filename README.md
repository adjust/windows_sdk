## Summary

This is the Windows SDK of adjust™. You can read more about it at
[adjust.io][].

## Basic Installation

These are the minimal steps required to integrate the adjust SDK into your
Windows Phone or Windows Store project. We are going to assume that you use
Visual Studio 2013 with the latest NuGet package manager installed, but
previous version that support Windows phone 8 or Windows 8 should work. The
screenshots show the integration process for a Windows Phone app, but the
procedure is very similar for Windows Store apps. The differences will get
pointed out.

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

### 2. Add capabilities (Windows Phone only)

In the Solution Explorer open the `Properties\WMAppManifest.xml` file, switch
to the Capabilities tab and check the `ID_CAP_IDENTITY_DEVICE` checkbox.

![][wp_capabilities]

### 3. Integrate adjust into your app

In the Solution Explorer open the file `App.xaml.cs`. Add the `using
AdjustSdk;` statement at the top of the file.

#### Windows Phone

In the `Application_Launching` method of your app, call the method
`AppDidLaunch`. This tells Adjust about the launch of your Application.

```cs
using AdjustSdk;

public partial class App : Application
{
    private void Application_Launching(object sender, LaunchingEventArgs e)
    {
        Adjust.AppDidLaunch("{YourAppToken}");
        Adjust.SetLogLevel(LogLevel.Info);
        Adjust.SetEnvironment(AdjustEnvironment.Sandbox);
        // ...
    }

    private void Application_Activated(object sender, ActivatedEventArgs e)
    {
        Adjust.AppDidActivate();
        // ...
    }

    private void Application_Deactivated(object sender, DeactivatedEventArgs e)
    {
        Adjust.AppDidDeactivate();
        // ...
    }
}
```

![][wp_app_integration]

#### Windows Store

In the `OnLaunched` method of your app, call the method `AppDidLaunch`. This
tells adjust about the launch of your Application.

```cs
using AdjustSdk;

sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Adjust.AppDidLaunch("{YourAppToken}");
        Adjust.SetLogLevel(LogLevel.Info);
        Adjust.SetEnvironment(AdjustEnvironment.Sandbox);
        // ...
    }
}
```

![][ws_app_integration]

### 4 Update adjust settings

Replace the `{YourAppToken}` placeholder with your App Token. You can find in
your [dashboard].

You can increase or decrease the amount of logs you see by calling the
`SetLogLevel` method with one of the following parameters:

```cs
Adjust.SetLogLevel(LogLevel.Verbose); // enable all logging
Adjust.SetLogLevel(LogLevel.Debug);   // enable more logging
Adjust.SetLogLevel(LogLevel.Info);    // the default
Adjust.SetLogLevel(LogLevel.Warn);    // disable info logging
Adjust.SetLogLevel(LogLevel.Error);   // disable warnings as well
Adjust.SetLogLevel(LogLevel.Assert);  // disable errors as well
```

Depending on whether or not you build your app for testing or for production,
you must call the `SetEnvironment` method with one of these parameters:

```cs
Adjust.SetEnvironment(AdjustEnvironment.Sandbox);
Adjust.SetEnvironment(AdjustEnvironment.Production);
```

**Important:** This value should be set to `AdjustEnvironment.Sandbox` if
and only if you or someone else is testing your app. Make sure to set the
environment to `AdjustEnvironment.Production` just before you publish the
app. Set it back to `AdjustEnvironment.Sandbox` when you start testing it
again.

We use this environment to distinguish between real traffic and artificial
traffic from test devices. It is very important that you keep this value
meaningful at all times! Especially if you are tracking revenue.

### 5. Build your app

From the menu select `DEBUG → Start Debugging`. After the app launched, you
should see the debug log `Tracked session start` in the Output view.

![][run_app]

## Additional features

Once you integrated the adjust SDK into your project, you can take advantage
of the following features wherever you see fit.

### 6. Add tracking of custom events

You can tell adjust about every event you want. Suppose you want to track
every tap on a button. You would have to create a new Event Token in your
[dashboard]. Let's say that Event Token is `abc123`. In your button's
`Button_Click` method you could then add the following line to track the click:

```cs
Adjust.TrackEvent("abc123");
```

You can also register a callback URL for that event and we will send a request
to the URL whenever the event happens. In that case you can also put some
key-value-pairs in a dictionary and pass it to the `TrackEvent`  method. We
will then forward these named parameters to your callback URL. Suppose you
registered the URL `http://adjust.com/callback` for your event and execute the
following lines:

```cs
var parameters = new Dictionary<string, string> {
    { "key", "value" },
    { "foo", "bar" }
};
Adjust.TrackEvent("abc123", parameters);
```

In that case we would track the event and send a request to
`http://adjust.com/callback?key=value&foo=bar`. In any case you need to import
Adjust with `using AdjustSdk` in any file that makes use of the SDK.
Please note that we don't store your custom parameters. If you haven't
registered a callback URL for an event, there is no point in sending us
parameters.

### 7. Add tracking of revenue

If your users can generate revenue by clicking on advertisements you can track
those revenues. If the click is worth one cent, you could make the following
call to track that revenue:

```cs
Adjust.TrackRevenue(1.0);
```

The parameter is supposed to be in cents and will get rounded to one decimal
point. If you want to differentiate between different kinds of revenue you can
get different Event Tokens for each kind. Again, you need to create those Event
Tokens in your [dashboard]. In that case you would make a call like this:

```cs
Adjust.TrackRevenue(1.0, "abc123");
```

You can also register a callback URL again and provide a dictionary of named
parameters, just like it worked with normal events.

```cs
var parameters = new Dictionary<string, string> {
    { "key", "value" },
    { "foo", "bar" }
};
Adjust.TrackRevenue(1.0, "abc123", parameters);
```

In any case, don't forget to import Adjust. Again, there is no point in
sending parameters if you haven't registered a callback URL for that revenue
event.

### 8. Receive delegate callbacks

Every time your app tries to track a session, an event or some revenue, you can
be notified about the success of that operation and receive additional
information about the current install. Follow these steps to implement a
delegate to this event.

1. Add the `using AdjustSdk` import and create a method with the signature of
the delegate `Action<ResponseData>`. It can be implemented in the `App.xaml.cs`
class.

2. After calling the `AppDidLaunch` method of the AdjustSdk, at the start of
your application, call the `SetResponseDelegate` with the previously created
method. It is also be possible to use a lambda with the same signature.

The delegate method will get called every time any activity was tracked or
failed to track. Within the delegate method you have access to the
`responseData` parameter. Here is a quick summary of its attributes:

- `ActivityKind ActivityKind` indicates what kind of activity was tracked. It has
one of these values:

```
Session
Event
Revenue
```

- `string ActivityKindString` human readable version of the activity kind.
Possible values:

```
session
event
revenue
```

- `bool Success` indicates whether or not the tracking attempt was
  successful.
- `bool WillRetry` is true when the request failed, but will be retried.
- `string Error` an error message when the activity failed to track or
  the response could not be parsed. Is `null` otherwise.
- `string TrackerToken` the tracker token of the current install. Is `null` if
  request failed or response could not be parsed.
- `string TrackerName` the tracker name of the current install. Is `null` if
  request failed or response could not be parsed.


#### Windows Phone
```cs
using AdjustSdk;

public partial class App : Application
{
    private void Application_Launching(object sender, LaunchingEventArgs e)
    {
        Adjust.AppDidLaunch("{YourAppToken}");
        Adjust.SetResponseDelegate(OnResponseAdjust);
        //...
    }
    
    private void OnResponseAdjust(ResponseData responseData) 
    {
        //...
    }
}
```

#### Windows Store
```cs
using AdjustSdk;

public partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        Adjust.AppDidLaunch("{YourAppToken}");
        Adjust.SetResponseDelegate(OnResponseAdjust);
        //...
    }
    
    private void OnResponseAdjust(ResponseData responseData) 
    {
        //...
    }
}
```

### 9.Enable event buffering

If your app makes heavy use of event tracking, you might want to delay some
HTTP requests in order to send them in one batch every minute. You can enable
event buffering by adding the following line after calling
`Adjust.AppDidLaunch` at the launch of the app.

```cs
Adjust.SetEventBufferingEnabled(enabledEventBuffering: true);
```

[adjust.com]: http://www.adjust.com
[dashboard]: http://www.adjust.com
[nuget]: http://nuget.org/packages/Adjust
[nuget_click]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[nuget_install]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/02_nuget_install.png
[wp_capabilities]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/03_windows_phone_capabilities.png
[wp_app_integration]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/04_wp_app_integration.png
[ws_app_integration]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/05_ws_app_integration.png
[run_app]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/06_run_app.png

## License

The adjust-sdk is licensed under the MIT License.

Copyright (c) 2014 adjust GmbH,
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

