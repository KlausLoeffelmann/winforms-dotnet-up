---
name: warp-winforms-extensions
description: >-
  Traverses WARP WinForms control trees, attaches converters to existing bindings,
  calculates Form geometry or font metrics and consumes typed modal results.
  Addresses missing bindings, incorrect traversal predicates and helpers mistaken
  for void operations or DialogResult-returning APIs.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.Extensions\\UI; ComponentExtensions; FontExtensions"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Focused WinForms extensions

## Critical constraints

- Use the actual helper namespace/signature; generic predicate overloads differ.
- `AddBindingConverter` modifies an **existing** binding; it does not create one.
- `CenterToScreen` returns a rectangle, not a mutation. Typed ShowDialog returns
  `IModalDialogResult<T>`, not WinForms DialogResult.
- Control-tree operations run on the owning UI thread. Read `winforms-async-apis`
  for marshaling; `winforms-designer-code` for declarative initialization.
- Shared layout/DPI belongs to `winforms-high-dpi-fluent-layout`. This is not a
  broad control catalog or a package-selection router.

## Workflow and traversal

Reference `WarpToolkit.WinForms.Extensions`; tree/Form helpers use
`WarpToolkit.WinForms.Extensions.UI`.

| Family | Actual operations |
|---|---|
| Immediate children | `ChildControls()` / `<T>()`, `FirstChild` / `<T>`, `FirstChildOrDefault<T>` |
| Ancestors | `AscendantControls()` / `<T>`, `FirstAscendant` / `<T>`, `FirstAscendantOrDefault` / `<T>` |
| Descendants | `DescendantControls()` / `<T>`, `FirstDescendant` / `<T>`, `FirstDescendantOrDefault` / `<T>` |
| Stateful descendant predicate | `DescendantControls<CArg>` / `<T,CArg>` with predicate and argument |
| Tree actions | `Root()`, `TraverseAction(Action<Control>)` |
| Null/ambient font | `EnsureNotNull<T>`, `HasAmbientFont`, `ApplyAmbientFont` |

First methods throw if no match; OrDefault methods return null.
`AscendantControls<T>` takes `Func<Control,bool>?`, whereas
`DescendantControls<T>` takes `Func<T,bool>?`: do not assume all four families
have identical overload inventories.

```csharp
using WarpToolkit.WinForms.Extensions.UI;

foreach (Button button in this.DescendantControls<Button>(b => b.Enabled))
{
    button.Visible = false;
}
TextBox? primary = this.FirstDescendantOrDefault<TextBox>(t => t.Tag is "Primary");
Control root = this.Root();
this.TraverseAction(c => c.UseWaitCursor = true);
```

## Existing-binding converters

Namespace: `WarpToolkit.WinForms.Extensions.ComponentExtensions`.
`AddBindingConverter(this IBindableComponent, string propertyName,
WarpToolkit.ComponentModel.IValueConverter)` returns the Binding.
It throws for null arguments or no binding on that property, hooks Format/Parse
using current culture and unhooks on component disposal.

```csharp
using WarpToolkit.WinForms.Extensions.ComponentExtensions;

textBox.DataBindings.Add(nameof(TextBox.Text), viewModel, "Amount",
    formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged);
Binding binding = textBox.AddBindingConverter(nameof(TextBox.Text), converter);
```

For a one-off transformation, ordinary Binding.Format/Parse is also appropriate.
`RemoveBinding` removes the property's binding and throws if missing; do not
assume it selects by source identity. Read `winforms-databinding` for update
modes, converter lifetime and validation.

## Form geometry and typed modal results

`CenterToScreen(form, screen = null, horizontalFillGrade = 50,
verticalFillGrade = 50)` computes a rectangle using **screen Bounds**, not
WorkingArea. Assign it deliberately only when full-screen bounds are intended;
use canonical working-area constraints for ordinary windows.

`GetRestorableBounds()` returns Bounds when Normal and RestoreBounds when
Minimized/Maximized; unknown state throws.

```csharp
using WarpToolkit.Desktop.AppServices;
using WarpToolkit.WinForms.Extensions.UI;

IModalDialogResult<EditCustomerVm> result =
    await dialog.ShowDialogAsync<EditCustomerVm>(viewModel);
if (result.DialogCloseReason == DialogCloseReason.OK
    && result.ReturnValue is { } accepted)
{
    SaveAcceptedCustomer(accepted);
}
```

This is a **host-method fragment**: dialog/viewModel and SaveAcceptedCustomer
belong to the application. The direct helper requires a class VM; the service
overload additionally requires INotifyPropertyChanged. The helper sets DataContext,
awaits the framework modal API and validates children when closing with OK.
Do not conflate it with native Form.ShowDialogAsync, or claim it takes the VM by ref.
The current helper's event-unsubscription is not unconditional; prefer a
single-use/disposed dialog instead of repeatedly invoking it on the same instance.
Dialog service registration belongs to `warp-app-services`.

## Font metrics

Import `WarpToolkit.WinForms.Extensions.FontExtensions`.
`GetAscent`, `GetDescent`, `GetLeading`, `GetBaseLine`, `GetLineHeight` return float
pixel metrics; baseline converts font-family design units using line spacing and
font height, and line height is the font's Height. They are not DirectWrite
metrics. Read `winforms-rendering` for font/resource ownership.

## Final checks

1. Traversal predicate and null/throw behavior fit the caller.
2. Binding exists before converter attachment; repeated attachment is intentional.
3. Returned geometry/result is consumed correctly, with UI-thread ownership.
