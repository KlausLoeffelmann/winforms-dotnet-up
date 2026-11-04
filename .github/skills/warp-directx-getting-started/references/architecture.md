# DirectX architecture and capability boundaries

Read when constructing layered/static scenes, media surfaces, complex controls or
an extension absent from the public package. Essential threading/resource rules
remain in the parent SKILL.md; this reference supplies their advanced context.

## Contents

- Architecture and ergonomics
- Static, immediate and retained content
- Continuous loops
- Camera/media pipeline
- Virtualized lists and documents
- Popup/window concerns
- Source-unavailable workflow

## Architecture and ergonomics

WARP's goal is first accelerated pixels without learning COM, swap chains or
DirectComposition setup. Familiar Color/Point/Rectangle/Font/Pen/Brush/Image and
roughly the everyday GDI+ drawing vocabulary make ports source-friendly, not
feature-complete or automatic performance wins.

```text
WinForms HWND/input/layout/focus/accessibility
  -> WARP host lifetime/render policy/thread ownership
     -> D3D/DXGI flip-model presentation buffers
     -> Direct2D/DirectWrite drawing
     -> DirectComposition HWND target/retained visual tree
        -> Windows DWM
```

Direct2D draws pixels, DXGI presents and DirectComposition retains/positions/
combines surfaces. Retained movement/transforms do not require reissuing drawing.
WinForms still owns handles, keyboard/mouse/focus and accessibility.
Do not rebuild this infrastructure in application code.

## Static, immediate and retained content

| Level | Public building blocks | Appropriate granularity |
|---|---|---|
| Immediate frame | Render/RenderBackground, ID2DGraphics | Small whole-frame surface |
| HWND-less interactive tree | RootElement, D2DElement, D2DElementContainer | Logical hit-test/focus/input/layout elements |
| Retained compositor layer | Visuals, D2DVisual | Independently moved/hidden/transformed cached layers |

Every retained visual has presentation resources. Avoid one visual per trivial
cell/row/glyph: paint dense logical items into one surface and reserve visuals
for coarse independent overlays/sprites/backgrounds.

D2DPanel caches its background. Subscribe RenderBackground and call
InvalidateBackground when background appearance/data changes. For example, an
Accent setter compares its old value, stores the new Color and invalidates the
background; its callback draws a complete background plus static accent shapes.
Classic mode can read UI state directly; background mode must use snapshots.
Keep dynamic foreground in child D2D content or another visual.

Drawing dynamic foreground into an immediate flip-discard swapchain still requires
a complete frame; backbuffers are not a scene store. PreserveLastFrame only keeps
presentation when no draws occur. A retained visual repaints from transparent on
PaintContent and automatically reconstructs after creation/resize/device loss/mode
change. InvalidateContent only for changed appearance, not movement.

## Continuous loops

Publish an immutable SceneSnapshot with Volatile.Write; render reads it with
Volatile.Read. Shared mode with RenderBackgroundFramesOnDemand=false,
PreserveLastFrame=false and deliberate VSync paints complete continuously changing
frames. Use FrameDelta for frame-rate-independent simulation; shared clock Frame
can advance one consistent step before all hosts.

Snapshot-driven controls instead enable on-demand background frames and invalidate
when the model changes. Dedicated mode is only for measured heavy independently
paced work; assignment can throw InvalidOperationException when capacity is exhausted.
Batch visual intent changes in one awaited CommitVisualsAsync per logical frame.

## Camera/media pipeline

There is no verified public camera capture service, media decoder, audio pipeline,
transport clock or ready-made camera/video control in DirectX.

1. Capture/decode outside rendering.
2. Publish latest immutable frame; drop obsolete frames rather than accumulate latency.
3. Request a frame in the chosen driver.
4. Render decoded data in the owning callback.
5. Dispose old frames deterministically after their consumers finish.

DrawImage(Image, ...) is the easy path but converts the GDI+ source.
`e.Graphics.FromImage(image)` creates reusable ID2DImage; BitmapBytes is a Span
and ApplyBitmapBytes updates its pixel data. Width/Height, GetPixel/SetPixel and
IDisposable are public. Create/update/draw/dispose in the owning resource domain,
and recreate after device loss or mode changes.

Native DXGI texture import/zero-copy interop is a capability to **verify**, not
permission to invent internal swapchain/device access. If absent, design a narrow
public extension and disclose copy/online/decoder dependencies.

## Virtualized lists and documents

No public ModernListView is established by this package. A new one can derive
D2DControl and use a D2DElementContainer or DocumentControlVisual RootElement.
Compose D2DVScrollbar/D2DHScrollbar as HWND-less elements, virtualize viewport plus
small overscan, hit-test logical rows/headers, keep selection/focus/editing/binding
in the model/control layer and publish render snapshots.

DocumentControlVisual already offers virtual extents, viewport transforms/zoom,
scroll offsets and composed scrollbars; reuse only when that document model fits.
Shared on-demand rendering often suits a large snapshot-driven list.
Use coarse retained visuals for drag images/animated overlays, not every cell.
Implement keyboard conventions, DPI/theme, Designer behavior and UI Automation:
HWND-less elements do not automatically become accessible WinForms children.

## Popup/window concerns

No public D2DPopup/PopupWindow abstraction is promised; the earlier popup issue
was a future proposal, not evidence of implementation.

| Need | Supported composition |
|---|---|
| Accelerated popup with own HWND | Owned borderless D2DForm |
| Popup with classic child controls | Owned ordinary Form plus D2DPanel/Control |
| Host-constrained overlay | D2DVisual or D2DElement |
| Entire all-DComp kiosk | Opt-in exclusive D2DForm |

Popup rendering does not solve owner/activation/focus restoration, keyboard and
outside-click dismissal, modality, working-area placement, per-monitor DPI,
shadows/corners/topmost semantics, accessibility or deterministic disposal.
Classic children prohibit exclusive top-level rendering.

## Source-unavailable workflow

Installed public assemblies and matching generated references are authoritative.
Prefer public hosts/callbacks/elements/visuals/images; do not use reflection to
internal COM objects, swapchains, device/resource caches or budget controls.
Request missing seams explicitly, keep fallback visible and do not claim inferred
capabilities. Maintain the separation: window semantics/input, drawing,
presentation and composition have different owners.
