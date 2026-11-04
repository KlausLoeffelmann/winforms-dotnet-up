# Maps platform and verification boundaries

## Native deprecated path

Current baseline: UWP MapControl and Windows.Services.Maps are deprecated and depend on Bing platform history; Windows Maps app removal began July 2025. Before retaining a native experiment, confirm API availability, service/token requirements, OS behavior and installed offline-data support on the actual machine.

Relevant APIs: MapService/DataUsagePreference, MapLocationFinder, MapRouteFinder, OfflineMaps.OfflineMapPackage and Windows.Devices.Geolocation.Geopoint. Modern Windows-targeted TFMs can project supported WinRT APIs; handwritten COM/CsWinRT is not required for ordinary projected use.

Desktop support/identity is **per API**. Verify the documentation for selected classes and offline-map-management capability; do not assume package identity alone grants every operation. Check identity before identity-gated calls and return actionable diagnostics. Full MSIX is a normal Store-bound identity route; external-location identity is a separate existing-installer option, not equivalent packaging.

OfflineMapDataOnly requests offline data usage; it doesn't establish that data is installed, complete, permitted or usable. MapService options can be global/process/platform state; avoid surprising concurrent consumers and restore old preferences if implementing a scoped policy.

Downloaded platform map packages do not expose a supported arbitrary raw road/vector/tile database enumeration contract. Never claim a private database-reader feature from the presence of offline packages.

## Online replacement is a different contract

Azure Maps/WebView2 may suit online maps and routes, but changes service/provider, credentials, network, privacy/licensing, browser runtime and hosting behavior. Obtain current provider requirements, keep credentials appropriately protected, handle failed/offline navigation and audit x64/ARM64 runtime deployment.

Web hosting does not silently provide the old native offline package manager or a durable offline map database. If offline/native requirements cannot be fulfilled, state the gap and evaluate providers with documented offline capabilities explicitly.

## Thin API boundary and tests

Prefer Task-based service boundaries for support detection, offline-package management, geocoding, routing and options. Preserve useful WinRT statuses but translate predictable prerequisites into stable application/library failures:

- API/platform unavailable or deprecated service unsupported;
- missing identity;
- missing required capability;
- offline package unavailable;
- network unavailable when requested;
- no result/no route;
- service/platform failure.

Keep UI/host packaging separate from reusable service code. Unit-test options/conversions/guards/error mapping; ordinary CI must not depend on downloaded packages, map service endpoints, Store registration or machine Settings state.

Offline verification requires a real machine with known required packages and network disabled. Record OS/provider/package/capability conditions and route/search success or failure. Separate live tests; don't count a set preference with network still enabled as offline proof.

Report package/manifest changes separately from library code, manual setup, actual x64/ARM64 builds/tests and unverified runtime behavior.

Sources: [native Maps lifecycle](https://learn.microsoft.com/en-us/windows/uwp/maps-and-location/), [WinRT desktop support](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-supported-api), [OfflineMapPackage](https://learn.microsoft.com/en-us/uwp/api/windows.services.maps.offlinemaps.offlinemappackage).
