---
name: warp-binding-autoview
description: >-
  Inspects WARP experimental property-to-control AutoView scaffolding and writes
  compatible control factories. Diagnoses invalid generic hosts or factory
  registrations and avoids treating the unfinished wrapper as a generated,
  automatically data-bound editor.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\Containers\\AutoView"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# AutoView scaffolding

## Critical constraints

- Current AutoView is **unfinished scaffolding**, not a functioning editor generator.
- `AutoViewBase<TControl>` is a wrapper, not a Control; its generic host must be
  `ContainerControl, new()`. A TableLayoutPanel does not meet that constraint.
- Factory registration requires a public parameterless factory constructor.
  Built-in `StringToTextBoxFactory` does not have one.
- DefaultMapping currently maps types to **control types**, not uniformly factory
  types; do not infer a complete generation algorithm from the dictionary.
- Shared binding, Designer and generated field layout belong to
  `winforms-databinding`, `winforms-designer-code`,
  `winforms-high-dpi-fluent-layout`, respectively.

## Actual wrapper

Namespace `WarpToolkit.WinForms.Extensions.AutoView`:

| Member | Contract |
|---|---|
| Constructor | `AutoViewBase(INotifyPropertyChanged viewModel)` rejects null |
| `Control` | `TControl`, private setter |
| `ViewModel` | `INotifyPropertyChanged`, null-rejecting getter/setter |

It creates an empty TControl and retains the VM; no property enumeration, factory
execution or binding takes place.

```csharp
using WarpToolkit.WinForms.Extensions.AutoView;

var scaffolding = new AutoViewBase<UserControl>(viewModel);
host.Controls.Add(scaffolding.Control);
```

The application must still construct and bind editor content.

## Mapping and factory contracts

Namespace `WarpToolkit.WinForms.Containers`:

- `AutoViewControlMappingBase.AddMapping<TProperty,TControlFactory>()` requires
  `TControlFactory : TypeToControlFactoryBase, new()`.
- `DefaultMapping` is `Dictionary<Type,Type>`. Defaults include string→TextBox,
  int→NumericUpDown, bool→CheckBox, DateTime→DateTimePicker,
  List&lt;string&gt;→ComboBox, FileInfo/DirectoryInfo→FilePathPicker.
- Adding a mapping before first default access initializes an empty dictionary;
  do not assume built-in defaults are merged automatically.

`TypeToControlFactoryBase` protected constructors accept either PropertyInfo or
`(string propertyName, string? displayName = default, string? category = default,
string? description = default)`.
PropertyInfo supplies Category/DefaultValue/Description/DisplayName attributes;
DefaultValue is stored as a string. The string constructor rejects blank names.

| Factory member | Shape/default |
|---|---|
| `Category`, `DefaultValue`, `Description`, `DisplayName` | string? getters |
| `HasCaption`, `HasLayout`, `RequestStackedLayout` | virtual bool: true, false, false |
| `GetCaption()` | IEnumerable&lt;Control&gt;; default label uses DisplayName |
| `GetControls()` | virtual IEnumerable&lt;Control&gt;?; default null |
| `GetControlLayout()` / `GetCaptionAndControlLayout()` | virtual TableLayoutPanel?; default null |

`StringToTextBoxFactory(string propertyName, string? displayName = null,
string? category = null, string? description = null)` returns a TextBox named
after the property with default-value text. It does **not** bind the property.
Instantiate it directly with a property name, rather than the invalid
`AddMapping<string,StringToTextBoxFactory>()`.

Custom factories can supply caption/control/layout; if registered generically,
provide a meaningful public parameterless constructor and verify how the caller
will supply per-property metadata. Do not invent a hidden generator that does so.
The default caption factory does not implement the canonical label layout;
apply the shared layout contract when building real UI.

## Related work and final checks

Read `warp-input-controls` for BindableComboBox's SelectedBindingValue, IList
DataContext gating and ValueNotFoundException. Generated controls inherit ambient
colors; custom factories have no special built-in theme handling.

1. Example host and factory satisfy the actual generic constraints.
2. Generation/binding status is honest; application construction is explicit.
3. Real UI follows the named Designer/layout owners, not an opaque helper pretending
   to round-trip through InitializeComponent.
