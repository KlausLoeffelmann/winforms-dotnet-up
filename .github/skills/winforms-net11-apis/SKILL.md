---
name: winforms-net11-apis
description: >-
  Uses verified .NET 11 WinForms renderer, deferred reveal, toggle appearance,
  live Windows text/accessibility settings, painting suspension and kiosk APIs
  when the project explicitly targets that release. Applies to modern control
  metrics, accent/High Contrast changes, bulk repaint churn, presentation mode,
  sort-glyph contrast or print-preview colors; preserves older-target fallbacks.
---

# .NET 11 target-specific APIs

## Durable rules

- Use only for an explicitly appropriate .NET 11 target; do not silently upgrade.
- This contract is verified against **11.0.100-rc.3.26522.148 / 11.0.0-rc.3.26522.148**. The release was prerelease; absence of Experimental attributes does not declare GA.
- Installed reference assembly and compilation are authoritative for exact names/overloads; documentation prose can lag or contain errors.
- Opt-in renderer metrics do not supersede [adaptive layout](../winforms-high-dpi-fluent-layout/SKILL.md), UI ownership or High Contrast.
- For multi-targeted implementations, isolate supported calls behind target guards and retain an older-target fallback.

## Renderer and reveal

Verified enums/APIs: `VisualStylesMode.Classic`, `Disabled`, `Net11`, `Latest`, `Inherit`; `Application.SetDefaultVisualStylesMode`; ambient `Control.VisualStylesMode`; protected `EffectiveVisualStylesMode`, `OnVisualStylesModeChanged`, `GetVisualStylesModeChangeImpact`.

Initialize the application's visual styles before selecting modern defaults, and do so before windows. The renderer default setter is write-once and does not accept Inherit. `Latest` deliberately accepts future changes. Use subtree overrides for compatibility boundaries. Base renderer-aware decisions on effective, not raw inherited, mode; don't cache it. Return the narrowest valid change impact (`None`, `Repaint`, `NonClientUpdate`, `Metrics`, `Recreate`) and call base change handlers.

Modern painting/metrics cover supported Button/CheckBox/RadioButton/GroupBox/TextBox/RichTextBox and other documented controls. Text padding needs supported modern mode. `Button.UseVisualStyleBackColor` controls button background behavior; it is **not** renderer selection or a substitute for VisualStylesMode.

`FormRevealMode.Deferred` and `Application.SetDefaultFormRevealMode` reduce initial reveal flash. Reveal default may be changed; untouched inherited defaults in dark-mode apps resolve according to framework policy. Choose deliberately and test startup; don't assume painting is cost-free.

`Appearance.ToggleSwitch` exists in the verified surface. Use supported toggle controls for immediate binary settings, not submission or multi-step actions; verify control renderer support rather than inferring every `Appearance` consumer implements it.

## System state, batching and kiosk

Read [verified examples](references/net11-examples.md) for C#/VB defaults, system settings events, painting scope and kiosk configuration:

- `Application.SystemVisualSettings`: immutable accent, text scale, High Contrast, client animation, keyboard cues and focus metrics.
- `OnSystemVisualSettingsChanged`/event: inspect `Changed` flags and `NewSettings`; prefer control-lifetime handlers. Unsubscribe short-lived static-event consumers.
- Respect text scale independently of DPI, system focus/cue settings and disabled animations; no hard-coded High Contrast override.
- `SuspendPainting`/`ControlMutationExtensions`: disposable narrow scope; **actual RC3 `LayoutSuspendTraversal`: None, TargetOnly, TargetAndDescendants**. Do not use website sample `Target` or invent `All`.
- Custom controls may integrate `ISupportSuspendPainting` or begin/end hooks. Scope is idempotent and can span await where supported, but keep suspension short and exception-safe.
- `KioskModeManager`: local toggle keys, Escape, topmost, mouse-pointer delay, FullScreenChanged; `AlwaysOn` power requests may fail and must be reported.
- `DataGridViewCellStyle.SortGlyphColor`: leave Color.Empty for framework contrast or choose accessible explicit color.
- `PrintPreviewControl.ForeColor`: no-content text and explicitly assigned page-background behavior; use theme-aware colors.

`TreeView.NodeLeading` is **absent**; use supported `ItemHeight` for required row spacing. Do not invent kiosk wake commands, `HideTaskbar`, or `SuppressPowerSaving` (`AlwaysOn` is the verified power property).

## Final checks

1. Compile against exact intended target/refpack, including enum names and control support.
2. Test renderer metrics, High Contrast/text scale, reveal, mutation cleanup and kiosk exit/power failure.
3. State prerelease/runtime verification boundaries and keep older-target behavior intact.

Source: [official .NET 11 changes](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net110), checked against the version-qualified references above; no source checkout is assumed.
