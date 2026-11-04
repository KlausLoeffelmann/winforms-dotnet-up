---
name: warp-windows-maps-experimental
description: >-
  Designs an explicitly experimental WARP Windows native/offline Maps assembly or
  test host, evaluates deprecated Bing-backed APIs, package identity and capability
  gates, and distinguishes real offline verification from online replacements.
metadata:
  status: "Experimental project brief; no shipping WARP Maps API is established"
  platform: "Windows-only; deprecated Windows.Services.Maps/Bing dependency"
---

# Legacy native/offline Maps experiment

## Critical constraints

- This preserves an **experimental project request**, not an existing stable assembly.
  Do not claim the suggested abstraction names are callable WARP APIs.
- UWP MapControl and Windows.Services.Maps APIs are deprecated and depend on
  retiring Bing services; Windows Maps app was removed in July 2025.
- Isolate legacy WinRT dependencies; stable WARP packages must not depend on the
  experiment. Unsupported API/service/data/identity conditions are normal outcomes.
- Package identity and operation-specific manifest capability are separate gates.
  Host packaging belongs to `winforms-store-packaging`; never package every consumer.
- Azure Maps/WebView2 is a modern **online** alternative, not a native/offline
  semantic substitute. Disclose changed dependencies and never silently fallback.
- Do not claim offline success without a real Windows test with networking disabled.

## Workflow and conditional reference

Read [Complete project brief and verification gates](references/project-brief.md)
before implementation. It retains integration, suggested service/data abstractions,
identity/MSIX/sparse distinctions, test host, documentation and architecture gates.
Read `winforms-windows-maps` for framework technology/deprecation choice;
`warp-winforms-application-builder` for a WARP host; `winforms-async-apis` for
WinRT-to-Task/cancellation and `winforms-com-apartments` only when native apartment
work is genuinely required. Use modern projected WinRT, not gratuitous manual COM.

Native areas to explore: MapService, MapLocationFinder, MapRouteFinder,
OfflineMaps.OfflineMapPackage, MapServiceDataUsagePreference and Geolocation.
Focus service/data APIs, not embedding deprecated UWP MapControl.

Report distinct API unavailable, package identity missing, offline data missing,
unsupported operation and ordinary service failure. Probe capability/usability,
not only metadata presence. Download/update access and OfflineMapDataOnly
behavior must be exercised; a policy flag does not establish offline data availability.
Do not assume current Windows Settings can still install maps after app/service retirement.

Official deprecation baseline:
https://learn.microsoft.com/en-us/windows/uwp/maps-and-location/
Verify operation-specific API contracts/capability/platform support against the
actual OS/SDK and service state; avoid guesses from old sample code.

## Final checks

1. Experiment remains isolated, capability-gated and explicit about deprecation.
2. Identity/capability/dependency changes are documented, with no silent online fallback.
3. Builds/tests/manual offline evidence match actual supported x64/ARM64 configurations.
