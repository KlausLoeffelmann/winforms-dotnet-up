# Experimental Windows Maps assembly: retained project brief

Read before implementing the independently activated experiment. This is a
requirements brief, not a statement that WARP already implements these services.

## Contents

- Repository integration and platform
- API experiments and statuses
- Identity and capabilities
- Proposed abstraction boundary
- Test host and deterministic CI
- Documentation and completion gates

## Repository integration and platform

Inspect the consuming repository's existing solution/projects **for implementation**.
Follow shared props/targets, project settings, language/nullable/analyzers, TFM,
signing/package metadata, namespaces, source layout and Windows/WinForms model.
Choose the existing WarpToolkit naming convention, not a parallel “WARP.Experimental”
style unless that actually matches. Add the project to appropriate solution/build/test
infrastructure without stable-to-experimental dependencies.

Explicitly Windows-only/experimental: isolate deprecated Windows.Services.Maps
contracts, provide support/status reports and graceful failure, document no
long-term platform commitment. Bing retirement/API deprecation and Windows Maps app
removal make old installation/availability assumptions unsafe.

Use modern Windows-targeted .NET WinRT projection. Preserve shared TFMs where
possible; make a minimum necessary Windows platform-target adjustment only for the
experiment and explain SDK/contract requirements. Manual COM only for a genuinely
unprojected operation.

## API experiments and statuses

Explore where available:
Windows.Services.Maps.MapService, MapLocationFinder, MapRouteFinder,
OfflineMaps.OfflineMapPackage, MapServiceDataUsagePreference and relevant
Windows.Devices.Geolocation types. First iteration is service/data, not old MapControl.

1. Report API presence **and usability**.
2. Detect process package identity.
3. Find offline packages covering a point/area.
4. Report package display name/status/estimated size.
5. Request/observe download or update only where permitted.
6. Set/scope OfflineMapDataOnly for actual supported experiments; investigate
   process-global usage-preference effects and restore state safely if applicable.
7. Geocode and reverse-geocode.
8. Route between two Geopoint values.
9. Distinguish unavailable API, missing identity, missing offline data, unsupported
   operation and ordinary service/API failure.

Prefer Tasks/CancellationToken where underlying operations can reasonably adapt;
do not imply cancellation can interrupt every native/service operation.
Avoid leaking implementation-specific WinRT types unless doing so improves the
experimental value deliberately.

## Identity and capabilities

Several desktop Maps APIs require package identity; determine this per operation,
not merely once for the namespace. Hosting/packaging remains a consumer concern.
Provide detection/actionable diagnostics and a small WinForms host example:

- Full MSIX is the preferred fully packaged Store submission model.
- Packaging with external location/sparse identity can retrofit installer-based
  desktop apps but is not the same submission model as full MSIX.
- Verify/document manifest capability needed by OfflineMapPackage management;
  package identity alone does not establish authorization/support.

Do not silently add packaging or stable deprecated-API dependencies.
Online Azure Maps/WebView2 migration must explicitly preserve or disclose changes to
identity/offline behavior, credentials, service availability and data semantics.

## Proposed abstraction boundary

Illustrative **nonshipping** names:

```text
MapPlatformSupport: IsApiPresent, HasPackageIdentity, GetSupportReport
OfflineMapService: FindPackagesAsync, GetPackageStatusAsync
MapGeocodingService: FindLocationsAsync, ReverseGeocodeAsync
MapRoutingService: FindDrivingRouteAsync
MapExperimentOptions: PreferOfflineData / OfflineOnly
```

Adapt these to repository conventions. Support reports distinguish metadata,
identity/capability, installed data and real operation results; no fake success.

## Test host and deterministic CI

Smallest appropriate WinForms sample/test harness, with canonical shared layout
owned by `winforms-high-dpi-fluent-layout` and Designer organization by
`winforms-designer-code`. It reports identity, offline data, OfflineMapDataOnly,
geocoding/routes with networking disabled and exact missing-requirement statuses/
exceptions.

CI must not require Maps/Bing services or installed offline data. Unit-test
deterministic wrappers/conversions; mark live platform/service checks manual/integration.
Verify currently supported ways to acquire installed data. Do not instruct users
to open removed Maps apps or guaranteed Settings downloads.

## Documentation and completion gates

Experimental README should explain purpose/deprecation, Windows version/contracts/
SDK, identity and manifest capabilities, reference/host integration, full/sparse
identity retrofit, current offline-data acquisition limitations, truly disconnected
testing, service failures and the absence of arbitrary raw offline map database/
vector-tile access.

Before claiming completion:

1. Build the consuming complete solution for normally supported architectures/
   configurations and verify existing projects are unaffected.
2. Build experimental x64 and ARM64 wherever supported.
3. Run applicable deterministic tests; report manual platform checks separately.
4. Report added/changed files and SDK/identity/capability assumptions.
5. Label offline functionality requiring manual verification unless actually
   exercised on Windows with network disabled.
