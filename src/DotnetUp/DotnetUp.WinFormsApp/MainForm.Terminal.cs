using WarpToolkit.WinForms.DirectX.Controls.Terminal;

namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private HostedTerminal? _terminal;
    private bool _closing;

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        InitializeNavigation();
        _terminal = new HostedTerminal(console);
        console.PromptSubmitted += Console_PromptSubmitted;
        try
        {
            await _terminal.StartAsync(_preferences.Shell);
            SetStatus("Terminal ready. Commands run in your user profile unless a repository is selected.");
            await RefreshInventoryAsync();
            if (!_preferences.SetupCompleted)
            {
                await RunWorkflowAsync(RunSetupWizardAsync);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Startup failed: {ex.Message}");
        }
    }

    private async void Console_PromptSubmitted(object? sender, ConsolePromptSubmittedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text) || _terminal is null)
        {
            return;
        }

        try
        {
            if (_terminal.IsBusy)
            {
                await console.SendInputAsync(e.Text + "\r");
                return;
            }

            if (_workflowRunning)
            {
                ShowError("Wait for the current workflow before submitting a command.");
                return;
            }

            SetStatus("Running command...");
            CommandResult result = await _terminal.RunTextAsync(e.Text);
            ReportResult(result);
            if (result.Succeeded && e.Text.Contains("dotnetup", StringComparison.OrdinalIgnoreCase))
            {
                await RefreshInventoryAsync();
            }
        }
        catch (OperationCanceledException)
        {
            ShowError("Command interrupted. Use Tools / Restart terminal to continue.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ReportResult(CommandResult result)
    {
        if (result.Succeeded)
        {
            SetStatus("Command completed successfully.");
        }
        else
        {
            ShowError(result.LastError ?? $"Command failed (exit {result.ExitCode}).");
        }
    }

    private async void Stop_Click(object? sender, EventArgs e)
    {
        _scanCancellation?.Cancel();
        try
        {
            if (_terminal is not null)
            {
                await _terminal.InterruptAsync();
                SetStatus("Interrupt sent. Restart the terminal if the active shell was stopped.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void RestartTerminal_Click(object? sender, EventArgs e)
    {
        if (_workflowRunning)
        {
            ShowError("Wait for the current workflow before restarting the terminal.");
            return;
        }

        try
        {
            if (_terminal is not null)
            {
                await _terminal.RestartAsync(_preferences.Shell);
                SetStatus("Terminal restarted.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task CloseTerminalAsync()
    {
        _inventoryLifetime.Cancel();
        if (_terminal is not null)
        {
            await _terminal.DisposeAsync();
            _terminal = null;
        }
    }
}
