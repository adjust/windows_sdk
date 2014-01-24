## Summary

This is the Windows SDK of AdjustIo. You can read more about it at [adjust.io][].

## Basic Installation

These are the minimal steps required to integrate the AdjustIo SDK into your Windows Phone or Windows Store project. We are going to assume that you use Visual Studio 2013 with the latest NuGet package manager installed, but previous version that support Windows phone 8 or Windows 8 should work. The screenshots show the integration process for a Windows Phone app, but the procedure is very similar for Windows Store apps. The differences will get pointed out.

### 1. Install the package AdjustIo using NuGet
In the Visual Studio menu select `TOOLS|Library Package Manager|Package Manager Console` to open the Package Manager Console view.

![][nuget_click]

After the `PM>` prompt, enter the following line and press `<Enter>` to install the [AdjustIo package][NuGet]:

    Install-Package AdjustIo

![][nuget_install]

It's also possible to install the AdjustIo package through the NuGet package manager for your Windows Phone or Windows Store project.

### 2. Add capabilities (Windows Phone only)
In the Solution Explorer open the `Properties\WMAppManifest.xml` file, switch to the Capabilities tab and check `ID_CAP_IDENTITY_DEVICE` checkbox.

![][wp_capabilities]

### 3. Integrate AdjustIo into your app

In the Solution Explorer open the file `App.xaml.cs`. Add the `using adeven.AdjustIo;` statement at the top of the file.

#### Windows Phone

In the `Application_Launching` method of your app call the method `AppDidLaunch`. This tells AdjustIo about the launch of your Application. 


```cs
    using adeven.AdjustIo;
    // ...
    
    public partial class App : Application
    {
        //...
    
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            AdjustIo.AppDidLaunch("{YourAppToken}");
            AdjustIo.SetLogLevel(AdjustIo.LogLevel.Info);
            AdjustIo.SetEnvironment(AdjustIo.Environment.SandBox);
            //...
        }
    
        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            AdjustIo.AppDidActivate();
            //...
        }
    
        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            AdjustIo.AppDidDeactivate();
        }
```

![][wp_app_integration]

#### Windows Store

In the `OnLaunched` of your app call the method `AppDidLaunch`. This tells AdjustIo about the launch of your Application. 

```cs
    using adeven.AdjustIo;
    // ...
    
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        //...
        
        
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            AdjustIo.AppDidLaunch("{YourAppToken}");
            AdjustIo.SetLogLevel(AdjustIo.LogLevel.Info);
            AdjustIo.SetEnvironment(AdjustIo.Environment.SandBox);
            //...
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
            AdjustIo.AppDidDeactivate();
            //...
        }

        /// <summary>
        /// Invoked when the application is activated by some means other than normal
        /// launching.
        /// </summary>
        /// <param name="args">Event data for the event.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            AdjustIo.AppDidActivate();
            //...
        }
```
![][ws_app_integration]

### 4 Add AdjustIo settings

Replace `{YourAppToken}` with your App Token. You can find in your [dashboard].

You can increase or decrease the amount of logs you see by calling the
`SetLogLevel` method with one of the following parameters:

```cs
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Verbose); // enable all logging
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Debug);   // enable more logging
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Info);    // the default
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Warn);    // disable info logging
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Error);   // disable warnings as well
AdjustIo.SetLogLevel(AdjustIo.LogLevel.Assert);  // disable errors as well
```

Depending on whether or not you build your app for testing or for production
you must call the `SetEnvironment:` method with one of these parameters:

```cs
AdjustIo.SetEnvironment(AdjustIo.Environment.SandBox);
AdjustIo.SetEnvironment(AdjustIo.Environment.Production);
```

**Important:** This value should be set to `AdjustIo.Environment.SandBox` if and only if you or someone else is testing your app. Make sure to set the environment to `AdjustIo.Environment.Production` just before you publish the app. Set it back to `AdjustIo.Environment.SandBox` when you start testing it again.

We use this environment to distinguish between real traffic and artificial traffic from test devices. It is very important that you keep this value meaningful at all times! Especially if you are tracking revenue.

### 5 Build your app

From the menu select `DEBUG|Start Debugging`. After the app launched, you should see the debug log `First session` in the Output view.



## Additional features

Once you integrated the AdjustIo SDK into your project, you can take advantage of the following features wherever you see fit.

### Add tracking of custom events
You can tell AdjustIo about every event you consider to be of your interest. Suppose you want to track every click on a button. Currently you would have to ask us for an event token and we would give you one, like `abc123`. In your button's `Button_Click` method you could then add the following code to track the click:

    AdjustIo.TrackEvent("abc123");

You can also register a callback URL for that event and we will send a request to the URL whenever the event happens. In that cas you can also put some key-value-pairs in a dictionary and pass it to the trackEvent method. We will then forward these named parameters to your callback URL. Suppose you registered the URL `http://www.adeven.com/callback` for your event and execute the following lines:

    var parameters = new Dictionary<string, string> {
        { "key", "value" },
        { "foo", "bar" }
    };
    AdjustIo.TrackEvent("abc123", parameters);

In that case we would track the event and send a request to `http://www.adeven.com/callback?key=value&foo=bar`. In any case you need to import AdjustIo with `using adeven.AdjustIo` in any file that makes use of the SDK. Please note that we don't store your custom parameters. If you haven't registered a callback URL for an event, there is no point in sending us parameters.

### Add tracking of revenue
If your users can generate revenue by clicking on advertisements you can track those revenues. If the click is worth one Cent, you could make the following call to track that revenue:

    AdjustIo.TrackRevenue(1.0);

The parameter is supposed to be in Cents and will get rounded to one decimal point. If you want to differentiate between different kinds of revenue you can get different event tokens for each kind. Again, you need to ask us for event tokens that you can then use. In that case you would make a call like this:

    AdjustIo.TrackRevenue(1.0, "abc123");

You can also register a callback URL again and provide a dictionary of named parameters, just like it worked with normal events.

    var parameters = new Dictionary<string, string> {
        { "key", "value" },
        { "foo", "bar" }
    };
    AdjustIo.TrackRevenue(1.0, "abc123", parameters);

In any case, don't forget to import AdjustIo. Again, there is no point in sending parameters if you haven't registered a callback URL for that revenue event.

### Enable event buffering

If your app makes heavy use of event tracking, you might want to delay some HTTP requests in order to send them in one batch every minute. You can enable event buffering by adding the following line after calling `AdjustIo.AppDidLaunch({YourAppToken)` at the launch of the app.

```cs
AdjustIo.SetEventBufferingEnabled(enabledEventBuffering: true);
```

[adjust.io]: http://www.adjust.io
[nuget]: http://nuget.org/packages/AdjustIo
[nuget_click]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[nuget_install]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/02_nuget_install.png
[wp_capabilities]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/03_windows_phone_capabilities.png
[wp_app_integration]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/04_wp_app_integration.png
[ws_app_integration]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/05_ws_app_integration.png
[run_app]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/06_run_app.png

## License

The adjust-sdk is licensed under the MIT License.

Copyright (c) 2014 adeven GmbH,
http://www.adeven.com

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
