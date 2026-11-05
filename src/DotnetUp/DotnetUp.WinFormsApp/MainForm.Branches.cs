namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private int _detailsRequest;
    private GitBranch? SelectedBranch => details.SelectedRows.Count == 1
        && details.SelectedRows[0].DataBoundItem is GitBranch branch
        && string.Equals(SelectedRepository?.Path, branch.RepositoryPath, StringComparison.OrdinalIgnoreCase) ? branch : null;

    private static string RequireGit() => HostedTerminal.FindExecutable("git.exe")
        ?? throw new FileNotFoundException("Git is not installed or git.exe is not on PATH.");

    private async Task ShowBranchesAsync(RepositoryInfo repo, int request, string? selectRef = null)
    {
        HostedTerminal terminal = _terminal ?? throw new InvalidOperationException("The terminal is unavailable.");
        string? selectedRef = selectRef ?? SelectedBranch?.FullRef;
        SetStatus($"Reading local and remote branches in {repo.Name}...");
        GitBranchSnapshot snapshot = await GitBranches.ReadAsync(RequireGit(), repo.Path, command => terminal.RunAsync(command, captureJson: true));
        if (request != _detailsRequest || !string.Equals(SelectedRepository?.Path, repo.Path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        details.DataSource = snapshot.Branches.ToList();
        details.ClearSelection();
        if (selectedRef is not null)
        {
            foreach (DataGridViewRow row in details.Rows)
            {
                row.Selected = row.DataBoundItem is GitBranch branch && branch.FullRef == selectedRef;
            }
        }

        UpdateSelectionStatus();
        SetStatus($"{snapshot.Branches.Count} local/remote branches, newest commits first; {snapshot.DirtyFiles} uncommitted changes. "
            + "Select one branch to switch or start Copilot.");
    }

    private async Task SwitchSelectedBranchAsync(bool startCopilot)
    {
        GitBranch branch = SelectedBranch ?? throw new InvalidOperationException("Select exactly one branch in the branch list.");
        RepositoryInfo repo = SelectedRepository ?? throw new InvalidOperationException("Select a GitHub repository.");
        TreeNode? originalNode = navigation.SelectedNode;
        int originalRequest = _detailsRequest;
        string git = RequireGit();
        string? copilot = startCopilot ? HostedTerminal.FindExecutable("copilot.exe")
            ?? HostedTerminal.FindExecutable("copilot.cmd") ?? HostedTerminal.FindExecutable("copilot") : null;
        if (startCopilot && copilot is null)
        {
            throw new FileNotFoundException("GitHub Copilot CLI is not installed or is not on PATH. Install it, then restart the app.");
        }

        HostedTerminal terminal = _terminal ?? throw new InvalidOperationException("The terminal is unavailable.");
        GitBranchSnapshot current = await GitBranches.ReadAsync(git, repo.Path, command => terminal.RunAsync(command, captureJson: true));
        GitBranch? target = current.Branches.SingleOrDefault(candidate => candidate.FullRef == branch.FullRef);
        if (target is null)
        {
            throw new InvalidOperationException("The selected branch no longer exists. Reload the branch list.");
        }

        if (!target.IsCurrent)
        {
            ShellCommand command = GitBranches.SwitchCommand(git, target, current.Branches);
            if (current.DirtyFiles > 0 && MessageBox.Show(this,
                $"There are {current.DirtyFiles} uncommitted changes. Git may carry compatible changes to '{target.Name}' "
                + "or refuse the switch if they conflict.\r\nNothing will be stashed, discarded or force-reset.\r\nContinue?",
                "Switch branch with local changes", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                return;
            }

            CommandResult result = await terminal.RunAsync(command);
            if (!result.Succeeded)
            {
                string error = result.LastError ?? "Git refused the branch switch.";
                if (navigation.SelectedNode == originalNode && originalRequest == _detailsRequest)
                {
                    await ShowBranchesAsync(repo, ++_detailsRequest);
                }
                throw new InvalidOperationException(error);
            }
        }

        if (navigation.SelectedNode == originalNode && originalRequest == _detailsRequest)
        {
            string localRef = target.IsRemote ? "refs/heads/" + target.Name[(target.Name.IndexOf('/') + 1)..] : target.FullRef;
            await ShowBranchesAsync(repo, ++_detailsRequest, localRef);
        }
        if (startCopilot)
        {
            SetStatus($"Copilot is running in {repo.Path} on {target.Name}. Use the terminal output surface; Stop interrupts it.");
            CommandResult result = await terminal.RunInteractiveAsync(GitBranches.CopilotCommand(copilot!, repo.Path));
            ReportResult(result);
        }
        else
        {
            SetStatus($"Switched to {target.Name} in {repo.Path}.");
        }
    }
}
