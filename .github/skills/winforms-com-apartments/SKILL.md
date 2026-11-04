---
name: winforms-com-apartments
description: >-
  Diagnoses native camera, render, media, shell or DirectX/DirectComposition
  failures in WinForms using CsWin32/COM: E_NOINTERFACE, InvalidCastException,
  wrong-thread access, teardown crashes and render-lock/UI deadlocks. Applies
  when a native object tree crosses UI STA and worker/capture/render apartments
  or queued work survives device, handle or ownership changes.
---

# COM apartment and native-resource ownership

## Durable rules

- CsWin32 defines ABI/interop; an RCW and a C# lock do **not** make COM objects free-threaded or perform apartment marshalling.
- Assign each object tree an owning apartment and keep creation, interface queries, methods, attachment/commit, teardown and releases there.
- Follow the native interface's documented threading model. WinForms UI is normally STA; worker/thread-pool callbacks normally MTA.
- Never synchronously marshal to UI while holding a render/resource lock needed by UI disposal.
- Recheck disposal and resource generation inside posted work; observe its failure through the normal application error path.

## Diagnose before retrying

An interface working in its creating apartment can fail elsewhere with `InvalidCastException`, `System.__ComObject`, or `E_NOINTERFACE (0x80004002)`. This is not proof the object never supported that interface. Check ownership/marshalling **and** generated ABI/platform support.

| Resource | Usual starting choice, subject to native contract |
|---|---|
| Controls/HWND visual tree; DirectComposition target/visuals | UI STA |
| Documented free-threaded D2D/D3D drawing resources | Serialized render/MTA path |
| Immutable frame snapshots | Producer until explicitly transferred |

Do not make a typed call on a worker-owned object from UI merely to prepare release: even `SetTarget(null)` can fail across apartments. Schedule disposal back to its owner; release dependents before dependencies when possible. If callbacks cannot be marshaled during teardown, verify documented dependency-safe destruction ordering rather than guessing an RCW release sequence. Avoid indiscriminate `Marshal.ReleaseComObject` on shared wrappers.

## Smallest native graph

For one flip-model swap chain filling one HWND, start with `CreateSwapChainForHwnd`; DWM already composes it. Use `CreateSwapChainForComposition` plus DirectComposition only for actual visual-tree features: multiple visuals, transforms, opacity, clipping, animation or non-HWND content.

If apartment correction does not resolve `E_NOINTERFACE`, verify generated ABI and remove unnecessary interface layers instead of adding dispatch everywhere.

Read [lifecycle pattern](references/lifecycle.md) for UI-context readiness, awaited dispatch, generation checks, disposal and native-error handling. Use [async](../winforms-async-apis/SKILL.md) only for managed UI dispatch mechanics; it does not define native threading contracts.

## Final checks

1. Creation/use/release threads match each native contract, including shutdown paths.
2. Snapshot control state on UI; release locks before awaited dispatch; ignore stale queued work.
3. Exercise device/camera switching, device loss, clear/error, resize, handle recreation and shutdown. Device-loss rebuild is not a retry policy for apartment/ABI errors.
