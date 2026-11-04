# Deployment checklist

## Inspect and preserve

Record SDK-style versus legacy project format, TFM/Windows target, Directory.Build settings, platform/RIDs, installer, executable identity/assets, package identity, signing/version conventions and native dependency support. Use supported current Visual Studio workloads/tooling; don't add packaging infrastructure incompatible with the solution.

Prefer separate packaging ownership when using a dedicated packaging project: reference startup application payload; manifest/assets/capabilities/architecture/version/signing belong there. Single-project/equivalent tooling may organize this differently when supported; do not duplicate sources or insist one project style fits all.

## Runtime and architectures

- Explicit x64/ARM64 application and package builds; architecture-specific publish payloads and packages.
- Audit COM servers/native DLLs/PInvoke targets, graphics/media, WebView2/runtime and helper processes for each architecture.
- Test ARM64 natively; x64 emulation success does not prove ARM64 package correctness.
- Bundle packages when the workflow calls for architecture selection; x86 only if the product requires it.
- Framework-dependent .NET runtime acquisition must use a supported deployment route and be tested on clean machines. Package dependency declarations alone are not a magic arbitrary runtime installer.
- Self-contained builds include architecture runtime files and increase package size. Verify single-file, extraction and native payload compatibility for the selected packaging technology.
- Preserve supported TFM unless a required Windows contract/tooling change demands otherwise.

## Identity, capabilities and assets

Full MSIX supplies conventional packaged identity/deployment. Packaging with external location adds identity around an existing install location; it is not the same as a full packaged Store app. Store MSI/EXE distribution is a separate route.

Manifest/package identity must match Partner Center reservation where Store-bound. Use actual publisher/display/version/logo/tile assets; never invent Store association or credential values. Declare only required capabilities/protocols/associations/aliases. Cite each identity/capability-gated feature and provide actionable unpackaged-debug diagnostics.

Preserve desktop manifest DPI, execution level, compatibility and other needed executable declarations. Introducing a package manifest does not automatically replace every desktop manifest role.

Check minimum Windows version, privacy implications and permissions. Installation directory is not writable application data; settings/cache/log paths must follow supported user-data policy. Full-trust packaged desktop applications do not automatically have identical storage/API semantics to UWP.

## Signing, Store and validation

Use appropriate development certificate/local trust, CI secrets and Store signing paths. Never embed private keys/credentials in code or fabricate an authorized submission. Versions and identity must support intended upgrade strategy.

Validate supported unpackaged launch, x64/ARM64 package build, optional bundle, install/launch, upgrade, uninstall/reinstall, files/settings, shell associations, native/COM and identity-gated APIs. Check clean runtime acquisition, architecture-dependent dependencies and install-directory assumptions.

Run WACK/current Store validation when available. State missing hardware, signing trust, tooling or Partner Center association/credentials as explicit manual boundaries.

Deliver exact changed files/projects, model rationale, workloads/tool assumptions, capabilities, architecture matrix actually built/tested, local signing instructions and remaining Store steps.

Sources: [MSIX desktop packaging](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net), [external-location identity](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/grant-identity-to-nonpackaged-apps), [Store desktop publication](https://learn.microsoft.com/en-us/windows/apps/publish/).
