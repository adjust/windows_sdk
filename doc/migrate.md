## Migrate your adjust SDK for Windows to v3.0.0 from v2.1.0

We renamed the main class `AdjustIo` to `Adjust`. Follow these steps to update
all adjust SDK calls.

1. Open the nuget manager console. In the Visual Studio menu select 
   `TOOLS → Library Package Manager → Package Manager Console` to open the Package
   Manager Console view.

       ![][nuget_click]

2. Uninstall the Adjust package. After the `PM>` prompt, enter the following
   line and press `<Enter>` to uninstall the AdjustIo package:

   ```
   Uninstall-Package AdjustIo
   ```

       ![][nuget_uninstall]

   It's also possible to uninstall the AdjustIo package through the NuGet package manager
   for your Windows Phone or Windows Store project.

3. Install the Adjust package. After uninstalling the AdjustIo package, keep in the
   same `PM>` prompt. Enter the following line and press `<Enter>` to install the
   new [Adjust package][NuGet]:

   ```
   Install-Package adjust
   ```

       ![][nuget_migration_install]

4. Replace the namespace import. In the Visual Studio menu select
   `EDIT → Find and Replace → Replace in Files` to open the Find and Replace window.
   Search for the import `using adeven.AdjustIo` and replace it for the import
   `using AdjustSdk`. Tick the checkbox `Keep Modified files open after Replace All` so
   it's easier to perform the next step. The file `App.xaml.cs` should be open, as
   well the ones used to track an event and/or revenue.

       ![][replace]

5. In the same files where the namespace was replaced, change the static invocations of
   the class `AdjustIo` to `Adjust`. For example, from `AdjustIo.TrackRevenue(1.5)` to
   `Adjust.TrackRevenue(1.5)`.


6. Replace the Environment enum references. In the `App.xaml.cs` file, where the method 
   `SetEnviornment` is called after the `AppDidLauch` method.

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

7. Replace the LogLevel enum references. In the `App.xaml.cs` file, 
   if you use the `SetLogLevel` method to change the default logging level, remove 
   the `AdjustIo` namespace prefix. For example, from `AdjustIo.LogLevel.Info`
   to `LogLevel.Info`.

8. Build your project to confirm that everything is properly connected again.

The adjust SDK v3.0.0 added delegate callbacks. Check out the [README] for
details.

[README]: ../README.md
[nuget_click]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[nuget_migration_uninstall]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/02_nuget_migration_uninstall.png
[nuget_migration_install]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/03_migration_install.png
[replace]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/04_replace.png
