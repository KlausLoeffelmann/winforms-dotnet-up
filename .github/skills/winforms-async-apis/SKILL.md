---
name: winforms-async-apis
description: >-
  Fixes loading freezes, cross-thread control access, worker progress updates,
  asynchronous forms/dialogs and startup work in WinForms. Selects correct
  InvokeAsync overloads and cancellation/error ownership, including WFO2001
  or nested-task mistakes. Applies when background work must read/update UI
  or an async operation starts from an event or synchronous lifecycle method.
---

# UI marshalling and asynchronous lifetime

## Durable rules

- UI controls and bound state remain UI-thread-owned. Plain asynchronous I/O needs `await`, not unnecessary `Task.Run`/marshalling.
- `InvokeAsync` posts, `Invoke` sends and blocks. Neither makes expensive UI callbacks harmless; keep synchronous UI work short.
- Async callbacks must use the **token-taking ValueTask overload**, not accidental `Func<Task>`/`Func<Task<T>>` selection.
- Observe every task's completion/fault; handle intentional cancellation separately. `async void` is confined to event-shaped boundaries with error handling.
- Tie cancellation and subscriptions to actual lifetime. Do not cancel a session merely because a close attempt was rejected.

## Overload decision

All return `Task`/`Task<T>` to the caller:

| Callback | Result | Correct overload |
|---|---|---|
| Synchronous action | none | `Action`, optional cancellation token |
| Synchronous value | T | `Func<T>`, optional cancellation token |
| Asynchronous action | none | `Func<CancellationToken,ValueTask>`, optional outer token |
| Asynchronous value | T | `Func<CancellationToken,ValueTask<T>>`, optional outer token |

`InvokeAsync` is stable from .NET 9. A Task-returning callback can select synchronous `Func<T>` with T=`Task`, yielding a nested task the marshaler does **not** await. `WFO2001` helps diagnose this; explicit typed delegates eliminate ambiguity.

Cancellation of a short synchronous callback may complete the returned task as canceled **while the callback still runs**. Cancellation is not rollback or guaranteed prevention. Async callbacks must cooperate with the supplied token and recheck lifetime after awaits.

## Form/dialog results and version gates

| API | Return/completion |
|---|---|
| `Form.ShowAsync` | `Task`; completes when closed/disposed, **not** a `DialogResult` |
| `Form.ShowDialogAsync` | `Task<DialogResult>` |
| `TaskDialog.ShowDialogAsync` | `Task<TaskDialogButton>` |

These form/task-dialog APIs are non-experimental in .NET 10; .NET 9 uses `WFO5002`. Actual .NET 10.0.14 references confirm no Experimental/Obsolete attributes for these APIs or the four `InvokeAsync` overloads.

`ShowAsync` handles marshalling when a usable `WindowsFormsSynchronizationContext` exists; do not add needless manual dispatch around it. Form construction, control ownership, owner windows, message loops and disposal still matter. Do not dispose a form before its show task completes or show the same active instance twice. Asynchronous showing is not a guarantee that expensive construction/show initialization cannot pause UI. Do not rely on task-internal weak-reference details as a lifetime policy.

Read [compiled patterns](references/async-examples.md) when choosing callbacks, showing forms, deferring a key reaction, coordinating loops, or starting lifecycle work. Posting from UI defers a callback; it does **not** guarantee every paint/activation message drained or a Form became visible.

`ConfigureAwait(false)` is for UI-independent continuations, never a license to set a label color off-thread. Snapshot UI values before background work; marshal only required UI state updates. COM native work additionally follows [apartment ownership](../winforms-com-apartments/SKILL.md).

## Final checks

1. Compile target-specific signatures and ensure async delegates select token/ValueTask overloads.
2. Exercise completion, faults, cancellation, rejected close and accepted shutdown without unobserved tasks.
3. Keep worker callbacks away from controls/bound collections and UI locks.

Sources: [InvokeAsync](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.invokeasync?view=windowsdesktop-10.0), [ShowAsync](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.showasync?view=windowsdesktop-10.0).
