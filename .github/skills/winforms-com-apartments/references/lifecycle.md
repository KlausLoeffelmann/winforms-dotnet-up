# Apartment-aware dispatch and teardown

## UI readiness and ownership

Create UI-owned native objects only after the intended UI handle/context exists. An `OnHandleCreated` callback can capture `SynchronizationContext.Current` and thread ID; verify it is the expected WinForms context, and invalidate generation when the handle is recreated/destroyed. A thread ID is an ownership check, not COM marshalling.

When using .NET 9+, awaited Control.InvokeAsync is a useful dispatch boundary. This compiled helper dispatches an **already UI-owned** operation, checks stale work on the receiving thread and exposes failures to its caller:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>Posts generation-checked work to an existing UI-owned control.</summary>
public static class ApartmentDispatch
{
    /// <summary>Executes current work and lets the caller observe any native failure.</summary>
    public static Task RunAsync(
        Control owner,
        int generation,
        Func<int> currentGeneration,
        Action nativeOperation,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(currentGeneration);
        ArgumentNullException.ThrowIfNull(nativeOperation);
        Action callback = () =>
        {
            if (!owner.IsDisposed && generation == currentGeneration())
            {
                nativeOperation();
            }
        };

        return owner.InvokeAsync(callback, token);
    }
}
```

Caller prerequisite: handle/context is ready; `currentGeneration` is evaluated on UI and lifecycle changes increment it. Finish render-locked work and release the lock **before awaiting this helper**. The caller handles faults and intentional cancellation. The helper does not initialize COM or marshal an arbitrary worker-owned interface.

Older targets can use a captured SynchronizationContext.Post plus a TaskCompletionSource/exception path and equivalent checks; fire-and-forget Post alone loses completion/error ownership.

## Teardown

1. Stop accepting new native work; invalidate generation.
2. Cancel producers and observe their completion; avoid UI/resource-lock inversion.
3. Detach HWND/composition targets and release UI-owned graph on UI.
4. Tear down worker-owned drawing contexts/resources on their documented owner, preserving references/dependency order.
5. Clear references only after no callback can still use them; device recreation and handle recreation need new generations.

Expected device-loss HRESULTs may rebuild a graph. Wrong apartment, invalid interface casts and ABI mismatches demand ownership/interop correction, not retrying unchanged work. Preserve first failure; do not hide it behind broad catches or convert broken rendering to a silent black surface.

Manual tests: camera switching, resize, minimization, clear/error states, device loss/recreation and shutdown during queued work. Check native documentation separately for each D2D/D3D/DirectComposition/shell family rather than treating all graphics COM interfaces identically.
