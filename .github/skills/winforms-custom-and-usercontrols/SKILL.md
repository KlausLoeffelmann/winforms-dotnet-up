---
name: winforms-custom-and-usercontrols
description: >-
  Builds reusable address editors, wizard/tab pages, composite UserControls,
  custom badges/sliders/gauges, scrollable controls and rich list/grid cells.
  Applies when selecting a base class, fixing preferred-size or runtime-font
  behavior, preserving consumer fonts, theming native scrollbars, batching
  property initialization, or exposing new control properties to the Designer.
---

# Reusable controls and composite behavior

## Durable rules

- Prefer the most specific reusable base that already solves input, focus, accessibility and data behavior; don't rebuild it on a blank canvas unnecessarily.
- Preferred size must honestly reflect font/content/padding/DPI and proposed constraints; bounds, painting and `AutoSize` must agree.
- Preserve ambient/consumer-owned fonts and existing scaling baselines; owned derived fonts need lifecycle updates.
- Native theming opt-in belongs in `CreateParams` **before** reading `base.CreateParams`.
- Static composition follows Designer serialization; runtime-added pages/content are legitimate regular code.

## Base selection

| Need | Starting point |
|---|---|
| Generic gauge/badge/slider/custom button | `Control` or closest `ButtonBase`, `ScrollableControl`, `Panel`, `Label`, etc. |
| Domain composite/address/preferences editor | `UserControl` containing standard controls |
| Text input | Usually derive concrete `TextBox` or compose it; `TextBoxBase` is **public abstract**, not internal |
| Tabular/list-shaped editable records | Prefer `DataGridView` reuse; use other bases only when requirements justify their accessibility/virtualization cost |

For rich records, custom `DataGridViewCell` can draw one whole item (symbols, colors, fonts, multiple lines). Headers/per-field columns are useful when schema/sort/filter is needed; don't require them for cards. Measure row height from its tallest cell. [Rendering](../winforms-rendering/SKILL.md) owns paint/resource/text rules, [binding](../winforms-databinding/SKILL.md) owns data/currency.

## Composite font/scaling contract

- Avoid arbitrary design-time fonts on composite children. Preserve intentional consumer fonts and Designer autoscale measurements.
- In `OnFontChanged`, update owned explicitly derived fonts/glyph measurements and request layout/repaint. Normal ambient children inherit naturally; don't overwrite consumer-specified fonts.
- Late-added UserControls intended as tab/wizard/page content normally use `AutoScaleMode.Inherit` to scale from the runtime parent. This does **not** update your explicitly derived child fonts for you.
- Consumer content (pages/decorators): apply font changes only where font is ambient, not explicitly assigned.
- WebView/BlazorWebView content does not inherit WinForms fonts. Bridge family/size using parameters, CSS variables or JS interop; it is a separate rendering/layout domain.
- Use [adaptive layout](../winforms-high-dpi-fluent-layout/SKILL.md) for nested tables/groups. Verify the outer control's preferred size includes all children. For very large content, use a scrollable host instead of an impossible fixed minimum.

Read [control examples](references/control-examples.md) when implementing preferred sizing, native scrollbar theming, or `ISupportInitialize`. Override `SetBoundsCore` only when needed and respect `BoundsSpecified`; don't clamp a dimension the caller did not set. Content/font/padding changes must notify the parent layout, not just repaint.

Virtual members such as `CreateParams`/`DefaultSize` may run during base construction before your derived constructor body; do not depend on state initialized there.

New properties follow [Designer metadata](../winforms-designer-code/SKILL.md). Rendering owns palettes/High Contrast and cache invalidation, while this skill owns native style opt-in and composite propagation.

.NET Framework does not provide modern `Application.IsDarkModeEnabled`/`ApplyThemingImplicitly` or the modern out-of-process Designer SDK. Offer target-compatible system-color/custom-paint/legacy-Designer alternatives instead of silently upgrading.

## Final checks

1. Test preferred size and composition at large fonts/200% DPI and after runtime text/font/padding changes.
2. Consumer explicit fonts survive; late-added pages inherit scaling; web fonts use an explicit bridge.
3. Properties roundtrip and initialization batches avoid half-configured expensive work.
