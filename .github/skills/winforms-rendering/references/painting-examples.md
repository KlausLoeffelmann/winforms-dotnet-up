# Matched painting and buffers

## Contents

- [Measurement and strokes](#measurement-and-strokes)
- [Owned manual buffer](#owned-manual-buffer)
- [Quality and cache choices](#quality-and-cache-choices)

## Measurement and strokes

Complete helper uses matching Graphics/font/typographic format; default MeasureString overhang padding is not inherently wrong, but mismatched measurement can accumulate errors in precise mixed-font/monospace positioning:

```csharp
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

/// <summary>Provides matched text measurement and inside-bound stroke drawing.</summary>
public static class PaintGeometry
{
    /// <summary>Measures and draws with the same typographic format.</summary>
    public static SizeF DrawRun(Graphics graphics, string text, Font font, PointF origin)
    {
        using StringFormat format = StringFormat.GenericTypographic;
        SizeF measured = graphics.MeasureString(text, font, PointF.Empty, format);
        graphics.DrawString(text, font, SystemBrushes.WindowText, origin, format);

        return measured;
    }

    /// <summary>Insets a centered stroke without negative dimensions.</summary>
    public static void DrawInside(Graphics graphics, RectangleF bounds, Color color, float width)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        using Pen pen = new(color, width);
        float half = width / 2;
        RectangleF inner = new(
            bounds.X + half, bounds.Y + half,
            Math.Max(0, bounds.Width - width), Math.Max(0, bounds.Height - width));
        if (inner.Width > 0 && inner.Height > 0)
        {
            graphics.DrawRectangle(pen, inner.X, inner.Y, inner.Width, inner.Height);
        }
    }
}
```

For GDI text, measure/draw with matching TextRenderer flags/constraints/device context. GDI+ StringFormat.GenericTypographic helps precise runs, but complex shaping/kerning requires whole-run measurement. Use Font.GetHeight(graphics) for line height; additional spacing is a design choice, not a universal 12-point nonlinear formula.

Thick strokes are centered around shape edges. Manual half-width inset is predictable; PenAlignment.Inset has limitations for dash/compound/closed shapes. Integer/half-pixel alignment depends on smoothing/pixel-offset/transforms, not one universally crisp coordinate trick.

## Owned manual buffer

Built-in buffering is normally enough. This complete control demonstrates an explicitly **owned** context, zero-size guards, size/DPI recreation and cleanup. It avoids disposing shared BufferedGraphicsManager.Current:

```csharp
using System.Drawing;
using System.Windows.Forms;

/// <summary>Demonstrates resize-aware manually buffered painting.</summary>
public class BufferedCaption : Control
{
    private readonly BufferedGraphicsContext _context = new();
    private BufferedGraphics? _buffer;
    private Size _bufferSize;
    private float _bufferDpi;

    /// <summary>Requests repaint when dimensions change.</summary>
    public BufferedCaption()
    {
        SetStyle(ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.ResizeRedraw, true);
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        if (_buffer is null || _bufferSize != ClientSize || _bufferDpi != e.Graphics.DpiX)
        {
            _buffer?.Dispose();
            _buffer = _context.Allocate(e.Graphics, ClientRectangle);
            _bufferSize = ClientSize;
            _bufferDpi = e.Graphics.DpiX;
        }

        Graphics target = _buffer.Graphics;
        target.Clear(BackColor);
        TextRenderer.DrawText(target, Text, Font, ClientRectangle, ForeColor);
        _buffer.Render(e.Graphics);
        base.OnPaint(e);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffer?.Dispose();
            _context.Dispose();
        }

        base.Dispose(disposing);
    }
}
```

For an offscreen Bitmap, own/dispose both bitmap and created Graphics, guard zero size, set required DPI, and draw only current content region after shrink. Grow-only reuse trades allocation for retained memory; enforce a budget. Transparent bitmap content does not magically make native HWND child composition transparent.

## Quality and cache choices

- Save/restore Graphics state when changing quality/transforms for a run.
- SmoothingMode.None suits crisp grids/pixel art; AntiAlias suits curves/diagonals. Don't set HighQuality everywhere regardless of cost.
- TextRenderingHint applies to GDI+ text, not TextRenderer. SystemDefault respects settings; ClearType needs an appropriate opaque surface; anti-alias choice depends on output, not fixed 12/20-point thresholds.
- Interpolation: high-quality bicubic for suitable photos, bilinear for general scaling, nearest neighbor for pixel art.
- SourceOver blends alpha; SourceCopy replaces destination, which can be appropriate for buffer clearing/composition but isn't an equivalent faster rendering policy.
- Cached pens/brushes/derived fonts/images need invalidation on effective font/DPI/theme/content changes. Dispose replaced objects, preserve shared/ambient ones, and don't allocate a fresh resource for every loop iteration.
- Geometry DPI conversion uses logical units × DPI/96; physical inches × DPI. Point-based Font size is already rendered DPI-aware—do not multiply it again.
