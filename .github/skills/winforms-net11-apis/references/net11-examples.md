# Version-qualified .NET 11 examples

## Contents

- [App defaults](#app-defaults)
- [System settings](#system-settings)
- [Painting scope and kiosk](#painting-scope-and-kiosk)
- [Verification boundaries](#verification-boundaries)

## App defaults

Compile using the verified RC3 target. In actual startup, call the defaults before creating windows:

```csharp
using System.Windows.Forms;

/// <summary>Configures explicitly opted-in .NET 11 defaults.</summary>
public static class Net11Defaults
{
    /// <summary>Sets application rendering and deferred reveal before windows exist.</summary>
    public static void Configure()
    {
        Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11);
        Application.SetDefaultFormRevealMode(FormRevealMode.Deferred);
    }
}
```

For VB Application Framework, assign supported `ApplyApplicationDefaultsEventArgs.VisualStylesMode` and `.FormRevealMode` in its defaults event. Don't call the write-once visual default repeatedly, including after a previous conflicting initialization.

## System settings

```csharp
using System.Windows.Forms;

/// <summary>Responds to live system visual state without a static subscription.</summary>
public class VisualSettingsPanel : Panel
{
    /// <inheritdoc/>
    protected override void OnSystemVisualSettingsChanged(SystemVisualSettingsChangedEventArgs e)
    {
        base.OnSystemVisualSettingsChanged(e);
        if ((e.Changed & SystemVisualSettingsCategories.TextScale) != 0)
        {
            PerformLayout();
        }

        Invalidate();
    }
}
```

Use Changed flags to restrict work; NewSettings supplies the new immutable snapshot. Relevant categories cover accent, text scale, High Contrast, client animations, keyboard cues and focus metrics. Snapshots expose AccentColor, TextScaleFactor, HighContrastEnabled, ClientAreaAnimationEnabled, KeyboardCuesVisible, FocusBorderMetrics. Text scale is independent of DPI; respect disabled animations and High Contrast.

Use static Application.SystemVisualSettingsChanged only for appropriate application-lifetime services, or unsubscribe. Renderer-aware controls inspect EffectiveVisualStylesMode and implement minimal change impact; consult target signatures rather than copying raw inherited mode into caches.

## Painting scope and kiosk

```csharp
using System;
using System.Windows.Forms;

/// <summary>Demonstrates verified painting and kiosk API shapes.</summary>
public static class Net11Operations
{
    /// <summary>Batches one short UI-thread mutation scope.</summary>
    public static void Mutate(Control target, Action mutation)
    {
        using IDisposable scope = target.SuspendPainting(LayoutSuspendTraversal.TargetOnly);
        mutation();
    }

    /// <summary>Creates a kiosk component whose caller owns disposal.</summary>
    public static KioskModeManager CreateKiosk(Form owner)
    {
        KioskModeManager kiosk = new();
        kiosk.ContainerControl = owner;
        kiosk.ToggleFullScreenKeys = Keys.F11;
        kiosk.EscapeExitsFullScreen = true;
        kiosk.TopMostInFullScreen = true;
        kiosk.MousePointerAutoHideDelay = 3000;

        return kiosk;
    }
}
```

The delay is an example policy, not a mandated default. Use component-container ownership in designed apps. Configure/observe FullScreen and FullScreenChanged according to actual target; power `AlwaysOn` can throw Win32Exception, so surface failure. Toggle keys are app-local, not global hotkeys or an OS security kiosk.

RC3 traversal values are None, TargetOnly, TargetAndDescendants. Predicate/scoping overloads and custom ISupportSuspendPainting hooks must be checked against target signatures. A scope may span await where documented, but avoid visibly freezing UI with long painting suspension.

## Verification boundaries

ToggleSwitch enum availability does not establish support on arbitrary controls. Verify renderer/control combinations. Keep adaptive layout and focus contrast around modern metrics.

SortGlyphColor Color.Empty delegates contrast to framework. Explicit print-preview ForeColor can change preview message and page-background behavior; test custom palettes.

The installed APIs have no Experimental attributes, but this version qualification is still a prerelease target contract. Recompile and retest when the target SDK changes. No source checkout or undisclosed unshipped API surface is assumed.
