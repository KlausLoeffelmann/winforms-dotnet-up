---
name: warp-api-decision-guide
description: >-
  Selects relevant WARP packages and skills for a whole WinForms feature before
  implementation, including navigation, settings, typed input, AI chat, Git,
  document editing, hardware-accelerated dashboards and legacy map experiments.
  Routes ordinary framework operations directly to their WinForms specialists.
---

# WARP feature selection

## Critical constraints

- This is the **sole cross-domain WARP router**. Select focused owners, not every skill.
- Carry selected skills' critical constraints into the working checklist. Re-open
  their SKILL.md after context compaction, resuming work or changing phases.
- WARP source and installed NuGet APIs can differ. Inspect the consuming version
  and public surface; historical preview versions are not a “latest” recommendation.
- Shared framework rules belong to the named WinForms owners below. References
  are by skill name, not assumed filesystem paths between installations.
- Keep platform-neutral ViewModels separate from WinForms implementations; keep
  Git composition data-preserving and native Maps explicitly experimental.

## Workflow

Identify the domain, select the entry below, inspect dependencies and API availability,
then load framework specialists only for the related layout, Designer, threading,
binding or packaging work. Use `winforms-api-decision-guide` only as a fallback
when no direct framework row fits.

## Domain → owner

| Requirement | WARP package / entry point | Skill |
|---|---|---|
| New app or retrofit to DI/configuration/logging/host lifetime | WinForms.AppServices; `Microsoft.Extensions.WinForms.WinFormsApplication.CreateBuilder` | `warp-winforms-application-builder` |
| ViewModel confirmation/input/selection/modal dialog; settings, API keys, exception hooks | Desktop.AppServices + WinForms.AppServices; `IDialogService`, `IUserSettingsService`, `EnvironmentVariableKeyService` | `warp-app-services` |
| Navigation/settings/diagnostics tabs | WinForms; `FluentTabControl`, one UserControl per tab | `warp-fluent-tab-control` |
| Tools/Options categories and ergonomic setting editors | WinForms; `FluentTabControl` plus standard or WARP inputs | `winforms-options-dialog` + `warp-fluent-tab-control`; persistence uses `warp-app-services` |
| First-run/account/setup wizard | WinForms; `WizardContainer.AddWizardPage` | `warp-wizard` |
| UI-side themed messages | WinForms; `FluentMessageBox` | `warp-messaging` |
| File/folder/save picker, bound selection, fractional slider, date/time/money formatting | WinForms + optional WinForms.Async; `FilePathPicker`, `BindableComboBox`, typed formatter components | `warp-input-controls` |
| Generated-property editor scaffolding | WinForms; experimental AutoView types, not a complete generator | `warp-binding-autoview` |
| Adorned cells, validation border, click-through overlay | WinForms; `AdornerPanel`, `AdornerTableLayoutPanel`, `TransparentPanel` | `warp-containers-layout` |
| Template cards or classic data columns with WARP theming | WinForms + Specialized; `GridView`, `WarpDataGridView` | `warp-data-grids` |
| Designer-extender command components | WinForms; experimental `UiCommandBase`, `CommandTypeProvider` | `warp-commands-mvvm` |
| Control traversal, existing-binding converter, form geometry, font metrics, typed modal helper | WinForms.Extensions | `warp-winforms-extensions` |
| Font/SVG/bitmap symbol assets, accessible metadata, renderer/cache contracts | WinForms + WinForms.Drawing; `FluentSymbols`, `ISymbol`, `SymbolBitmapCache` | `warp-symbols` |
| Symbol ToolStrip/MenuStrip items or enum-helper command/icon configuration | WinForms + WinForms.Extensions; `SymbolToolStripButton`, `ConfigureItem` | `warp-toolstrip-icons` |
| In-process console output, byte dumps, unique export paths | WinForms.Specialized; `ConsoleControl`, `HexAsciiDumper`, `FilenameDisambiguator` | `warp-console-diagnostics` |
| Accelerated sensor/telemetry dashboard or custom drawing | WinForms.DirectX; `D2DControl`, `D2DPanel`, `ID2DGraphics` | `warp-directx-getting-started` |
| Retained animation/sprites/layered kiosk scene | WinForms.DirectX; `D2DVisual`, `PaintContent`, batched commits | `warp-directx-using-visuals` |
| All-DComp fullscreen/kiosk top-level without classic child controls | WinForms.DirectX; `D2DForm` exclusivity | `warp-directx-dcomp-exclusive-tlw` |
| Plain text, classified code, raw/rendered Markdown, editor options/document persistence | WinForms.DirectX + optional Roslyn | `warp-directx-text-editors` |
| Squiggles, dictionary discovery/corrections, chat-prompt spelling | Text.Spelling + optional Windows.Spelling source + DirectX | `warp-text-spelling` |
| Provider components, prompt/schema templates, streaming paragraphs/code, chat/history | Desktop.AI + Microsoft.Extensions.AI + WinForms.AI/Chat; verify current component model | `warp-winforms-ai` |
| GitHub auth/Octokit, repository inspection, safe integration branches | WinForms.Github | `warp-git-and-github-api` |
| Explicit legacy Windows native/offline Maps experiment | Experimental project brief, not a shipping abstraction | `warp-windows-maps-experimental` |

Package names above abbreviate the `WarpToolkit.` prefix.

## Direct framework routes

| Related operation | Canonical skill |
|---|---|
| Project/startup without WARP hosting | `winforms-development` |
| Modal/UserControl layout, DPI, grouped fields, button footer, scrolling/fullscreen layout | `winforms-high-dpi-fluent-layout` |
| Shared options categories, value/picker layout, typed setting selection | `winforms-options-dialog` |
| Shared icon-first/mixed-item ToolStrip, keyboard and accessible command rules | `winforms-toolstrip-icons`; WARP implementation uses `warp-toolstrip-icons` |
| Designer serialization and InitializeComponent | `winforms-designer-code` |
| InvokeAsync, async form/TaskDialog presentation | `winforms-async-apis` |
| Native COM apartment and cross-thread lifetime | `winforms-com-apartments` |
| New/derived control and Designer properties | `winforms-custom-and-usercontrols` |
| BindingSource, collections, validation, master-detail | `winforms-databinding` |
| Commands/DataContext/CommunityToolkit ViewModels | `winforms-mvvm` |
| GDI/GDI+ painting and resource ownership | `winforms-rendering` |
| .NET11-specific API/version gates | `winforms-net11-apis` |
| Package identity/MSIX/Store capabilities | `winforms-store-packaging` |
| Framework map technology choice/deprecation | `winforms-windows-maps` |

## Additional package capabilities

These retain useful routes without inventing absent specialty skills:

- `WarpToolkit.WinForms.Typography`: `ITypographyRenderer`, blocks/inlines,
  bulleted paragraphs and Markdig integration for custom formatted/Markdown layout.
- `WarpToolkit.WinForms.Async`: `AsyncButton`, `AwaitableForm`, `AwaitableEvent<T>`,
  `ContentFreezePanel`; inspect the installed public contracts for async clicks,
  closing waits and busy overlays.
- `WarpToolkit.Desktop.Roslyn`: comment-preserving document/member rewrites,
  `ReplaceCommentsByGuids` / `ReplaceGuidsByComments`. For whole VB→C# migration,
  drive `VisualBasicConverter.ConvertText` through a CLI/MSBuildWorkspace and
  project-wide collectors/WithEvents collapse pass; inspect `ConversionOptions`,
  `ConversionResult.HasManualWork`, `Manual` diagnostics and `// TODO(vb-convert):`.
  The converter specialty skill is not included in this installation; do not
  hand-translate a whole project or claim its helpers exist in every package.
- `WarpToolkit.WinForms.Roslyn`: `RoslynDocumentView` for classified documents.
- `WarpToolkit.Microsoft.Extensions`: designer-aware logging; verify
  `AddTimeStampedDebug`, `AddWinFormsFileLogger`, `UseWinFormsLogging` in the version used.
- `WarpToolkit.Windows.Interop.PrecisionTimer`: `HighPrecisionTimer`.
- `WarpToolkit.ComponentModel.IServiceProviderAssignable` +
  WinForms.Design `ServiceProviderAssignableComponentCodeDomSerializer`:
  Designer-aware provider assignment; follow builder initialization timing.
- `WarpToolkit.Windows.Bluetooth`: discovery/radio and protocol-neutral RFCOMM/GATT
  byte transports; WinForms.Bluetooth adds `BluetoothServicesComponent` and
  `BluetoothDeviceListView`. Do not assume protocol or manifest capabilities.

Reference provenance: WARP package API references under `src\docs\reference`;
local audit baseline `0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5`. Source-only APIs
and publication status require checking against the consuming package.

## Final checks

1. Selected domain owner and necessary framework specialists are in the checklist.
2. Entry points/package versions actually exist in the consuming build.
3. No competing broad router, silent fallback or cross-root file-link assumption.
