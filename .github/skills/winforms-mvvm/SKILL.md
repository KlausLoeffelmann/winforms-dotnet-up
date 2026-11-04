---
name: winforms-mvvm
description: >-
  Separates testable customer/preferences screen state and service work from
  WinForms, migrates business-heavy event handlers to commands, and binds
  command-enabled buttons/menus. Applies to MVVM Toolkit properties, dependent
  values, CanExecute, loading/error/cancellation state, inherited DataContext,
  shared ViewModels/collections, dependency injection and ViewModel tests.
---

# ViewModels, commands and views

## Durable rules

- Models/services own domain and persistence; ViewModels own presentation state/commands; views own controls, dialogs, cursor and window results.
- Keep reusable ViewModels UI-stack-independent. A separate library promotes reuse/testability, but source generators **can work in a WinForms project**.
- Bind one action through commands or an event path deliberately, not duplicated execution.
- Notify dependent values and `CanExecute` when state changes; async command failures/cancellation need an explicit policy.
- ViewModel and collection notifications observed by views remain UI-affine, including cross-framework shared models.

## Platform and project boundaries

.NET 8 introduced built-in `Control.DataContext`, `ButtonBase.Command`, `ToolStripItem.Command`/`CommandParameter`, and `BindableComponent` support. Commands update enabled state using `CanExecute`; context is ambient, commands are explicitly assigned/bound. MVVM itself is possible on older targets with explicit wiring.

Typical solution: WinForms presentation, portable ViewModels, models, services. Toolkit generator package must be installed/configured in the project **compiling annotated code**; source-generator configuration does not automatically cascade between projects. Use the project's compatible pinned `CommunityToolkit.Mvvm` version.

Read [coherent ViewModel/view example](references/mvvm-example.md) when generating properties/commands, parameter actions, inherited-context views, or unit tests.

## Binding and composition

Designer uses `BindingSource.DataSource=typeof(ViewModel)`; regular constructor/Load/context change assigns actual instance. Build generated properties before Designer discovery. [Databinding](../winforms-databinding/SKILL.md) owns datasource XML, writeback, validation, currency and adapters.

`DataContext` flows to children but is not automatic property-path wiring. A child may react in `OnDataContextChanged`, replace/null its BindingSource and preserve bindings instead of rebuilding them on every change. Handle early construction callbacks and detach old subscriptions.

Shared `ObservableCollection` requires a declared WinForms adapter; the supplied one-way structural adapter is suitable for source-command add/remove. Use BindingSource.Current/CurrentChanged or grid row `DataBoundItem` for current selection, not nonexistent `DataGridView.SelectedItem`. Recreate/detach adapter when the source collection property is replaced.

## Migration and testing

Move persistence/business work out of Click handlers into injected services and commands. ViewModels expose error/loading state; views observe it for MessageBox/cursor/dialog-close behavior. Unsubscribe on disposal and don't update controls from worker-raised notifications. UI-specific event handlers remain appropriate; “pure view” does not mean no code-behind.

Test dependent properties, boundary `CanExecute`, service calls, loading reset, faults and cancellation without Form construction. Also test actual UI binding/selection/lifetime; ViewModel tests do not replace UI integration tests.

Generator troubleshooting: package/compiler support, partial annotated type, diagnostic errors, generated output and rebuild. Command enablement: missing `NotifyCanExecuteChanged`/Toolkit notification attribute. Discovery: build correct assembly/type metadata in supported VS tooling—not a promise about VS Code.

## Final checks

1. Commands execute once, notify enabled state, and surface errors/cancellation.
2. Views detach old sources/subscriptions and honor inherited/null context safely.
3. ViewModel tests and relevant binding/UI lifetime checks cover the actual feature.
