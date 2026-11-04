---
name: winforms-windows-maps
description: >-
  Chooses or troubleshoots WinForms maps, geocoding/routes, downloaded offline
  map packages and package-identity/capability failures. Separates deprecated
  Windows.Services.Maps/native offline experiments from modern online Azure
  Maps/WebView2 integration. Applies when offline operation must be verified
  without network access or a library must report unavailable map prerequisites.
---

# Maps: modern online versus gated legacy native

## Durable rules

- `Windows.Services.Maps.*` and UWP `MapControl` are deprecated/Bing-dependent; Windows Maps app removal began July 2025. Do not imply durable service availability.
- Offer Azure Maps/WebView2 for appropriate **online** scenarios, not as a silent substitute for requested native/offline requirements.
- Isolate retained native experiments behind explicit user acceptance, platform/identity/capability guards and actionable status reporting.
- Package identity belongs to the host; reusable code can diagnose it but must not silently package the app.
- Offline claims require real-device testing with connectivity disabled, not just an offline preference flag.

## Scenario choice

| Need | Contract |
|---|---|
| Online displayed map/routes | Evaluate current Azure Maps service and WebView2 hosting, credentials/licensing/privacy/network policy and architecture/runtime deployment |
| Existing native Windows geocoding/routing/offline experiment | Verify deprecated API availability and service behavior first; retain only in explicit legacy/experimental boundary |
| Durable offline vector/tile/raw-road access | Native Windows APIs do not promise raw downloaded database enumeration; evaluate a provider with an explicit supported offline contract |

Read [platform/verification notes](references/platform.md) before implementing any Maps integration. Current official prerequisites and provider lifecycle override old assumptions.

Modern .NET consumes supported WinRT APIs through a Windows-versioned TFM; custom projections/handwritten COM require a genuine interop need. Inspect solution target/analyzers/signing/architectures and preserve conventions. Stable portable libraries should not accidentally acquire a deprecated Windows dependency.

Legacy API inventory: `MapService`, `MapServiceDataUsagePreference`, `MapLocationFinder`, `MapRouteFinder`, `OfflineMaps.OfflineMapPackage`, geolocation `Geopoint`. Package identity/capability requirements differ by API. Check current desktop-support documentation, not “WinRT compiles so every API works.”

`OfflineMapDataOnly` is a preference, not proof that required packages exist or operations succeed. Changes to MapService process/platform state must be scoped/restored when needed.

## Final checks

1. State provider, native/online/offline boundary, current deprecation and prerequisite diagnostics.
2. Unit-test conversion/guards/status mapping; separate service/device-dependent live tests from ordinary CI.
3. Report identity/manifest changes, x64/ARM64 coverage, manual package/network setup and unverified behavior.

Sources: [Maps deprecation](https://learn.microsoft.com/en-us/windows/uwp/maps-and-location/), [Azure Maps](https://learn.microsoft.com/en-us/azure/azure-maps/), [WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/).
