---
name: warp-directx-dcomp-exclusive-tlw
description: >-
  Builds an explicitly all-DirectComposition WARP kiosk/fullscreen Form without
  a GDI redirection bitmap. Diagnoses classic child controls on exclusive windows
  and sets the top-level style before HWND creation.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.DirectX\\Controls\\D2DForm.cs; D2DControl.cs; D2DPanel.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Exclusive DirectComposition top-level

## Critical constraints

- Read `warp-directx-getting-started` for essential render/COM/GPU lifetime rules.
- D2DForm.UseDirectCompositionExclusiveRendering requests
  WS_EX_NOREDIRECTIONBITMAP; default false preserves classic child hosting.
- Decide **before handle creation**; changing effective value afterward throws.
- Every child of an exclusive top-level must be D2D/DComp content. Ordinary
  Button/TextBox/Panel/DataGridView children need redirection support.
- Acceleration does not require exclusivity; keep default nonexclusive Form
  when menus/toolbars/classic child controls are needed.

## Host distinction

D2DForm derives Form. Its protected virtual
DefaultUseDirectCompositionExclusiveRendering returns false; it is **not a public
default getter**. ResetUseDirectCompositionExclusiveRendering restores that default.

Leaf D2DControl always carries the exclusive style; D2DPanel remains a nonexclusive
container with child clipping/background erase support. Do not invent the Form flag
on child controls. D2DCompositionTuning compile-time switches are internal tuning,
not application instance knobs. The earlier D2DPopup proposal/issue is future status.

## Workflow

Use the Windows-targeted DirectX stack, derive a Form and configure its policy early:

```csharp
internal sealed partial class KioskForm : D2DForm
{
    public KioskForm()
    {
        InitializeComponent();
        Render += OnFrameRender;
    }

    private void OnFrameRender(object? sender, D2DRenderEventArgs e)
    {
        e.Graphics.Clear(Color.Black);
        using Font font = new("Segoe UI", 48f, FontStyle.Bold);
        e.Graphics.DrawString("KIOSK READY", font, Color.White, 80f, 80f);
    }
}
```

Keep static configuration in the Designer partial; this scene needs no child controls:

```csharp
partial class KioskForm
{
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        UseDirectCompositionExclusiveRendering = true;
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        Name = "KioskForm";
        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private System.ComponentModel.IContainer components;
}
```

Retain an existing InitializeComponent and set the style before anything creates
the handle. Read `winforms-designer-code` and `winforms-high-dpi-fluent-layout` for
associated Form structure/fullscreen state; do not manufacture a layout helper.

Benefits are no top-level GDI redirection allocation/copyback and flip-model DWM
composition. OS title bar/borders remain unless chrome is removed; client-area
composition is not a custom nonclient-window implementation.

Read `warp-directx-using-visuals` for layered animation and bottom immediate-surface
z-order. All render modes/PreserveLastFrame/VSync/target-rate rules remain those of
the getting-started owner; background visual painting must use the owning callback.

## Final checks

1. No classic child HWNDs depend on the missing top-level surface.
2. Exclusivity is set before handle creation and is actually needed.
3. Rendering/resource lifetime, window chrome and animation policy are explicit.
