---
name: winforms-options-dialog
description: >-
  Builds ergonomic Tools/Options and preferences dialogs with one Designer-friendly
  UserControl per category, typed label/value fields, nested value/dialog pickers,
  grouped choices and accessible category navigation. Uses WARP FluentTabControl
  when available; otherwise uses an available vendor tab host or ToolStrip/panel
  navigation rather than styling a native TabControl.
---

# Tools/Options composition

## Durable rules

- Use one separately Designer-openable **UserControl per options category**.
  Keep the Form responsible for navigation, confirmation and committing preferences,
  not every category's control creation or business logic.
- Shared geometry, spacing, overflow and the exact modal footer belong to
  [layout](../winforms-high-dpi-fluent-layout/SKILL.md); static composition belongs
  to [Designer](../winforms-designer-code/SKILL.md).
- Do not use a native WinForms `TabControl` to style this dialog. Select the
  navigation host below without adding a new vendor dependency just for styling.
- Edit a draft. Commit/persist only after OK/Apply and successful validation;
  Cancel, Escape, the window close button and cancelled pickers leave saved
  values and live application state unchanged.
- Preserve existing defaults when introducing an option. Explain units, bounds
  and sentinel values beside the field; do not silently clamp invalid saved input.

## Navigation host selection

| Application | Host |
|---|---|
| WARP is already used and the installed API supports it | `FluentTabControl`; load `warp-fluent-tab-control` |
| Non-WARP with a suitable existing third-party tab control | That vendor's supported host, still one UserControl per category |
| Non-WARP without such a host | AutoSize `ToolStrip` above a panel that displays the category UserControls |

For ToolStrip/panel navigation, use text headers as a deliberate exception to
icon-first **command** toolbars. Use checked/selected header state and a lightweight
renderer with a Windows 11-style accent underline beneath the **selected** category.
Keep keyboard focus indication distinct: moving into the page must not erase its
selection underline. Respect High Contrast with system highlight/focus colors.
Do not owner-draw away the item's text, focus or accessible state.

Headers need descriptive category names, mnemonics, discoverable selected state and
keyboard navigation/activation. Show only the active page and exclude inactive
pages from Tab traversal. Tab/Shift+Tab enters/leaves the current page; Enter/Escape
still reach the dialog's affirmative/cancel actions.
Use [ToolStrip icons/layout](../winforms-toolstrip-icons/SKILL.md) and
[rendering](../winforms-rendering/SKILL.md) for the fallback strip.

WARP changes **only the navigation host**, not the category, picker, grouping,
keyboard, draft-state or accessibility rules. `warp-app-services` owns persistence
and dialog-service integration; do not dispose shared settings services from a page.

## Option control selection

| Value | Default editor |
|---|---|
| Text | `TextBox` |
| Date/time or currency requiring formatted/masked entry | `MaskedTextBox` (the WinForms masked-edit control); define culture, mask and typed validation |
| Numeric value with a meaningful/longer range | `NumericUpDown`; set actual minimum, maximum, precision and increment |
| Boolean/classic option | `CheckBox`; use `Appearance.ToggleSwitch` in supported .NET 11 modern rendering |
| Mutually exclusive named choices | RadioButtons inside one logical GroupBox |
| Font, file, folder, color, printer or another dialog-picked value | Caption plus value editor and adjacent picker button, as below |

Masks alone do not validate dates or currency. Store typed values and surface parse
errors; do not silently turn invalid input into a default. WARP typed formatters
may replace masked entry where already appropriate; load `warp-input-controls`
and verify the consuming API. [Databinding](../winforms-databinding/SKILL.md)
owns conversion/writeback; [.NET 11](../winforms-net11-apis/SKILL.md) owns toggle
availability and effective renderer gates.

## Dialog-picked values

Use the visible layout `CaptionLabel: | [value TextBox] [...]`, not one long button
whose caption also carries the current value.

The label/value table has an AutoSize caption column and Percent100 value column.
Put a **dedicated nested TableLayoutPanel in the value cell**:

| Element | Required contract |
|---|---|
| Nested picker table | AutoSize=true, GrowAndShrink, Dock=Fill; one AutoSize row; Percent100 first column and AutoSize second column |
| Value TextBox, column 0 | Anchor=Left\|Right only; show the current value; ReadOnly when free typing/type conversion is unsupported, such as Font |
| Picker Button, column 1 | AutoSize=true, GrowAndShrink, Dock=Fill; compact `...` caption and a context-specific AccessibleName, such as "Choose interface font" |

The picker opens the relevant standard dialog with the draft's current value.
On accepted selection, update both the draft and its displayed value; on Cancel,
change neither. Clone/dispose Font or Image objects according to ownership.
Reuse a WARP picker only when it preserves the visible value, accessibility and
adaptive sizing contract; do not invent properties on it.

## Grouping, spacing and access

Use clear label/value pairs and consistent vertical **control Margins** between
logical rows. Nest coherent groups in AutoSize/GrowAndShrink GroupBoxes rather
than turning a long page into a dense unstructured grid. Keep radio choices in
the same group. Large pages scroll inside their UserControl; actions remain
outside that viewport in the canonical modal footer.

Provide mnemonics and sensible TabIndex order. Inputs without a self-describing
caption need an AccessibleName; ambiguous pickers need a concise action name.
Use AccessibleDescription only for otherwise missing context, units or hazards,
not to repeat captions or announce every decorative label.

## Final checks

1. Each category roundtrips independently in the Designer; navigation is not a
   styled native TabControl.
2. Test large fonts/text scale, DPI and narrow viewports: editors, picker values,
   category headers and pinned actions remain readable or deliberately scrollable.
3. Verify keyboard/screen-reader context and OK/Apply/Cancel/picker-cancel behavior.
