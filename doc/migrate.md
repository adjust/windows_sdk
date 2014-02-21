## Summary

This is the migration guide of the Adjust Windows Sdk, from version 2 to version 3.
If you are using the Adjust Windows Sdk for the first time in your project, follow the
instructions on the README of the project.
You can read more about it at [adjust.com][].

## Major changes

The breaking changes from the version 2 to 3 of the Adjust Windows Sdk is the renaming
of most public names, from adeven to Adjust, and from AdjustIo to Adjust. This guide
will help you migrate this name change. For the new features please consult the README 
of the project.

### 1. Open the nuget manager console

In the Visual Studio menu select `TOOLS → Library Package Manager → Package
Manager Console` to open the Package Manager Console view.

![][nuget_click]

### 2. Uninstall the Adjust package

After the `PM>` prompt, enter the following line and press `<Enter>` to uninstall the
AdjustIo package:

```
Uninstall-Package AdjustIo
```

![][nuget_uninstall]

It's also possible to uninstall the AdjustIo package through the NuGet package manager
for your Windows Phone or Windows Store project.

### 3. Install the AdjustIo package

After uninstalling the AdjustIo package, keep in the same `PM>` prompt. Enter the 
following line and press `<Enter>` to install the new [Adjust package][NuGet]:

```
Install-Package AdjustIo
```

![][nuget_migration_install]

### 4. Replace the namespace import

Search and replace in your app the import of the Adjust Windows Sdk from 
`using adeven.AdjustIo` to `using Adjust`.

They should be in the file `App.xaml.cs` of your app, as well, whenever you track an
event and/or revenue. Keep this files open for the next step.

### 5. Replace the static method invocations

In the same files where the namespace was replaced, change the static invocations of
the class `AdjustIo` to `Adjust`. For example, from `AdjustIo.TrackRevenue(1.5)` to
`Adjust.TrackRevenue(1.5)`.

### 6. Replace the Environment enum references

In the `App.xaml.cs` file, where the method `SetEnviornment` is called after the 
`AppDidLauch` method. Change the reference of the enum `Environment` from
`AdjustIo.Environment.Production` to `AdjustEnvironment.Production`. This example
is for the Production environment. Use the Staging for testing.

### 7. Replace the LogLevel enum references

In the `App.xaml.cs` file, if you use the `SetLogLevel` method to change the default
logging level, remove the `AdjustIo` namespace prefix. For example, from
`AdjustIo.LogLevel.Info` to `LogLevel.Info`.

[adjust.com]: http://www.adjust.com
[nuget_click]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/01_nuget_console_click.png
[nuget_migration_uninstall]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/02_nuget_migration_uninstall.png
[nuget_migration_install]: https://raw.github.com/adjust/adjust_sdk/master/Resources/windows/03_migration_install.png

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
