using System.Diagnostics;

namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private enum WorkspaceAction { InstallLocal, Website, Folder, Edit, VisualStudio, VsCode, Terminal, GlobalJson, Branches, SwitchBranch, Copilot }
    private WorkspaceEntry? SelectedWorkspace => (navigation.SelectedNode?.Tag as NavigationItem)?.Value as WorkspaceEntry;
    private string? SelectedWorkspaceDirectory => SelectedWorkspace?.DirectoryPath ?? SelectedRepository?.Path;
    private IReadOnlyList<VisualStudioInstance>? _visualStudio;

    private IEnumerable<ToolStripItem> WorkspaceActionItems =>
        Descendants(menu.Items).Concat(Descendants(repositoryContext.Items)).Concat(Descendants(toolbar.Items))
            .Concat(details.ContextMenuStrip is { } context ? Descendants(context.Items) : [])
            .Where(item => item.Tag is WorkspaceAction);

    private static IEnumerable<ToolStripItem> Descendants(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            yield return item;
            if (item is ToolStripDropDownItem dropdown)
            {
                foreach (ToolStripItem child in Descendants(dropdown.DropDownItems))
                {
                    yield return child;
                }
            }
        }
    }

    private void UpdateWorkspaceCommandState(bool ready)
    {
        WorkspaceEntry? workspace = SelectedWorkspace;
        foreach (ToolStripItem item in WorkspaceActionItems)
        {
            item.Enabled = ready && (WorkspaceAction)item.Tag! switch
            {
                WorkspaceAction.Website => SelectedRepository is not null,
                WorkspaceAction.Branches => SelectedRepository is not null,
                WorkspaceAction.SwitchBranch => SelectedBranch is { IsCurrent: false },
                WorkspaceAction.Copilot => SelectedBranch is not null,
                WorkspaceAction.Folder or WorkspaceAction.Terminal => SelectedWorkspaceDirectory is not null,
                WorkspaceAction.InstallLocal => workspace is not null || SelectedRepository is not null,
                WorkspaceAction.Edit or WorkspaceAction.VisualStudio => workspace is { Kind: WorkspaceKind.Solution or WorkspaceKind.Project },
                WorkspaceAction.VsCode => workspace is { Kind: WorkspaceKind.Project },
                WorkspaceAction.GlobalJson => SelectedWorkspaceDirectory is { } path && RepositoryScanner.FindGlobalJson(path) is not null,
                _ => false
            };
        }
    }

    private async void WorkspaceAction_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripItem { Tag: WorkspaceAction action })
        {
            return;
        }

        await RunWorkflowAsync(async () =>
        {
            switch (action)
            {
                case WorkspaceAction.Branches:
                    await ShowBranchesAsync(SelectedRepository ?? throw new InvalidOperationException("Select a GitHub repository."),
                        ++_detailsRequest);
                    break;
                case WorkspaceAction.SwitchBranch:
                    await SwitchSelectedBranchAsync(startCopilot: false);
                    break;
                case WorkspaceAction.Copilot:
                    await SwitchSelectedBranchAsync(startCopilot: true);
                    break;
                case WorkspaceAction.InstallLocal:
                    RepositoryInfo context = SelectedWorkspace?.SdkContext() ?? SelectedRepository
                        ?? throw new InvalidOperationException("Select a solution or project first.");
                    using (SdkTaskDialog dialog = new(RequireDotnetUp(), _preferences.Shell, _preferences.Channel,
                        false, context, null, dedicated: true)
                    { Font = Font })
                    {
                        if (dialog.ShowDialog(this) == DialogResult.OK)
                        {
                            WorkspaceSdkConfiguration? configuration = dialog.GetLocalConfiguration();
                            await ExecutePlanAsync(DotnetUpCommands.Plan(RequireDotnetUp(), dialog.GetOptions()));
                            configuration?.Save();
                            await ShowNodeDetailsAsync(navigation.SelectedNode);
                            SetStatus(configuration is null ? "Dedicated SDK installed. Use its dotnet.exe or configure sdk.paths in global.json."
                                : "Dedicated SDK installed and local global.json configured.");
                        }
                    }
                    break;
                case WorkspaceAction.Website:
                    Process.Start(new ProcessStartInfo(WorkspaceTools.RepositoryWebsite(SelectedRepository
                        ?? throw new InvalidOperationException("Select a GitHub repository.")).AbsoluteUri)
                    { UseShellExecute = true });
                    break;
                case WorkspaceAction.Folder:
                    OpenSelectedPath(false);
                    break;
                case WorkspaceAction.Edit:
                    WorkspaceEntry workspace = SelectedWorkspace ?? throw new InvalidOperationException("Select a solution or project.");
                    using (WorkspaceEditor editor = new(workspace.Path) { Font = Font })
                    {
                        editor.ShowDialog(this);
                    }
                    break;
                case WorkspaceAction.VsCode:
                    string code = WorkspaceTools.FindVsCode() ?? throw new FileNotFoundException("Visual Studio Code is not installed or on PATH.");
                    await LaunchWorkspaceAsync(code);
                    break;
            }
        });
    }

    private async Task LaunchWorkspaceAsync(string executable)
    {
        WorkspaceEntry workspace = SelectedWorkspace ?? throw new InvalidOperationException("Select a solution or project.");
        if (executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            await ExecutePlanAsync([new ShellCommand(executable, [workspace.Path], workspace.DirectoryPath)], refresh: false);
        }
        else
        {
            Process.Start(WorkspaceTools.LaunchInfo(executable, workspace.Path, workspace.DirectoryPath));
            SetStatus($"Opened {workspace.Name} in {Path.GetFileNameWithoutExtension(executable)}.");
        }
    }

    private async void VisualStudio_DropDownOpening(object? sender, EventArgs e)
    {
        if (sender is not ToolStripDropDownItem item)
        {
            return;
        }

        try
        {
            if (_visualStudio is null)
            {
                string? vswhere = WorkspaceTools.FindVsWhere();
                if (vswhere is null)
                {
                    _visualStudio = [];
                }
                else
                {
                    ProcessStartInfo start = new(vswhere)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };
                    foreach (string argument in new[] { "-all", "-prerelease", "-products", "*", "-format", "json", "-utf8" })
                    {
                        start.ArgumentList.Add(argument);
                    }

                    using Process process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start vswhere.");
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> error = process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(_inventoryLifetime.Token);
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Visual Studio discovery failed: {await error}");
                    }

                    _visualStudio = WorkspaceTools.ParseVisualStudio(await output);
                }
            }

            if (_closing || item.IsDisposed)
            {
                return;
            }

            while (item.DropDownItems.Count > 0)
            {
                ToolStripItem old = item.DropDownItems[0];
                item.DropDownItems.RemoveAt(0);
                old.Dispose();
            }

            if (_visualStudio.Count == 0)
            {
                item.DropDownItems.Add(new ToolStripMenuItem("No installed Visual Studio IDE found") { Enabled = false });
            }
            else
            {
                foreach (VisualStudioInstance instance in _visualStudio)
                {
                    ToolStripMenuItem version = new(instance.Name) { Tag = instance };
                    version.Click += VisualStudioVersion_Click;
                    item.DropDownItems.Add(version);
                }
            }
        }
        catch (OperationCanceledException) when (_inventoryLifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void VisualStudioVersion_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: VisualStudioInstance instance })
        {
            await RunWorkflowAsync(() => LaunchWorkspaceAsync(instance.Executable));
        }
    }
}
