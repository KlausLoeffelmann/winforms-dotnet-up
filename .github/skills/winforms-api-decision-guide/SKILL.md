---
name: winforms-api-decision-guide
description: >-
  Selects WinForms skills and APIs for whole-feature requests, initial design,
  choosing a control or library, and planning settings dialogs, customer entry,
  preferences pages, resizable windows, background loading, custom controls,
  deployment, or maps. Uses specialized skills for implementation details
  rather than replacing their contracts.
---

# WinForms API decision guide

## Durable rules

- Inspect target framework, language, Designer use, and existing architecture before selecting APIs; do not silently upgrade the application.
- Select by the user's ordinary requirement, not only API names. Load only relevant owners; multi-domain work legitimately needs several.
- Treat each selected owner's constraints as implementation requirements. Carry its critical constraints into the working checklist.
- Re-open relevant `SKILL.md` files after context compaction, resuming a long task, or before changing implementation phase. Do not rely on a compressed recollection of exact layout or threading rules.
- Keep static UI Designer-compatible, runtime work UI-thread-safe, and font/DPI/accessibility requirements visible throughout implementation.

## Requirement → owner

| Request | Read when this work is required |
|---|---|
| New application/library, target/framework choice, startup, ordinary C#/VB conventions | [Development](../winforms-development/SKILL.md) |
| Tools/Options, preferences categories, font/file/color picker settings | [Options dialogs](../winforms-options-dialog/SKILL.md), plus layout and Designer; with WARP also load `warp-fluent-tab-control` |
| Command toolbar icons, mixed ToolStrip buttons/labels/editors, image factory without WARP | [ToolStrip icons](../winforms-toolstrip-icons/SKILL.md); with WARP use `warp-toolstrip-icons` |
| Customer entry, labels clipped by larger text, resizing/DPI, button footer | [Layout](../winforms-high-dpi-fluent-layout/SKILL.md); [Designer](../winforms-designer-code/SKILL.md) when creating/editing static Forms or UserControls |
| Reusable address editor, wizard/tab content, custom badge/slider, control property | [Custom/UserControls](../winforms-custom-and-usercontrols/SKILL.md); layout for composition, Designer for property serialization |
| Save/search binding, current customer/orders, list updates, validation/ErrorProvider | [Databinding](../winforms-databinding/SKILL.md) |
| Testable screen state, command-enabled buttons/menus, shared ViewModels | [MVVM](../winforms-mvvm/SKILL.md); databinding for collection/currency/writeback |
| Loading freezes, worker progress, cross-thread errors, asynchronous dialogs | [Async](../winforms-async-apis/SKILL.md) |
| Flicker, gauge/chart/cell drawing, text measurement, painting resources/themes | [Rendering](../winforms-rendering/SKILL.md) |
| Camera/render/shell COM wrong-thread, deadlock, native interface cast failure | [COM apartments](../winforms-com-apartments/SKILL.md) |
| Store, MSIX, package identity, signing, x64/ARM64 | [Store packaging](../winforms-store-packaging/SKILL.md) |
| Offline maps, geocoding/routes, embedded online map | [Windows Maps](../winforms-windows-maps/SKILL.md) |
| Explicit .NET 11 renderer, toggle appearance, kiosk, visual accessibility state, painting suspension | [.NET 11 APIs](../winforms-net11-apis/SKILL.md); domain owner still governs layout/lifetime |

Examples: a “customer preferences dialog with larger text and save validation” needs options + layout + Designer + binding, not every skill. A “worker loading customers into a grid” needs async + binding and layout only if changing its viewport.

## Version and capability decisions

| Target/scenario | Gate |
|---|---|
| .NET Framework maintenance | Preserve legacy startup/tooling. Modern dark mode, `InvokeAsync`, built-in commands and the out-of-process Designer SDK are not interchangeable Framework APIs. |
| .NET 8+ | Built-in `DataContext`, button/menu commands; older targets can still implement MVVM explicitly. |
| .NET 9 | `InvokeAsync`; dark-mode APIs and async dialogs have experimental diagnostics appropriate to that target. |
| .NET 10 | Dark mode and async dialogs stable. Prefer typed clipboard/JSON for new clipboard formats; do not re-enable unsafe BinaryFormatter compatibility. Screen capture policy is an explicit privacy requirement, not a general default. |
| .NET 11 prerelease | Use only when target requested; inspect actual reference assembly/signatures. Release status is separate from API attributes. |
| Windows APIs/identity | Check Windows contract, runtime OS, package identity, capability and architecture; compile-time targeting alone is insufficient. |

Current .NET 10 details, including typed clipboard, `Form.ScreenCaptureMode`, and legacy-control obsoletion, are owned by [development](../winforms-development/SKILL.md), not an additional feature skill.

## Final checks

1. Selected owners cover every requested domain without a second competing router.
2. Required target/OS/identity gates and exact layout/threading constraints remain in the working checklist.
3. Verify compilation plus relevant runtime behavior; report unsupported/manual boundaries rather than inventing success.
