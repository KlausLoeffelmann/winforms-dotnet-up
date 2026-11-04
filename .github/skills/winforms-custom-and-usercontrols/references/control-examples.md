# Preferred-size, native theme and initialization

## Contents

- [Text-bearing control](#text-bearing-control)
- [Native scrollbar opt-in](#native-scrollbar-opt-in)
- [Initialization batching](#initialization-batching)

## Text-bearing control

Complete single-line .NET 10 owner-painted example. It intentionally measures an unwrapped caption; a wrapping control must use proposed width/flags consistently instead.

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

/// <summary>A text-bearing managed control with honest preferred size.</summary>
public class CaptionControl : Control
{
    private const TextFormatFlags Flags =
        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;

    /// <summary>Enables managed painting and resize redraw.</summary>
    public CaptionControl()
    {
        SetStyle(ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw, true);
    }

    /// <inheritdoc/>
    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font, Size.Empty, Flags);

        return new Size(
            Math.Max(MinimumSize.Width, text.Width + Padding.Horizontal),
            Math.Max(MinimumSize.Height, text.Height + Padding.Vertical));
    }

    private void ContentChanged()
    {
        Parent?.PerformLayout(this, nameof(PreferredSize));
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        ContentChanged();
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ContentChanged();
    }

    /// <inheritdoc/>
    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        ContentChanged();
    }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Rectangle content = new(
            Padding.Left, Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Horizontal),
            Math.Max(0, ClientSize.Height - Padding.Vertical));
        TextRenderer.DrawText(e.Graphics, Text, Font, content, ForeColor, Flags);
    }
}
```

AutoSize/parent layout must actually use PreferredSize; merely overriding it doesn't invent a universal self-sizing engine. Bounds clamping belongs in SetBoundsCore with BoundsSpecified respected. ForeColor/ambient fonts preserve consumer choices; themed rich palettes have separate rendering rules.

## Native scrollbar opt-in

```csharp
using System.Windows.Forms;

/// <summary>Opts a native-scrolling control into supported framework theming.</summary>
public class ThemedScrollControl : Control
{
    /// <inheritdoc/>
    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            CreateParams parameters = base.CreateParams;

            return parameters;
        }
    }
}
```

This demonstrates the ordering contract; add documented native scrollbar styles/scroll logic only when required. Setting the style in a derived constructor can miss early CreateParams evaluation. Framework targets without ApplyThemingImplicitly require a separate supported alternative.

## Initialization batching

```csharp
using System.ComponentModel;
using System.Windows.Forms;

/// <summary>Defers expensive property-dependent work during Designer initialization.</summary>
public class InitializedControl : Control, ISupportInitialize
{
    private bool _initializing;
    private int _minimum;

    /// <summary>Gets or sets a layout-affecting value.</summary>
    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (!_initializing)
            {
                PerformLayout();
                Invalidate();
            }
        }
    }

    /// <summary>Begins a property initialization batch.</summary>
    public void BeginInit() => _initializing = true;

    /// <summary>Recalculates once from the completed property set.</summary>
    public void EndInit()
    {
        _initializing = false;
        PerformLayout();
        Invalidate();
    }
}
```

Real range constraints/resources still need domain validation. Runtime setters outside BeginInit/EndInit must remain functional. Early base virtual calls must tolerate not-yet-initialized child fields; font propagation logic especially needs construction guards.
