### Version 4.12.1 (19th April 2018)
#### Added
- Added integration tests support to the repository.
- Added `initiated_by` flag indicating whether SDK or backend initiated attribution request.

---

### Version 4.12.0 (13th December 2017)
#### Added
- Added new `click label` parameter in attribution.
- Added reading of responses to `sdk_click` packages.
- Added `SetDeviceKnown` method in `AdjustConfig` class.
- Added `EventTrackingSucceeded` property on `AdjustConfig` object for setting a callback to be triggered if event is successfully tracked.
- Added `EventTrackingFailed` property on `AdjustConfig` object for setting a callback to be triggered if event tracking failed.
- Added `SesssionTrackingSucceeded` property on `AdjustConfig` object for setting a callback to be triggered if session is successfully tracked.
- Added `SesssionTrackingFailed` property on `AdjustConfig` object for setting a callback to be triggered if session tracking failed.
- Added deferred deep link callback `DeeplinkResponse` with decision whether deep link should be launched or not.
- Added background tracking feature.
- Added support for suppress log level.
- Added possibility to delay first session.
- Added support for session parameters to be sent in every session/event.
- Added possibility to inject custom user agent to each request.
- Added sending of push token with dedicated package called `sdk_info`.
- Added `adid` field to the attribution callback response.
- Added method `Adjust.GetAdid()` to be able to get `adid` value at any time after obtaining it, not only when session/event callbacks have been triggered.
- Added methd `Adjust.GetAttribution()` to be able to get current attribution value at any time after obtaining it, not only when attribution callback has been triggered.
- Added check if `sdk_click` package response contains attribution information.
- Added sending of attributable parameters with every `sdk_click` package.
- Added reading of network type.
- Added reading of connectivity type.
- Added log messages for saved actions to be done when the SDK starts.
- Added usage of app secret in authorization header.

#### Changed
- Removed `Windows Phone 8.0` support.
- Sending `sdk_click` immediately with a dedicated handler.
- Firing attribution request as soon as install has been tracked, regardless of presence of attribution callback implementation in user's app.
- Replaced `assert` level logs with `warn` level.
- Removed dependency to `PCL Storage`.
- Not sending `sdk_click` packages when SDK is disabled.
- Setting enable/disable or offline/online is now queued.
- Guaranteeing that first package is sent even with event buffering turned ON.
- Not creating first session package if SDK is disabled before first launch.
- `Adjust.SetupLogging` method is osbsolete. Use `AdjustConfig` constructor to set up logging instead.

---

### Version 4.0.3 (2nd February 2016)
#### Added
- Added access to Windows Advertising Identifier with `GetWindowsAdId` method.

---

### Version 4.0.2 (6th October 2015)
#### Added
- Added Windows 10 target.
- Added sending of information from `EasClientDeviceInformation` library.

---

### Version 4.0.1 (10th September 2015)
#### Added
- Added possibility to set SDK prefix in config object.

---

### Version 4.0.0 (10th September 2015)
#### Added
- Added config objects to initialise SDK.
- Added event object to track events.
- Added sending of currency and revenue with event.
- Added support for callback and partner parameters appending.
- Added offline mode feature.
- Added Criteo plugin.

#### Changed
- Replaced response data delegate with attribution changed delegate.

---

### Version 3.5.1 (19th November 2014)
#### Fixed
- Fixed "umlaut" charachters in user agent parsing.

---

### Version 3.5.0 (4th September 2014)
#### Added
- Added `Nuget` package built with `Release` configuration.
- Added possibility to pass a method to SDK to see the SDK logs.

---

### Version 3.4.2 (4th September 2014)
#### Added
- Added support for `Windows Phone 8.1 Apps` and `Universal Apps`.

---

### Version 3.4.0 (1st August 2014)
#### Added
- Added support for deferred deep linking.
- Added new response fields with tracker information.

#### Fixed
- Fixed user agent escaping error.

---

### Version 3.3.2 (12th May 2014)
#### Added
- Added possibility to set SDK prefix (to be used by non-native SDKs).

---

### Version 3.3.1 (9th May 2014)
#### Added
- Added support for Unity3d.

#### Fixed
- Fixed disposed `HttpClient` object.

---

### Version 3.3.0 (2nd May 2014)
#### Added
- Added option to disable SDK.
- Added deep link parameters sending.

---

### Version 3.0.0 (24th February 2014)
#### Added
- Added delegate to support in-app source access.

#### Changed
- Renamed `AdjustIo` class to `Adjust`.

---

### Version 2.1.0 (27th January 2014)
#### Added
- Added session aggregation.
- Added meta information for sessions and events.
- Added offline tracking.
- Added persistance to local storage.
- Added multi threading.
- Added event buffering.
- Added sandbox environment.

---

### Version 1.0 (14th December 2012)
#### Added
- Initial Windows SDK release.
