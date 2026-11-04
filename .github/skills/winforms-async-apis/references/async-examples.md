# Async patterns

## Contents

- [Explicit overloads](#explicit-overloads)
- [Show results](#show-results)
- [Synchronous kickoff](#synchronous-kickoff)
- [Deferred reactions and coordination](#deferred-reactions-and-coordination)

## Explicit overloads

Complete .NET 10 helper; UI handle/message loop must exist. The callback uses the supplied token and preserves UI context:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>Demonstrates correctly typed UI dispatch callbacks.</summary>
public static class AsyncUiExamples
{
    /// <summary>Runs all four overload shapes.</summary>
    public static async Task<int> UpdateAsync(Control control, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(control);
        Action setText = () => { control.Text = "Loading"; };
        await control.InvokeAsync(setText, token);
        int length = await control.InvokeAsync(() => control.Text.Length, token);
        Func<CancellationToken, ValueTask> finish = async ct =>
        {
            await Task.Delay(1, ct);
            control.Text = "Loaded";
        };
        await control.InvokeAsync(finish, token);
        Func<CancellationToken, ValueTask<int>> read = async ct =>
        {
            await Task.Delay(1, ct);

            return control.Text.Length;
        };

        return length + await control.InvokeAsync(read, token);
    }
}
```

Avoid `InvokeAsync(() => SomeTaskAsync())`: it can select `Func<Task>` and return `Task<Task>`. Wrap Task work in the typed token-taking async delegate instead. `Task.Run` expects an Action/Func delegate, not an already-created Task; `Task.Run(() => control.InvokeAsync(...))` is mechanically possible but normally redundant because InvokeAsync already posts. CPU work belongs in Task.Run; I/O normally does not.

## Show results

```csharp
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>Shows modeless/modal forms and a task dialog with their actual result types.</summary>
public static class AsyncWindows
{
    /// <summary>Awaits completion before disposing each Form.</summary>
    public static async Task ShowExamplesAsync()
    {
        using Form document = new();
        await document.ShowAsync();
        using Form editor = new();
        DialogResult result = await editor.ShowDialogAsync();
        TaskDialogPage page = new()
        {
            Heading = "Finished",
            Text = result == DialogResult.OK ? "Accepted" : "Closed"
        };
        TaskDialogButton clicked = await TaskDialog.ShowDialogAsync(page);
    }
}
```

Prefer supported owner overloads when an owner exists; check exact target signatures. The show methods use usable WinForms context; the caller still owns construction and disposal.

## Synchronous kickoff

This complete Form starts from `Shown` without making the override async-void. The observer retains work, handles cancellation/faults, and invalidates the session only after an accepted close:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/// <summary>Starts observed asynchronous work after the shown event.</summary>
public class LoadingWindow : Form
{
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _work;
    private bool _lifetimeDisposed;

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _work ??= ObserveAsync(_lifetime.Token);
    }

    private async Task ObserveAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(1, token);
            if (!IsDisposed && !token.IsCancellationRequested)
            {
                Text = "Ready";
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                MessageBox.Show(this, ex.Message, "Load failed");
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel)
        {
            _lifetime.Cancel();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_lifetimeDisposed)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            _lifetimeDisposed = true;
        }

        base.Dispose(disposing);
    }
}
```

For a designed Form, merge cleanup into its one ownership path. For a longer operation, retain the token before disposing CTS and observe completion; do not dispose resources while work still uses them. Async event handlers use the same try/cancellation/fault policy rather than escaping errors.

## Deferred reactions and coordination

In a KeyDown event, posting `SelectAll` can defer a reaction past the current handler/message; test actual key behavior and handle errors/lifetime. It is not a guarantee about pending paint/activation order. A synchronous `OnLoad` can post a retained, observed kickoff, but `Shown` is clearer when visibility matters.

Two cooperative UI-bound activities are concurrent, not necessarily parallel. Start both; `WhenAny` returns the completed **Task**, so await that task to observe its result/fault before restarting only that activity. When stopping, cancel the session and observe **both** outstanding tasks (e.g. WhenAll in cleanup) so faults don't disappear. Rejected close must not permanently kill the session.

Do not use `ConfigureAwait(false)` if the next continuation touches a control. Library I/O can avoid context only when its entire continuation is UI-independent, then marshal a small UI update explicitly.
