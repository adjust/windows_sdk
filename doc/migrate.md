## Summary

This is the Windows SDK of Adjust. You can read more about it at
[adjust.com][].

## Basic Installation

These are the minimal steps required to integrate the Adjust SDK into your
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

