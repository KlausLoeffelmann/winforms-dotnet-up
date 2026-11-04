---
name: warp-directx-using-visuals
description: >-
  Composes WARP retained DirectComposition layers for animated dashboards, sprites
  and kiosk scenes. Paints cached content on the correct thread, changes bounds/
  transforms/z-order without redraw and batches cancellable frame commits.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.DirectX\\Controls\\D2DVisual.cs; D2DVisualCollection.cs; D2D\\D2DVisualPaintEventArgs.cs; src\\WarpToolkit.Windows.Interop\\PrecisionTimer"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Retained visual content and animation

## Critical constraints

- Essential mode/snapshot/COM/resource rules remain in `warp-directx-getting-started`.
- PaintContent is the portable content path in every **Direct2D** mode, not GDIPlus.
  Graphics is already begun/precleared; do not retain it or begin/end/present again.
- Bounds/Visible/Transform/z-order are thread-safe intent; pixel drawing is
  render-thread-affine. UI-thread FromVisual is valid naturally only in classic mode.
- Repaint cached content only for changed appearance; create visuals once and move/
  transform them without recreating or repainting each frame.
- Await one CommitVisualsAsync per logical frame, not per visual; do not block
  UI/render threads with GetAwaiter().GetResult().
- Timer callbacks are not implicitly UI-thread callbacks; never read live
  ClientRectangle or other Control state there.

## API and content

Controls namespace: D2DVisual/collection/RenderMode; D2D namespace: graphics/event args.
Hosts expose Visuals:

| Operation | Contract |
|---|---|
| AddNew | Rectangle or Point+Size → D2DVisual |
| Remove | Remove a retained visual |
| Bounds / Visible | Move/resize and show/hide |
| Transform / SetRotation / SetTransform | Affine compositor transform |
| BringToFront / SendToBack | Retained z-order |
| InvalidateContent | Thread-safe request for repaint |
| CommitVisualsAsync(waitForVSync, cancellationToken) | Apply pending intent on render thread |

The collection includes a nonremovable default visual at index zero:
DefaultVisual and RootVisual refer to it. Remove returns bool and throws if asked
to remove that default surface.

```csharp
D2DVisual visual = host.Visuals.AddNew(bounds);
visual.PaintContent += OnPaintContent;
await host.CommitVisualsAsync(waitForVSync: false, cancellationToken);

static void OnPaintContent(object? sender, D2DVisualPaintEventArgs e)
{
    e.Graphics.FillRectangle(Color.CornflowerBlue,
        new RectangleF(PointF.Empty, e.ContentSize));
}
```

ContentSize is **Size in pixels**; coordinates start at the visual's top-left.
Framework clears transparent before each complete repaint, presents automatically
and reissues PaintContent after creation, resize, device loss and mode changes.
InvalidateContent schedules changed data; snapshot any UI-dependent fonts/colors.
Do not rely on old backbuffer pixels surviving the callback.

D2DGraphics.FromVisual/visual.GetGraphics is an immediate drawing path only when
already on the owning render thread; in classic mode that is the UI thread.
It throws for wrong-thread use in background modes. Use PaintContent instead of
building an application dependency on internal render dispatch.

## Z-order and loop shape

Immediate/default host surface is anchored at the **bottom**.
Retained visuals stack in collection order; last added is frontmost.
SendToBack stays above the immediate background; transparent pixels reveal lower
layers.

1. Tick immutable simulation from Stopwatch elapsed time/FrameDelta.
2. Apply Bounds/Visible/Transform; invalidate only changed appearance.
3. Await one commit for related intent.
4. Request presentation through the chosen driver/UI-safe invalidation path.

PreserveLastFrame=true preserves a frame with no drawing; VSyncEnabled=false plus
TargetFrameRate (e.g. 60 Hz) selects timer pacing. Background continuous/on-demand
drivers are distinct; use the getting-started contracts rather than assume the
knobs themselves create a UI animation loop.

## High-precision timer lifetime

WarpToolkit.Windows.Interop.PrecisionTimer.HighPrecisionTimer supports synchronous
and `Func<TTarget,CancellationToken,ValueTask>` callbacks. A safe timer frame owns
only immutable scene extents/model snapshots, updates thread-safe visual intent and
awaits CommitVisualsAsync; UI changes marshal through `winforms-async-apis`.

PeriodicTimerOptions can use Interval=TimeSpan.FromMilliseconds(1000d/60d),
MaxConcurrency=1 and OverloadPolicy.SkipAndLog. Keep a registration handle,
unregister it at stop and await drained work/DisposeAsync as appropriate before
disposing the host/resources. Do not leave a fire-and-forget commit running after close.
Elapsed time uses Stopwatch.GetTimestamp/GetElapsedTime, not low-resolution wall clock.

## Final checks

1. Content-local repaint/recovery and owning-thread graphics use are correct.
2. One awaited logical-frame commit and explicit timer shutdown prevent overlap.
3. Retained resource granularity, snapshots and z-order fit the scene.
