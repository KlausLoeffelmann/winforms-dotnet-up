---
name: warp-data-grids
description: >-
  Displays classic tabular data or template-painted card rows with WARP grids,
  configures alternating-row and header theming and diagnoses virtual single-column
  GridView binding versus ordinary DataGridView behavior.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\GridView; src\\WarpToolkit.WinForms.Specialized\\Controls\\WarpDataGridView.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Data grids

## Critical constraints

- GridView is virtual-mode, **single template-painted column**, not a drop-in
  arbitrary multi-column DataGridView.
- Use WarpDataGridView for classic columns; do not re-enable visual-style headers
  and thereby override its SystemColors headers.
- GridView.DataContext is a `new` shadowed member; use the concrete grid reference.
- Read `winforms-databinding` for lists/selection/update/disposal and
  `winforms-high-dpi-fluent-layout` for a data viewport's dedicated flexible row.
- Theme initialization is not proof every cached custom brush refreshes live.

## Classic grid

WarpToolkit.WinForms.Specialized.WarpDataGridView derives DataGridView.
Constructor enables DoubleBuffered, disables EnableHeadersVisualStyles and applies
SystemColors. A handle-lifetime SystemEvents.UserPreferenceChanged hook reapplies
the theme.

AlternatingRowColorMode defaults Auto:
Auto derives banding by shifting SystemColors.Window lightness;
Off opts out of generated banding; Custom preserves the caller's alternating style.
Inspect the existing style when changing modes rather than assuming Off clears
every previous custom setting.

```csharp
grid.AutoGenerateColumns = true;
grid.DataSource = bindingSource;
```

A strongly typed application grid may wrap BindingList&lt;TRow&gt; plus BindingSource
and define columns; this is an application pattern, not another shipped generic
grid type. Existing DataGridView ApplyDarkMode is in WinForms.Extensions.UI; use
only for the applicable dark mode and do not duplicate WarpDataGridView's theming.

## Template grid

WarpToolkit.WinForms.Grid.GridView (WarpToolkit.WinForms package) creates one
internal GridViewCell column and enables VirtualMode.
Members: new object? DataContext, object? SelectedItem,
GridViewItemTemplate? GridViewItemTemplate;
events GridViewItemTemplateChanged and SelectedItemChanged.
The host assigns template IsDarkMode from Application.IsDarkModeEnabled.
Several classic styling properties (Columns/DataSource/cell/header styles) are
shadowed; they do not turn the grid into a normal multi-column surface.

```csharp
gridView.GridViewItemTemplate = template;
gridView.DataContext = customers;
```

## Template contract

GridViewItemTemplate implements INotifyPropertyChanged.
Virtual Padding defaults (5,5,5,0), ContentPadding (5,10,5,5), LineSpacing 10.
Theme-sensitive color getters: ItemBackgroundColor/ItemForegroundColor,
HighlightFontColor/StandardFontColor, HighlightedBackgroundColor/SelectedBackgroundColor.
Brush getters exist for item background/foreground and highlighted/selected
background—not a fabricated brush for every color.

Required subclass members, in an **external consuming assembly**, use protected
override (the base declarations are protected internal):

```csharp
protected override Size GetPreferredSize(Size restrictedSize, object? value, int rowIndex)
{
    return new Size(restrictedSize.Width, 48);
}

protected override void OnPaintContent(object content, DataGridViewCell gridViewCell,
    PaintEventArgs e, Rectangle clipBounds, bool isMouseOver)
{
    e.Graphics.DrawString(content.ToString(),
        gridViewCell.InheritedStyle.Font ?? SystemFonts.DefaultFont,
        ItemForegroundColorBrush, clipBounds.Location);
}
```

This is a **template-class member fragment**, not a standalone program.
Virtual PaintBorder and PaintErrorIcon accept Graphics/clip/cell bounds plus their
style/error arguments. SetProperty&lt;T&gt; updates a backing field and raises
PropertyChanged; its source explicitly warns against VB WithEvents fields.
Template-owned cached brushes must be treated as template resources, not disposed
after each paint or assumed to retint themselves.

Nested GridViewItemTemplateConverter and GridViewItemTemplateWrapper(Type) support
Designer template selection; wrapper exposes ItemTemplate.
GridViewExtension.Pad(Rectangle, Padding) supports paint geometry.
Designer code follows `winforms-designer-code`; custom painting/resource work
follows `winforms-rendering`.

## Final checks

1. Grid model fits classic columns versus template virtualization.
2. Binding/selection uses the correct concrete properties.
3. Header/row/custom-template theme and resource behavior are tested explicitly.
