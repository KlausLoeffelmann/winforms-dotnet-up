---
name: winforms-high-dpi-fluent-layout
description: >-
  Designs or fixes settings dialogs, customer-entry forms, preferences pages
  and UserControls whose labels/buttons clip with larger text, DPI, translation
  or resizing. Owns TableLayoutPanel grouping, exact modal button footers,
  resizable multiline/list/tree/grid viewports, monitor overflow, window bounds,
  keyboard accessibility and fullscreen layout. Pairs with designer-code when
  creating or editing serialized static UI.
---

# Adaptive layout and window geometry

## Durable rules

- Default dialogs and UserControls to `TableLayoutPanel` for runtime-font/DPI adaptation. **Never use `FlowLayoutPanel` for buttons unless explicitly requested**; it remains an option for other flow-shaped content.
- Real-content rows normally `AutoSize`; columns normally `Percent`, except caption columns `AutoSize`. Avoid absolute text geometry.
- Single-line editors use **`Anchor=Left|Right` only**, not top/bottom anchoring or `Dock=Fill`. Containers, buttons and genuine viewports intentionally fill their cells.
- Keep logical nested groups and continuous preferred-size measurement; do not let one fixed-size intermediate container clip the inner content.
- Choose one root profile below. **The standard modal footer requires root `AutoSize=false`**, even though its inner/outer button tables autosize.
- Never force pixel equality by capping below preferred size. Equal Percent columns can round by one pixel.

## Root profile decision

| Profile | Use | Root and rows |
|---|---|---|
| A — resizable fields | Entry form with ordinary fields | Constrained root Dock Fill/four-side anchor; real rows AutoSize; **empty final Percent100 spring**. |
| B — standard modal footer | Modal form with pinned bottom actions | Root **AutoSize=false**, Dock Fill or four-side anchor; final complete row AutoSize footer; optional empty **immediately preceding** Percent100 spring. |
| C — resizable viewport | Multiline editor, ListBox, TreeView, grid | Resizable Form; constrained root; deliberate **occupied Percent content row**, viewport Dock Fill; other content rows AutoSize. |
| D — genuinely content-sized | Whole Form grows/shrinks to content | Continuous inner-to-outer AutoSize/GrowAndShrink; dock nested containers into cells; **no root spring**. Not the pinned standard-modal-footer recipe. If that footer is needed, choose B. |

Percent rows in C are viewports, not empty springs. An AutoSize field row is not suitable for a viewport that needs useful working space.

## Caption/editor and logical groups

Label/input table: caption column `AutoSize`, editor column `Percent,100`; rows `AutoSize`. Label: `AutoSize=true`, `Dock=Fill`, `TextAlign=MiddleLeft`. Manually break long caption text at readable points. Single-line `TextBox`, `ComboBox`, `MaskedTextBox`, `NumericUpDown`, etc.: `Anchor=Left|Right` only, retaining preferred height and vertical centering.

Large entry forms: root has **1, 2 or 3 Percent GroupBox columns** by logical section; group rows AutoSize. These are group columns, not the caption/editor columns inside each group. Each GroupBox **and** its inner table: `AutoSize=true`, `AutoSizeMode=GrowAndShrink`, `Dock=Fill`. Child real rows AutoSize; optional empty final Percent spring inside groups absorbs equal-height multi-column spare space. Omit springs when whole content autosizes.

Radio choices: single-column table, AutoSize rows, inside one AutoSize/GrowAndShrink GroupBox; keep mutually exclusive buttons within the same group. Break a large screen into sections/UserControls, not one mega-grid. Tool/status strips use AutoSize rows around a Percent split/viewport area.

Percent columns distribute available width; AutoSize tables still measure child preferred widths. These design preferences are not a claim that the allocation engine measures Percent before AutoSize/Absolute cells.

## Exact standard modal button footer — profile B

This complete property contract is required, not optional example guidance:

| Element | Declarative contract |
|---|---|
| Root | `AutoSize=false`; `Dock=Fill` **or** Top/Bottom/Left/Right anchor. Final full root row `AutoSize`. |
| Inner button table | `AutoSize=true`, `AutoSizeMode=GrowAndShrink`; **one AutoSize row**; **N Percent columns at 100/N%**; `Margin=new Padding(0,20,0,0)`. |
| Every button | `AutoSize=true`, `AutoSizeMode=GrowAndShrink`, `Dock=Fill`, **`Padding=new Padding(15,5,10,5)`**, **`Margin=new Padding(5,5,5,5)`**. Padding asymmetry is intentional. |
| Captions | **Longer than 30 characters:** manually insert readable line breaks; do not rely on automatic wrapping. |
| Outer right-alignment table | `AutoSize=true`, `AutoSizeMode=GrowAndShrink`; **two columns: first Percent100, second AutoSize**; **one AutoSize row**. Put inner table in **second column**. |
| Root placement | Outer table in **column 0 of complete final AutoSize row**; `SetColumnSpan(outer, root.ColumnCount)`. Dock outer table Fill into that row. |
| Optional spring | Empty Percent100 row **immediately before** footer; never put actions in the spring. |

Two inner columns use 50/50; three use equal thirds as literals in Designer code. `AcceptButton`/`CancelButton` provide Enter/Escape; corresponding `DialogResult` closes automatically only when no validation/save gate is needed. For validation, leave affirmative button result unset and assign the Form result **after** successful validation/save.

Read [declarative examples](references/layout-examples.md) when implementing the footer, group or viewport. Static layout belongs in `InitializeComponent`; legitimate runtime content/measurement belongs in regular code under [Designer rules](../winforms-designer-code/SKILL.md).

## DPI, minima, overflow and accessibility

Use 96 DPI/100% as a deliberate new-design baseline; preserve existing `AutoScaleMode`/`AutoScaleDimensions` until a justified migration. Set startup PerMonitorV2 coherently; runtime-added composites follow [custom-control scaling](../winforms-custom-and-usercontrols/SKILL.md).

`Margin` separates child controls; container `Padding` contributes its inner display area, not independent per-cell padding. Buttons also use padding for content measurement. Respect both instead of claiming padding does nothing.

Derive useful minimum space from current font, content, operation and monitor working area. Viewports need enough visible lines/items/columns to work; no one-size-fits-all pixel minimum. If content exceeds a monitor, use a deliberate scrollable host/adapted layout and keep actions keyboard-reachable. Reevaluate on font/DPI/monitor changes; don't clamp below necessary content and simply hide overflow.

Provide `AccessibleName`/`AccessibleDescription`, sensible TabIndex/mnemonics, and Tab/Shift+Tab/Enter/Escape paths. Test longest translations rather than assuming particular languages always dominate.

Read [window state and monitor handling](references/window-state.md) when persisting bounds or implementing fullscreen. Designer visual inheritance can lock a base Form's TableLayoutPanel; use a supported composition boundary rather than assuming an inherited grid is editable.

## Final checks

1. Test 10/18/24/**36 pt**, 100/150/200% DPI, resize, long captions/translations; labels, editors, buttons and viewports remain usable.
2. Confirm profile, row occupancy, AutoSize chain, exact footer, horizontal-only editor anchoring, and no stale width caps.
3. Verify keyboard navigation, overflow access and monitor transitions; report untested DPI/device scenarios.
