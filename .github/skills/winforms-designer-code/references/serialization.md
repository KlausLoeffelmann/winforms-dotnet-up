# Serialization examples

## Partial skeleton

Complete .NET 10 partial pair shown in one block. An application's Designer normally controls qualification/nullability style; keep this structure rather than introducing helpers for static assignments:

```csharp
using System;
using System.Windows.Forms;

/// <summary>A Designer-compatible shell with component ownership.</summary>
public partial class DesignerShell : Form
{
    /// <summary>Initializes the shell.</summary>
    public DesignerShell()
    {
        InitializeComponent();
    }

    private void InspectClick(object? sender, EventArgs e)
    {
        Text = "Inspected";
    }
}

#nullable disable
public partial class DesignerShell
{
    private System.ComponentModel.IContainer components;
    private ToolStrip _toolbar;
    private ToolStripButton _inspect;
    private PictureBox _preview;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _toolbar = new ToolStrip();
        _inspect = new ToolStripButton();
        _preview = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
        SuspendLayout();
        _toolbar.SuspendLayout();
        _inspect.Text = "Inspect";
        _inspect.Click += InspectClick;
        _toolbar.Items.Add(_inspect);
        _preview.Dock = DockStyle.Fill;
        _toolbar.Dock = DockStyle.Top;
        Text = "Preview";
        Controls.Add(_preview);
        Controls.Add(_toolbar);
        _toolbar.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }
}
#nullable restore
```

No label/editor/footer recipe is independently defined here; use the canonical layout examples when adding them.

## Resources and initialization

Designer-supported resource patterns include a local ComponentResourceManager for the Form type, `ApplyResources(control,"name")` and supported resource casts. Use actual `.resx` entries; don't call arbitrary static icon/image factories and assume changes survive a roundtrip.

For custom controls implementing ISupportInitialize, generated BeginInit/EndInit surrounds interdependent property assignments. Expensive reconfiguration is deferred inside the control, not through a generated UI helper.

Custom disposal needs one override: prefer container-owned components and explicit owner cleanup hooks, merge necessary additional cleanup deliberately, and confirm regeneration preserves it. Don't define competing Dispose overrides in two partial files.

## VB differences

Generated fields commonly use `Friend WithEvents`. A named handler in the main partial uses `Handles _inspect.Click`; alternatively preserve explicit `AddHandler ... AddressOf` where used. Event body logic and async exception handling do not belong in InitializeComponent.

## Roundtrip checklist

Open in supported Visual Studio, change a property, save, close/reopen and inspect serialized source/resources. Compilation proves neither serialization nor correct property defaults. If unsupported source/factory expressions disappear, move runtime behavior to main code or implement an appropriate converter/serializer; do not hide it in generated helper methods.
