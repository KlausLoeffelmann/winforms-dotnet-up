---
name: warp-input-controls
description: >-
  Adds WARP file/folder/save pickers, binding-aware selection and floating-point
  sliders, or formats date/time/money TextBox input with typed extenders. Diagnoses
  wrong picker properties, missing selection values and integer TrackBar shadows.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\Controls; Experimental\\Binding; src\\WarpToolkit.WinForms.Async\\TypedInputExtenders"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Input controls and typed formatting

## Critical constraints

- Use concrete floating-point control types; TrackBar base references expose integer
  members instead of the `new float` members.
- FilePathPicker stores the selection in **FileOrFolderPath**, not Path; ReadOnly
  defaults true.
- BindableComboBox autoassigns IList DataContext to DataSource only outside design
  mode; value binding is SelectedBindingValue.
- Layout/Designer/binding belong to `winforms-high-dpi-fluent-layout`,
  `winforms-designer-code`, `winforms-databinding`; retain their single-line editor
  and caption contracts by reference.
- Inspect installed versions: experimental selection and typed/AI formatter contracts
  may differ from source; decimal money is not a binary floating-point slider.

## Picker inventory

Package WarpToolkit.WinForms; namespace WarpToolkit.WinForms.Controls.
`FilePathPicker : Control` combines text and pick/reveal glyph buttons;
source default Size is 300×24, not a recommended fixed-DPI layout.

| Properties | Type / defaults |
|---|---|
| FileOrFolderPath, InitialDirectory, Filter, DialogTitle, DefaultExt | string, empty |
| PickerMode | OpenFile; also SaveFile, FolderBrowser |
| FilterIndex | int, 1 |
| ShowReadOnly, MultiSelect | bool, false |
| CheckFileExists, CheckPathExists, AddExtension | bool, true |
| ButtonWidth / ButtonText | int 24 / string “…” |
| PickGlyph / RevealGlyph | “…” / “↗” |
| ShowPickButton / ShowRevealButton | true / false |
| AutoFitGlyphs / ReadOnly | true / true |

Changed events include FileOrFolderPath, PickerMode, Filter, DialogTitle, DefaultExt,
FilterIndex, ShowReadOnly, MultiSelect, CheckFileExists, CheckPathExists, AddExtension,
ButtonText and ReadOnly; also PathChanged, PickButtonClicked and RevealButtonClicked.
Do not infer a Path property from PathChanged.

Designer configuration fragment (field declared in the Designer partial):

```csharp
_pathPicker = new WarpToolkit.WinForms.Controls.FilePathPicker();
_pathPicker.PickerMode = WarpToolkit.WinForms.Controls.FilePathPickerMode.FolderBrowser;
_pathPicker.DialogTitle = "Select export folder";
_pathPicker.ShowRevealButton = true;
_pathPicker.Anchor = AnchorStyles.Left | AnchorStyles.Right;
```

Parent it declaratively into the canonical field layout; bind or react to
FileOrFolderPathChanged in regular code. Set ReadOnly false only for manual typing.
Picker colors are ambient, not an independent theme implementation.

## Bindable selection

`BindableComboBox : ComboBox` and `ValueNotFoundException` are in
WarpToolkit.WinForms.Experimental.Binding.
`object? SelectedBindingValue` is Bindable(true); `BindingValueChanged` notifies.

```csharp
combo.DataBindings.Add(nameof(BindableComboBox.SelectedBindingValue),
    viewModel, "SelectedProvider", formattingEnabled: true,
    DataSourceUpdateMode.OnPropertyChanged);
```

Catch/report ValueNotFoundException when assigning values not present in the list.
Ordinary list/selection synchronization follows `winforms-databinding`;
AutoView factories follow `warp-binding-autoview`.

## Floating-point controls

All are in WarpToolkit.WinForms.Controls:

| Control/base | Minimum/Maximum/Value | SmallChange/LargeChange/TickFrequency |
|---|---|---|
| FloatingPointSlider : TrackBar | 0/1/0 float | .01/.1/.1 |
| FloatingPointTrackBar : TrackBar | 0/1/0 float | .1/.25/.1 |
| EditFloatingPointSlider : ContainerControl | 0/1/0 float | .1/.25/.1 |

TrackBar derivatives shadow the range/step members. FloatingPointTrackBar enables
OptimizedDoubleBuffer/AllPaintingInWmPaint. EditFloatingPointSlider composes TextBox
and trackbar, exposing ValueChanged.

## Dates, times and money

Package WarpToolkit.WinForms.Async; namespace
WarpToolkit.WinForms.Async.TypedInputExtenders.
DateFormatterComponent derives TypedFormatterComponent&lt;DateOnly?&gt;;
DecimalFormatterComponent uses decimal?; TimeFormatterComponent uses TimeOnly?.
Their GetValue(Control)/SetValue(Control, typedValue) expect a TextBox.

TypedInputExtenderPanel selects a formatter component per TextBox and exposes
focus/error colors and error text. TypedFormatterComponent provides formatter
settings, TryConvertToValueAsync, ConvertToDisplayAsync, InitializeEditedValueAsync,
and TryGetValueAsync. Conversion events can indicate busy/prevent changes.
The abstract AITypedFormatter&lt;T&gt; requires three cancellation-aware conversion
methods; its name alone does not register an AI provider. AI-specific extenders
have additional dependencies covered by `warp-winforms-ai`.

Choose culture/format and validation deliberately; DateFormatter.DateFormat and
TimeFormatter.TimeFormat select format enums. Do not invent extender wiring without
checking the installed component's public settings/Designer serialization.

## Final checks

1. Selection and typed values bind to their actual properties/types.
2. Range/format/culture and invalid-input reporting fit the domain.
3. Input layout follows the shared owner, with no fabricated AutoView/AI setup.
