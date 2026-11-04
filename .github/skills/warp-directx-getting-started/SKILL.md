---
name: warp-directx-getting-started
description: >-
  Adds first hardware-accelerated shapes/text to a WinForms control, panel or Form
  through WARP Direct2D/DirectWrite. Selects an appropriate render backend and
  diagnoses cross-thread Control reads, COM/resource-domain misuse or unsupported
  GDI+ assumptions in dashboards, animations and custom surfaces.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.DirectX\\Controls; D2D"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# First accelerated pixels

## Critical constraints

- Start with D2DWinFormsClassic and a render callback, not copied COM/DXGI setup.
  ID2DGraphics is conceptually GDI+-friendly, **not every System.Drawing feature**.
- Each host owns its D3D/D2D/DComp/DirectWrite resource domain. Never share device
  managers/factories/resources across hosts, threads or render modes.
- Background callbacks consume immutable snapshots; never read live Control.Handle,
  ClientSize, Text, Font or child state there. WinForms input/layout remains UI-owned.
- Frame ID2DGraphics is callback-scoped: do not retain it or manage its draw/present
  bracket. Retained content uses D2DVisual.PaintContent on the owning render thread.
- Application-created GPU images/resources must be created, updated, drawn and
  disposed on their owning render thread and rebuilt after mode/device changes.
  Native COM apartment rules also apply; read `winforms-com-apartments`.
- Dedicated rendering is scarce process-budgeted capacity and may throw; choose an
  explicit shared-mode fallback, not a silent broken surface.

## Host and namespace

Package WarpToolkit.WinForms.DirectX; inspect the consuming TFM/package.
Source Windows targets include net10/net11 with windows10.0.22000.0 platform version;
“.NET11 source available” does not establish package publication or framework GA.

```csharp
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.DirectX.D2D;
```

| Host | Actual base / use |
|---|---|
| D2DControl | Control; leaf accelerated surface, custom control base |
| D2DPanel | D2DControl; container for ordinary WinForms child HWNDs |
| D2DForm | **Form**, not D2DControl; accelerated top-level window |

Ready-made D2DButton/D2DLabel/D2DPictureBox and document controls can avoid new
subclasses. Use `winforms-custom-and-usercontrols` / `winforms-designer-code`
only when authoring/placing controls; shared layout belongs to
`winforms-high-dpi-fluent-layout`.

## Render callback

D2DPanel exposes RenderBackground/OnRenderBackground; D2DControl and D2DForm
expose Render. D2DRenderEventArgs supplies ID2DGraphics Graphics,
TimeSpan FrameDelta and monotonically increasing FrameIndex.
Familiar verbs include Clear/DrawLine/DrawRectangle/FillRectangle/DrawEllipse,
DrawString/MeasureString/DrawImage, Transform/ClipBounds/AntialiasMode.

```csharp
internal sealed class StatusCanvas : D2DPanel
{
    public StatusCanvas()
    {
        RenderBackground += OnRenderBackground;
    }

    private void OnRenderBackground(object? sender, D2DRenderEventArgs e)
    {
        e.Graphics.Clear(Color.Black);
        e.Graphics.FillRectangle(Color.CornflowerBlue,
            new RectangleF(20, 20, 240, 120));
        using Font font = new("Segoe UI", 14f, FontStyle.Bold);
        e.Graphics.DrawString("Ready", font, Color.White, 32f, 40f);
    }
}
```

This normal-code control uses the default UI-thread mode. Framework handles
BeginDraw/EndDraw/presentation/resize/recovery. Place it declaratively in the
owning Form Designer; Invalidate requests repaint. For a D2DForm, keep normal
Designer initialization and attach a named Render handler after construction.
Do not replace D2D callbacks with OnPaint/CreateGraphics GDI drawing.

## Backend contract

| RenderMode | Driver / ownership |
|---|---|
| GDIPlus | Normal WinForms painting; no ID2DGraphics/retained D2D resources |
| D2DWinFormsClassic (default) | WM_PAINT, UI thread, short invalidate-driven work |
| D2DSharedRenderThread | Process-wide high-precision background clock; snapshot state |
| D2DDedicatedRenderThread | Per-host background thread; hard budgeted request |

PreserveLastFrame defaults true: **no drawing** preserves presentation; it is not
a general guarantee that flip-discard backbuffer pixels survive incremental drawing.
Set false for complete game frames cleared to BackColor.
TargetFrameRate defaults 50 Hz and drives timer modes when VSyncEnabled=false;
VSyncEnabled defaults depend on mode. RenderBackgroundFramesOnDemand=true makes
background loops invalidation-driven; continuous games intentionally disable it.
D2DSharedRenderClock.Frame can coordinate simulation before shared hosts render.

## Conditional reading

- [Architecture and advanced compositions](references/architecture.md): read for
  static/dynamic scene boundaries, HWND-less lists/scrollbars, media image updates,
  missing native texture/popup APIs and source-unavailable extension design.
- `warp-directx-using-visuals`: independent retained layers/animation/batched commits.
- `warp-directx-dcomp-exclusive-tlw`: all-DComp top-level exclusivity, not needed
  merely to accelerate a panel or host classic controls.
- `warp-directx-text-editors`: current editor/document surfaces; verify availability, then do not derive
  another editor merely to change options.
- `winforms-rendering`: GDI fallback/theming/resources; `winforms-net11-apis` only
  for related framework preview APIs.

## Final checks

1. Every callback reads only state/resources valid for its thread/domain.
2. Frame graphics is not retained; native/GPU cleanup and recovery are owned.
3. Chosen mode/capability/fallback and complete-versus-retained drawing are explicit.
