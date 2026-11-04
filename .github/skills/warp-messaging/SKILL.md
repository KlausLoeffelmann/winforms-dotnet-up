---
name: warp-messaging
description: >-
  Shows WARP themed messages and confirmations from UI code, corrects copied
  MessageBox owner-overload calls and customizes FluentMessageBoxForm when an
  explicit owner or asynchronous Form presentation is required.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\FluentMessageBox"
---

# Themed UI messages

## Critical constraints

- Package/namespace `WarpToolkit.WinForms`; FluentMessageBox uses
  **text first, caption second, no owner overload**.
- ViewModels use IDialogService through `warp-app-services`, not this static UI API.
- Customized dialogs use FluentMessageBoxForm and ordinary Form ownership.
- App-font/theme defaults are built in. Shared modal layout/async presentation
  belong to `winforms-high-dpi-fluent-layout` and `winforms-async-apis`.

## Workflow and overloads

```csharp
DialogResult result = FluentMessageBox.Show(
    "Discard unsaved edits?", "Unsaved changes",
    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
```

The four Show overloads return DialogResult and take text/caption, optionally
buttons, then icon, then defaultButton. Do not copy `MessageBox.Show(owner, ...)`
or claim full overload parity.

```csharp
using FluentMessageBoxForm dialog = new(
    "File saved.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
DialogResult result = dialog.ShowDialog(this);
```

`FluentMessageBoxForm : Form` has constructors with text/caption/buttons/icon and
optionally MessageBoxDefaultButton. Standard Form.ShowDialogAsync applies on
supported frameworks; WARP's typed helper has a different result contract.

## Final checks

1. UI versus ViewModel ownership is correct.
2. Argument order and owner handling match the actual overload.
3. Dialog is disposed and inherits configured app theme/DPI context.
