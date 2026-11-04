using System.Diagnostics;
using WarpToolkit.WinForms.Containers.Wizard;

namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private bool _workflowRunning;

    private RepositoryInfo? SelectedRepository =>
        navigation.SelectedNode?.Tag is NavigationItem { Value: RepositoryInfo repo } ? repo : null;

    private DetailRow? SelectedDetail => details.SelectedRows.Count > 0
        ? details.SelectedRows[0].DataBoundItem as DetailRow : null;

    private async Task RunWorkflowAsync(Func<Task> action)
    {
        if (_workflowRunning || _terminal?.IsBusy == true)
        {
            ShowError("A command or workflow is already running. Wait or stop it first.");
            return;
        }

        _workflowRunning = true;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            if (!_closing)
            {
                ShowError("Operation cancelled. Restart the terminal if needed.");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _workflowRunning = false;
        }
    }

    private string RequireDotnetUp() => SystemInventory.FindDotnetUp()
        ?? throw new InvalidOperationException("dotnetup is not installed. Run Tools / Setup wizard first.");

    private async Task ExecutePlanAsync(IReadOnlyList<ShellCommand> commands, bool refresh = true)
    {
        if (_terminal is null)
        {
            throw new InvalidOperationException("The terminal has not started.");
        }

        foreach (ShellCommand command in commands)
        {
            SetStatus($"Running {Path.GetFileName(command.Executable)} {string.Join(" ", command.Arguments.Take(3))}...");
            CommandResult result = await _terminal.RunAsync(command);
            if (!result.Succeeded)
            {
                // Updates can make partial progress even when the final exit code is nonzero.
                string error = result.LastError ?? $"Command failed with exit code {result.ExitCode}.";
                if (refresh)
                {
                    try
                    {
                        await RefreshInventoryAsync();
                    }
                    catch (Exception ex)
                    {
                        error += $" Inventory refresh failed: {ex.Message}";
                    }
                }

                throw new InvalidOperationException(error);
            }
        }

        if (refresh)
        {
            await RefreshInventoryAsync();
        }

        SetStatus("Command completed successfully.");
    }

    private async Task ShowSdkDialogAsync(bool update)
    {
        string executable = RequireDotnetUp();
        DetailRow? selected = SelectedDetail;
        string? installPath = selected?.Kind is "Managed installation" or "Tracked requirement" ? selected.Location : null;
        using SdkTaskDialog dialog = new(executable, _preferences.Shell, _preferences.Channel,
            update, SelectedRepository, installPath)
        { Font = Font };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await ExecutePlanAsync(DotnetUpCommands.Plan(executable, dialog.GetOptions()));
        }
    }

    private async void Install_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(() => ShowSdkDialogAsync(false));

    private async void Update_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(() => ShowSdkDialogAsync(true));

    private async void Setup_Click(object? sender, EventArgs e) => await RunWorkflowAsync(RunSetupWizardAsync);

    private async Task RunSetupWizardAsync()
    {
        using SetupWizard wizard = new(_preferences.Channel, Font, SystemInventory.FindDotnetUp() is not null);
        if (await wizard.ShowAsync(this) != WizardDialogResult.Finished)
        {
            SetStatus("Setup postponed. Use Tools / Setup wizard when ready.");
            return;
        }

        SetupOptions options = wizard.GetOptions();
        if (options.Mode == "everywhere"
            && MessageBox.Show(this, "Everywhere mode changes system PATH for all users and overrides machine .NET resolution.\r\n"
                + "Elevation may be required; missing managed SDKs can break builds.\r\nContinue?",
                "System-wide environment change", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        if (options.Mode == "shell" && HostedTerminal.FindExecutable("pwsh.exe") is null)
        {
            throw new InvalidOperationException("PowerShell 7 (pwsh.exe) is required for shell profile setup. Install it or select none.");
        }

        HostedTerminal terminal = _terminal ?? throw new InvalidOperationException("The terminal is unavailable.");
        ShellKind originalShell = _preferences.Shell;
        bool installedTool = false;
        try
        {
            if (SystemInventory.FindDotnetUp() is null)
            {
                if (terminal.Shell != ShellKind.PowerShell)
                {
                    await terminal.RestartAsync(ShellKind.PowerShell);
                }

                SetStatus("Installing dotnetup from Microsoft's installer...");
                CommandResult bootstrap = await terminal.RunTextAsync("$ErrorActionPreference = 'Stop'; " + DotnetUpCommands.Bootstrap);
                if (!bootstrap.Succeeded)
                {
                    throw new InvalidOperationException(bootstrap.LastError ?? "dotnetup download failed.");
                }

                installedTool = true;
            }

            string executable = RequireDotnetUp();
            await ExecutePlanAsync(SetupWizard.SetupCommands(executable, options));
            _preferences.SetupCompleted = true;
            _userSettingsService?.Set("Preferences", System.Text.Json.JsonSerializer.Serialize(_preferences));
            _userSettingsService?.Flush();
            SetStatus("Setup complete. Use dotnetup dotnet -- <arguments> in none mode. New terminals inherit saved PATH changes.");
        }
        finally
        {
            if (!_closing && (installedTool || terminal.Shell != originalShell) && !terminal.IsBusy)
            {
                await terminal.RestartAsync(originalShell);
            }
        }
    }

    private async void Verify_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(() => RefreshInventoryAsync(verify: true));

    private async void Environment_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(() => ExecutePlanAsync([new ShellCommand(RequireDotnetUp(), ["env", "show"])], refresh: false));

    private async void Info_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(() => ExecutePlanAsync([new ShellCommand(RequireDotnetUp(), ["--info"])], refresh: false));

    private async void SelfUpdate_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(async () =>
        {
            if (MessageBox.Show(this, "Update the dotnetup tool itself from its current release channel?\r\n"
                + "This is separate from SDK updates.", "Update dotnetup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await ExecutePlanAsync([new ShellCommand(RequireDotnetUp(), ["self", "update", "--no-progress"])]);
            }
        });

    private async void Uninstall_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(async () =>
        {
            DetailRow? row = SelectedDetail;
            if (row?.Kind != "Tracked requirement" || row.Channel is null)
            {
                throw new InvalidOperationException("Select a tracked requirement row, not an installed version or external SDK.");
            }

            if (MessageBox.Show(this, $"Remove tracked requirement {row.Name} {row.Channel} from {row.Location}?\r\n\r\n"
                + "dotnetup garbage collection can also remove unreferenced SDKs and runtimes in this root.\r\n"
                + "Other requirements can retain the requested version. global.json files are not deleted.\r\nContinue?",
                "Uninstall requirement", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            string target = row.Component switch
            {
                "SDK" => row.Channel,
                "Runtime" => $"runtime@{row.Channel}",
                "ASPNETCore" => $"aspnetcore@{row.Channel}",
                "WindowsDesktop" => $"windowsdesktop@{row.Channel}",
                _ => throw new InvalidOperationException("This component is not supported for uninstall.")
            };
            await ExecutePlanAsync([new ShellCommand(RequireDotnetUp(),
                [row.Component == "SDK" ? "sdk" : "runtime", "uninstall", target,
                    "--install-path", row.Location, "--source", row.Source == "GlobalJson" ? "globaljson" : "explicit",
                    "--non-interactive"])]);
        });

    private async void RepositoryTerminal_Click(object? sender, EventArgs e) =>
        await RunWorkflowAsync(async () =>
        {
            RepositoryInfo repo = SelectedRepository ?? throw new InvalidOperationException("Select a repository first.");
            if (_terminal is null)
            {
                throw new InvalidOperationException("The terminal is unavailable.");
            }

            string command = _terminal.Shell == ShellKind.PowerShell
                ? $"Set-Location -LiteralPath {ShellCommand.Quote(repo.Path, _terminal.Shell)}"
                : $"cd /d {ShellCommand.Quote(repo.Path, _terminal.Shell)}";
            CommandResult result = await _terminal.RunTextAsync(command);
            ReportResult(result);
            console.FocusPrompt();
        });

    private void OpenFolder_Click(object? sender, EventArgs e) => OpenSelectedPath(false);
    private void OpenGlobalJson_Click(object? sender, EventArgs e) => OpenSelectedPath(true);

    private void OpenSelectedPath(bool globalJson)
    {
        try
        {
            string? path = globalJson ? SelectedRepository?.GlobalJson : SelectedRepository?.Path ?? SelectedDetail?.Location;
            if (string.IsNullOrEmpty(path) || (!Directory.Exists(path) && !File.Exists(path)))
            {
                throw new InvalidOperationException("Select an existing folder or a repository with global.json.");
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void Rescan_Click(object? sender, EventArgs e)
    {
        if (_scanCancellation is not null)
        {
            ShowError("Stop the active scan before resetting discovery.");
            return;
        }

        foreach (TreeNode node in navigation.Nodes[0].Nodes)
        {
            if (node.Tag is NavigationItem { Kind: "drive" })
            {
                node.Collapse();
                node.Nodes.Clear();
                node.Nodes.Add("Expand to discover GitHub repositories");
            }
        }

        SetStatus("Repository discovery reset. Expand a drive to scan again.");
    }
}
