---
name: winforms-rendering
description: >-
  Fixes flicker, resize artifacts, clipped or mismeasured text, chart/gauge
  painting and rich owner-drawn grid/list cells in WinForms. Owns GDI/GDI+
  measurement, buffering, stroke bounds, image quality, DPI geometry, color
  contrast/High Contrast and disposable drawing-resource caching/invalidation.
  Applies when overriding OnPaint or optimizing repeated drawing.
---

# Painting, measurement and resource ownership

## Durable rules

- Match measurement and drawing for each text run: font, device context, constraints, flags/format and rendering API.
- Dispose resources you own; never dispose framework-owned/shared drawing resources.
- Buffer managed custom painting and invalidate appropriately on resize/content changes; don't impose managed owner-paint styles blindly on native controls.
- Scale geometry/pen widths deliberately; do not double-scale point fonts already rendered for current DPI.
- System color **roles** and explicit light/dark palettes must remain readable in High Contrast. Refresh cached resources after font/DPI/theme/content changes.

## API choice and repaint policy

`TextRenderer` (GDI) generally suits UI-consistent high-volume grid/list/log text. `Graphics.DrawString` (GDI+) suits transforms, rotation, alpha/effects and typographic runs. Neither is universally superior or an exact clone of every native control. Mixing APIs for different content is fine; measuring one text run with GDI and drawing it with GDI+ is not.

For a managed owner-painted control, `DoubleBuffered=true` or `UserPaint|AllPaintingInWmPaint|OptimizedDoubleBuffer` reduces flicker. `ResizeRedraw` is useful when output depends on dimensions; otherwise invalidate affected geometry explicitly. Use `Invalidate` to coalesce repaint, not repeated synchronous `Refresh`. Unconditional invalidation from `OnPaint` can cause continuous repainting, not inherently recursive stack overflow.

## Ownership and caches

| Resource | Policy |
|---|---|
| Created Graphics (`CreateGraphics`, `FromImage`, `FromHwnd`) | Dispose; durable UI painting still belongs in paint events |
| `PaintEventArgs.Graphics` | Framework-owned; do not dispose |
| Owned Pen, Brush, Font, Image/Bitmap, Region, GraphicsPath, StringFormat, BufferedGraphics | Dispose when replaced and during owning cleanup |
| SystemBrushes/SystemPens, shared Brushes/Pens and ambient/SystemFonts | Shared; do not dispose |
| `BufferedGraphicsManager.Current` | Shared context; do not dispose; explicitly created context is yours |
| `Color`/SystemColors values | Values, not disposable resources |

Cache only genuinely repeated expensive resources, keyed by their effective font/DPI/palette/content inputs. Dispose stale cached objects; temporary `using` resources are often simpler. Reuse resources outside tight drawing loops. Limit offscreen buffer memory, recreate when size/DPI requires, and handle zero-size controls.

Read [painting examples](references/painting-examples.md) when implementing matched typography, inset strokes, cached/manual buffering or quality settings.

## Theme and accessibility

`SystemColors.Window/WindowText`, etc. express roles that can change under dark mode/High Contrast. Names such as ControlDark/ControlLight are not promises of actual brightness or exact mathematical complements. Default to compatible role pairs; a custom palette needs separate light/dark defaults, measured contrast, and a **High Contrast system-color fallback**, not hard-coded branding everywhere.

`Application.IsDarkModeEnabled` is target-qualified (.NET 9 experimental, .NET 10 stable). Mode query alone doesn't update a cached brush/font or repaint your control. Respond to available system/theme/font/DPI changes, recreate dependent resources and invalidate/layout as needed.

Custom native scrollbar opt-in and composite font propagation are owned by [custom controls](../winforms-custom-and-usercontrols/SKILL.md). Rich cells should reuse grid behavior and compute heights from real content.

## Final checks

1. Test text, thick/dashed borders, image scaling, resize and zero-size transitions.
2. Check GDI-resource growth, buffer budget and repaint cost under scrolling/animation.
3. Verify large fonts, DPI, light/dark and High Contrast, including cached-resource refresh.
