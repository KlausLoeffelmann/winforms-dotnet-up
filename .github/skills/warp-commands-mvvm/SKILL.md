---
name: warp-commands-mvvm
description: >-
  Configures WARP experimental ICommand components and Designer command extenders,
  refreshes CanExecute state and opens Forms or messages through reusable command
  objects assigned to Buttons or ToolStrip items.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\Experimental\\CommandTypes"
---

# Experimental command components

## Critical constraints

- Package `WarpToolkit.WinForms`; namespace
  `WarpToolkit.WinForms.Experimental.CommandTypes`. Pin and inspect this evolving API.
- `UiCommandBase.Execute` is abstract; enablement changes require
  `RaiseCanExecuteChanged()`.
- After extender assignment, call `WireUpCommands()` in regular code, not
  InitializeComponent. Do not add a second Click dispatch to an already-bound command.
- Read `winforms-mvvm` for framework/CommunityToolkit commands and
  `winforms-designer-code` for serialization; not every command needs these components.

## Contracts and workflow

`IUiCommand : ICommand, INotifyPropertyChanged, IDisposable` adds
`string CommandName { get; set; }` and `void RaiseCanExecuteChanged()`.
`UiCommandBase : Component, IUiCommand` is Designer-droppable:

| Member | Contract |
|---|---|
| `CanExecute(object? parameter)` | virtual bool |
| `Execute(object? parameter)` | abstract void |
| `RequestCanExecute` | EventHandler&lt;RequestCanExecuteEventArgs&gt;? |
| `CanExecuteChanged`, `PropertyChanged` | standard notification events |

`RequestCanExecuteEventArgs(object? parameter)` exposes
`object? CommandParameter` and writable `bool CanExecute`. Handle this request
instead of subclassing when a simple predicate suffices.

`FormCommand` adds `Type? FormType`; `MessageBoxCommand` adds
`MessageText`, `Caption` (string?), `Buttons`, `Icon`. Both override execution;
FormCommand also overrides CanExecute. Manual Execute is valid only when the
target is **not** already wired by command machinery.

`CommandTypeProvider : Component, IExtenderProvider` offers CanExtend;
Get/SetUICommandType(Component, Type?); Get/SetUICommand(Component, UiCommandBase?);
ShouldSerialize/Reset helpers for both extended properties; and WireUpCommands.

```csharp
// Regular configuration fragment; saveCommand is an existing UiCommandBase.
commandProvider.SetUICommand(saveButton, saveCommand);
commandProvider.WireUpCommands();
// After the document state changes:
saveCommand.RaiseCanExecuteChanged();
```

Custom commands implement Execute and either override CanExecute or handle
RequestCanExecute. Keep async domain operations in a suitable Task-aware command
rather than claiming this void API automatically awaits them.

## Designer converters

`UiCommandInstanceConverter` lists command instances, `UiCommandTypeConverter`
lists command types, `FormsTypeConverter` lists Forms for FormType.
They support Designer selection/serialization, not normal runtime invocation.
Nonvisual commands have no own DPI/theme work; target controls do.

## Final checks

1. Installed experimental API matches the assigned types.
2. Exactly one dispatch path executes each command.
3. CanExecute notifications and component disposal have an owner.
