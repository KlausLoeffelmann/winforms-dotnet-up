---
name: warp-fluent-tab-control
description: >-
  Builds WARP navigation/settings/diagnostics tabs with one Designer-friendly
  UserControl per page, sizes the host from natural content within screen/parent
  caps and adds content-owned scrolling when tabs are clipped.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\Containers\\FluentTabControl"
---

# Fluent tabs

## Critical constraints

- Exactly one UserControl type per visual tab; do not parent loose controls into
  page panels.
- AddTab is runtime state, **not InitializeComponent**. Designer declares the tab
  control and separate UserControls; use `winforms-designer-code`.
- Shared UserControl/group/field layout belongs to `winforms-high-dpi-fluent-layout`.
  Tab host caps below are WARP-specific, not a button-width workaround.
- Tools/Options must also load `winforms-options-dialog`: category grouping,
  label/value controls, nested value/picker tables, row spacing, accessibility
  and draft commit/cancel rules are identical with or without WARP. Use this
  FluentTabControl instead of native TabControl styling or a custom emulation.
- Capture natural content size **before docking**; scrolling belongs to the
  UserControl/content viewport, never the tab strip itself.
- DI-aware tabs use `warp-winforms-application-builder`; do not duplicate its
  façade or resolve async/UI services in Designer construction.

## API and workflow

Package **WarpToolkit.WinForms**, namespace WarpToolkit.WinForms.Containers
(not a separate Containers package).
FluentTabControl derives Panel:

| Member | Contract |
|---|---|
| AddTab | void AddTab(string tabPageTitle, ContainerControl tabContent) |
| CurrentTabIndex | int get/set |
| CurrentTab | Control? getter |
| Tabs | IEnumerable&lt;Panel&gt; page hosts |
| TabChanged | EventHandler? |

There is no TabPage type or documented SelectedTab binding property.
AddTab wraps content in a Panel, docks it Fill and adds a MenuStrip header item.
Header font tracks Font+1pt; header rendering chooses light/dark styling.

Category headers are deliberately text-based, not ordinary icon commands.
Keep meaningful names, keyboard focus/activation and selected-category indication;
verify the installed renderer at large fonts and High Contrast. Do not replace
its navigation solely to paint another selection underline.

```csharp
// Regular OnLoad/configuration; each view is a Designer-created UserControl.
fluentTabs.AddTab("Chat", chatView);
fluentTabs.AddTab("Settings", settingsView);
fluentTabs.AddTab("Diagnostics", diagnosticsView);
```

Each UserControl owns its Designer file, initialization/events and optional VM/scope.
Designer-set Size or an intentional PreferredSize override provides natural size.
DI-created views must be registered and resolved in the host's valid lifetime scope.
Dynamic document tabs follow the same UserControl rule; inspect RoslynDocumentView
constructors/LoadDocument in the installed version rather than copying an old
Chatty sample overload blindly.

## Host sizing policy

1. Record each tab's desired content extent before AddTab docks it.
2. Aggregate maximum width/height; add the **measured header height exactly once**
   and any actual surrounding layout/chrome.
3. For top-level hosts cap to 75% of the current screen **WorkingArea**.
   For embedded hosts also cap to 90% of the **actual containing control's**
   client area; use the more restrictive applicable cap.
4. Keep client sizes and window MinimumSize/chrome conversions in consistent units.
   If a minimum cannot fit a cap, surface the conflict rather than silently exceed it.
5. After layout compare saved natural extents against each tab's available content
   viewport—not post-docking Size or the whole Form's ClientSize.

Do this after construction/settings at the appropriate layout boundary, not in
InitializeComponent. Recompute for dynamic tabs, DPI/host changes as needed.
Do not size the form from arbitrary fixed pixel additions or assume a 1080p monitor.

## Two scrolling approaches

| Requirement | Approach |
|---|---|
| Whole tab content scrolls | UserControl.AutoScroll=true with AutoScrollMinSize reflecting the saved natural content extent |
| Pinned toolbar plus scrolling fields/content | Canonical TLP root with AutoSize toolbar row and dedicated flexible viewport; Panel.AutoScroll=true contains a natural-sized/top-anchored inner content surface |

Dock Fill on the immediate scroll content can shrink it to the viewport and remove
the overflow. AutoScrollMinSize or a correctly sized inner content surface must
preserve the scroll extent. Follow shared layout rules for the inner field/group
content; do not treat all UserControl roots as fixed-size canvases.

Do not enable scrolling reactively using `uc.Size` after docking: it no longer
contains the natural size. Preserve that value separately and update the extent
when content changes.

## Final checks

1. Every tab has one separately Designer-openable UserControl.
2. Header/natural/client/window sizes are not double-counted or mixed.
3. Clipped content scrolls in its tab, with correctly scoped runtime/DI lifetime.
