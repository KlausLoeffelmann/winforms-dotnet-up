---
name: winforms-toolstrip-icons
description: >-
  Builds icon-first WinForms command ToolStrips without WARP using a small
  symbol-font image factory, readable mixed button/label/editor layouts, keyboard
  accelerators and accessible icon names. Owns source-font verification,
  DPI/theme/High Contrast regeneration, bitmap lifetime and Designer boundaries;
  routes WARP symbol hosts to their specialist.
---

# Icon-first ToolStrips

## Durable rules

- **Command ToolStrips have icons by default.** Prefer image-only buttons with
  useful tooltips and AccessibleName; keep meaningful Text for command identity.
  ImageAndText is a deliberate discoverability choice, not an excuse to omit icons.
- Text-only items need a reason: a ToolStripLabel captioning its adjacent ComboBox,
  TextBox labels, status text and category/tab headers are legitimate exceptions.
  Normal menu captions retain text and mnemonics.
- With WARP, load `warp-toolstrip-icons` and `warp-symbols`; reuse the installed
  symbol infrastructure instead of duplicating a local glyph renderer.
- Without WARP, use an appropriate installed symbol font and a **small shared
  Icon/Image factory** to deploy ordinary command icons consistently.
- Keep factory/rendering calls in regular runtime code after Designer-created
  items have owners. Static resources use supported `.resx` serialization;
  factory calls inside InitializeComponent are not a Designer-roundtrip shortcut.

## Non-WARP image factory contract

Use Segoe Fluent Icons for an appropriate Windows 11 target, or an installed
Segoe MDL2 Assets source for older supported Windows. Verify the chosen font
family and real glyph code points; do not treat GDI's silent font fallback as
success or invent private-use glyphs.

The factory accepts a named/typed glyph, device-pixel image size and current host
color; it returns an independently owned transparent bitmap. Render with an
explicit pixel-unit Font and centered glyph metrics. Cache only when useful;
do not add a general asset framework for a handful of toolbar commands.

Images assigned to ToolStripItems need an explicit owner:

- Keep source fonts alive while rendering; dispose owned Fonts, Graphics and
  temporary drawing objects.
- Give each item its own image or a clone of a borrowed cached image.
- On regeneration replace the item image, then dispose its previous owned image.
  A strip/item should not be assumed to dispose arbitrary assigned Images.
- Clear/dispose owned images when the window/strip is disposed. Do not dispose
  shared resources from one item's cleanup.

Render against the **current host** DPI, ForeColor and theme/High Contrast state.
Regenerate raster images on DPI, theme or relevant host-color changes. A scalable
font source is not a live scalable/recolored bitmap. Keep ImageScalingSize and
actual image pixels consistent; avoid baking a 16px image once and stretching it
on every monitor. GDI resource and measurement details belong to
[rendering](../winforms-rendering/SKILL.md).

## Mixed-item strips

For a horizontal toolbar mixing ToolStripButtons with ToolStripLabels,
ToolStripTextBoxes or ToolStripComboBoxes, explicitly use:

```csharp
toolbar.AutoSize = true;
toolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
```

These are declarative property assignments suitable for the Designer partial.
Use `VerticalStackWithOverflow` only for an intentionally vertical strip.
Do not force a fixed strip height or a Flow layout that clips different preferred
heights. Keep inherited/current fonts, image scaling, item margins and editor
widths coherent. Labels remain beside their editors when space permits; verify
overflow still allows meaningful command and editor access.

Set DisplayStyle deliberately: Image for ordinary icon commands; Text for
caption labels; ImageAndText where justified. Test mixed preferred heights at
large runtime fonts and DPI, not just an all-button 100% layout.
Shared host geometry belongs to [layout](../winforms-high-dpi-fluent-layout/SKILL.md).

## Keyboard and accessibility

Frequently used menu commands need `&` mnemonics and appropriate ShortcutKeys;
avoid duplicate accelerators within a menu/category. Icon buttons need concise
AccessibleName and ToolTipText explaining their action and available shortcut.
Do not rely on a private-use glyph or the tooltip as the screen-reader name.

Captioned editors need their own semantic name and predictable focus order.
Use AccessibleDescription only for context not already supplied by the name.
Do not flood screen readers with repeated labels, decorative separators or
continuously announced status/clock updates.

Options/category navigation is a text-header exception owned by
[Options dialogs](../winforms-options-dialog/SKILL.md); it is not a command-icon
factory use case. Static item/events/cleanup follow
[Designer](../winforms-designer-code/SKILL.md).

## Final checks

1. Ordinary commands show real glyph images, not fallback boxes, and have tooltips
   and accessible names; text-only exceptions are intentional.
2. Mixed controls, overflow, keyboard accelerators and focus remain usable after
   font/DPI/theme changes.
3. Each handler executes once; font/image ownership and regeneration are explicit.
