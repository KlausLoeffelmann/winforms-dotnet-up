---
name: warp-app-services
description: >-
  Registers WARP dialogs, exception hooks, user settings and provider-key services;
  implements ViewModel confirmation/input/selection/modal workflows and persists
  Form bounds, grid columns or splitters without coupling ViewModels to WinForms.
metadata:
  source: "WARP src\\WarpToolkit.Desktop.AppServices; src\\WarpToolkit.WinForms.AppServices\\Services; src\\WarpToolkit.WinForms.Extensions\\UI\\UIServiceExtensions.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Application services

## Critical constraints

- ViewModels reference `WarpToolkit.Desktop.AppServices` contracts, not the
  WinForms implementation; mock `IDialogService` in tests.
- Hosting/DI constructors and initialization timing belong to
  `warp-winforms-application-builder`, not a second bootstrap recipe here.
- Keep keys out of source; distinguish missing-key discovery from exceptional failure.
- Centralize feature exception hooks through `IWinFormsAppExceptionService`;
  use `ISyncContextService` for UI coordination rather than reading OpenForms in a VM.
- Shared modal layout and async boundaries belong to
  `winforms-high-dpi-fluent-layout` and `winforms-async-apis`.
- For Tools/Options, load `winforms-options-dialog` and `warp-fluent-tab-control`.
  Use separate category UserControls with the shared ergonomic picker/grouping
  rules; save validated accepted drafts only, not cancelled dialog/picker state.

## Registration and contracts

Host packages: `WarpToolkit.WinForms.AppServices` + Desktop.AppServices.
Contract `IUserSettingsService` is in `WarpToolkit.ComponentModel`.

```csharp
using WarpToolkit.WinForms.AppServices.ServiceExtensions;

builder.Services.AddWinFormsDialogService()
    .AddWinFormsExceptionService()
    .AddWinFormsUserSettingsService()
    .AddLocalKeyRetrievalService();
```

The settings registration accepts Action<WinFormsUserSettingsServiceOptions>
(options namespace WarpToolkit.WinForms.AppServices), including SaveMode =
UserSettingsSaveMode.Debounced (enum in ComponentModel). DI disposal flushes pending
debounced/flush-on-dispose changes; do not dispose the shared settings store from a view.

| IDialogService operation | Result |
|---|---|
| `ShowMessageAsync(message, title)` | Task |
| `ShowWarningAsync(message, title)` / `ShowErrorAsync(message, title)` | Task |
| `ShowConfirmationAsync(message, title)` | Task&lt;bool&gt; |
| `RequestInputAsync(message, title, defaultValue = "")` | Task&lt;string&gt;; current WinForms implementation returns empty on Cancel |
| `SelectFromAsync<T>(message, title, options, defaultOption = default)` | Task&lt;T&gt;; inspect cancellation/selection behavior for the consumer |
| `ShowDialogAsync<TViewModel>(vm)` | Task&lt;IModalDialogResult&lt;TViewModel&gt;&gt;; VM must be class + INotifyPropertyChanged |

The full-dialog implementation resolves a **keyed Form using `vm.GetType()`**,
sets DataContext through the typed helper, and returns `ReturnValue` plus
`DialogCloseReason`—not a tuple or WinForms DialogResult.
The current dialog service is registered singleton and uses its captured provider;
do not assume this automatically resolves a view in the startup Form's scope.
Register/own keyed views with that lifetime boundary in mind.

```csharp
using WarpToolkit.Desktop.AppServices;

bool delete = await dialogs.ShowConfirmationAsync(
    "Delete the selected customer?", "Delete customer");
if (delete)
{
    await repository.DeleteSelectedAsync();
}
```

Use Task-returning service/RelayCommand consumers, not async void VM methods.
The implementation uses TaskDialog for messages; ordinary UI code can instead
use `warp-messaging`.

## Provider keys

`EnvironmentVariableKeyService` implements `IKeyRetrievalService` and
`ILocalKeyRetrievalService`; `WellKnownAIProviders` is in Desktop.AI.
Registration prepopulates provider environment-variable names.
Current AddLocalKeyRetrievalService registers ILocalKeyRetrievalService, not the
concrete EnvironmentVariableKeyService; inject the registered interface rather than
assuming the concrete class is DI-resolvable. SetAiProviderKey defaults Process;
choose User/Machine explicitly when persistence is intended and permitted.

```csharp
using WarpToolkit.Desktop.AI;

string apiKey = keys.GetAiProviderKey(WellKnownAIProviders.OpenAI);
keys.SetAiProviderKey(WellKnownAIProviders.OpenAI, suppliedKey,
    EnvironmentVariableTarget.User);
```

Use `TryGetAiProviderKey` for unconfigured keys so the UI can offer setup.
`GetAiProviderKey` can throw `EnvironmentVariableNotFoundException`.
An OS credential store can be accessed through a suitable remote retrieval
implementation; the interface is not a promise of a shipped vault.

## Saved UI state

Inject settings; import `WarpToolkit.WinForms.Extensions.UI`. Apply after controls
exist and save at an appropriate successful-close/settings boundary.

| State | Save / restore helper |
|---|---|
| Form bounds | `SaveFormBounds` / `TryApplyFormBounds` |
| DataGridView column widths | `SaveDataGridViewColumnWidths` / `TryApplyDataGridViewColumnWidths` |
| ListView column widths | `SaveListViewColumnWidths` / `TryApplyListViewColumnWidths` |
| SplitContainer distance | `SaveSplitterDistance` / `TryApplySplitterDistance` |
| Richer Form placement | `SaveFormPlacement` / `ApplyFormPlacement`; inspect parameters before adoption |

```csharp
settings.TryApplyFormBounds(this, key: nameof(MainForm));
settings.TryApplyDataGridViewColumnWidths(grid, key: "Grid.Customers");
// At the chosen save boundary:
settings.SaveFormBounds(this, key: nameof(MainForm));
settings.SaveDataGridViewColumnWidths(grid, key: "Grid.Customers");
```

Use stable per-view/control keys. Geometry helpers return restorable bounds;
framework layout remains responsible for working-area/DPI validity.

## Exception hook

`exceptions.RegisterExceptionHandler(OnUiException)` connects feature handling
through `IWinFormsAppExceptionService`; handler takes `ThreadExceptionEventArgs`.
Do not attach competing feature hooks directly to `Application.ThreadException`.
This does not eliminate async event try/catch or native lifetime requirements.

Read `warp-winforms-ai` only for AI consumers and `winforms-mvvm` /
`winforms-databinding` only for related ViewModel/binding work.

## Final checks

1. ViewModel contracts remain framework-neutral; full-dialog keyed view exists.
2. Missing keys/settings produce an actionable path, not silent success.
3. Saved state, exception hooks and scoped views have explicit lifetime owners.
