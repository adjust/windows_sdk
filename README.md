## Summary

This is the Windows SDK of AdjustIo. You can read more about it at [adjust.io][].

## Basic Installation

These are the minimal steps required to integrate the AdjustIo SDK into your Windows Store project. We are going to assume that you use Visual Studio 2012 with the NuGet package manager installed. The screenshots show the integration process for a Windows Store app, but the procedure is very similar for Windows Phone apps. The differences will get pointed out.

### 1. Install the package AdjustIo
In the Visual Studio menu select `TOOLS|Library Package Manager|Package Manager Console` to open the Package Manager Console view.

![][console]

After the `PM>` prompt, enter the following line and press `<Enter>` to install the [AdjustIo package][NuGet]:

    Install-Package AdjustIo

![][install]

### 2. Integrate AdjustIo into your app
In the Solution Explorer open the file `App.xaml.cs`. Add the `using` statement at the top of the file. In the `OnLaunched` method (`Application_Launching` for Windows Phone apps) of your app call the method `AppDidLaunch`. This tells AdjustIo about the launch of your Application.

    using adeven.AdjustIo;
    // ...
    AdjustIo.AppDidLaunch();

![][launch]

If you are building a Windows Phone app you need to add the `ID_CAP_IDENTITY_DEVICE` capability. In the Solution Explorer open the WMAppManifest.xml file, switch to the Capabilities tab and check the appropriate checkbox.

![][capabilities]

### 3. Build your app
From the menu select `DEBUG|Start Debugging`. After the app launched, you should see the debug log `Tracked session start` in the Output view.

![][output]

You can improve the session tracking by calling `AppDidActivate` in `OnActivated` (`Application_Activated` for Windows Phone apps).

    protected override void OnActivated(IActivatedEventArgs args)
    {
        AdjustIo.AppDidActivate();
        // ...
    }

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

    AdjustIo.TrackRevenue(1.0f);

The parameter is supposed to be in Cents and will get rounded to one decimal point. If you want to differentiate between different kinds of revenue you can get different event tokens for each kind. Again, you need to ask us for event tokens that you can then use. In that cas you would make a call like this:

    AdjustIo.TrackRevenue(1.0f, "abc123");

You can also register a callback URL again and provide a dictionary of named parameters, just like it worked with normal events.

    var parameters = new Dictionary<string, string> {
        { "key", "value" },
        { "foo", "bar" }
    };
    AdjustIo.TrackRevenue(1.0f, "abc123", parameters);

In any case, don't forget to import AdjustIo. Again, there is no point in sending parameters if you haven't registered a callback URL for that revenue event.

[adjust.io]: http://www.adjust.io
[nuget]: http://nuget.org/packages/AdjustIo
[console]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/console.png
[install]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/install.png
[launch]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/launch.png
[output]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/output.png
[capabilities]: https://raw.github.com/adeven/adjust_sdk/master/Resources/windows/capabilities.png


## License

The adjust-sdk is licensed under the MIT License.

Copyright (c) 2012 adeven GmbH,
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
