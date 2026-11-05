using System.Text;
using System.Text.Json;
using DotnetUp.WinFormsApp;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.DirectX.Controls.Terminal;

namespace DotnetUp.SmokeTests;

internal static partial class Program
{
    private static async Task CheckGitBranchesAsync(HostedTerminal terminal, D2DConsole console, ShellKind shell)
    {
        string folder = Path.Combine(Path.GetTempPath(), "DotnetUp.SmokeTests", Guid.NewGuid().ToString("N"), "repository with spaces");
        Directory.CreateDirectory(folder);
        string git = HostedTerminal.FindExecutable("git.exe") ?? throw new FileNotFoundException("Git is required for branch regressions.");
        async Task<CommandResult> Run(ShellCommand command) =>
            await terminal.RunAsync(command, captureJson: true).WaitAsync(TimeSpan.FromSeconds(30));
        async Task<CommandResult> Git(params string[] args)
        {
            CommandResult result = await Run(new ShellCommand(git, ["--no-pager", "-C", folder,
                "-c", "core.hooksPath=" + Path.Combine(folder, ".empty-hooks"), .. args]));
            Require(result.Succeeded, $"{shell} Git fixture command succeeds: {string.Join(" ", args)}: {result.LastError}");
            return result;
        }

        try
        {
            await Git("init", "--initial-branch=main");
            await Git("config", "core.autocrlf", "false");
            GitBranchSnapshot empty = await GitBranches.ReadAsync(git, folder, Run);
            Require(empty.Branches.Count == 0, $"{shell} empty repositories have an empty branch list");
            string tracked = Path.Combine(folder, "tracked.txt");
            File.WriteAllText(tracked, "main version\n");
            await Git("add", "--", "tracked.txt");
            await Git("-c", "user.name=Smoke Test", "-c", "user.email=smoke@example.invalid",
                "-c", "commit.gpgsign=false", "commit", "-m", "Initial commit");
            await Git("switch", "-c", "feature");
            File.WriteAllText(tracked, "feature version\n");
            await Git("add", "--", "tracked.txt");
            await Git("-c", "user.name=Smoke Test", "-c", "user.email=smoke@example.invalid",
                "-c", "commit.gpgsign=false", "commit", "-m", "Feature commit");
            string sha = (await Git("rev-parse", "HEAD")).Output.Trim();
            await Git("switch", "main");
            await Git("config", "remote.origin.url", folder);
            await Git("config", "remote.origin.fetch", "+refs/heads/*:refs/remotes/origin/*");
            await Git("update-ref", "refs/remotes/origin/remote-feature", sha);
            await Git("update-ref", "refs/remotes/origin/feature", sha);
            await Git("symbolic-ref", "refs/remotes/origin/HEAD", "refs/remotes/origin/feature");

            GitBranchSnapshot snapshot = await GitBranches.ReadAsync(git, folder, Run);
            Require(snapshot.Branches.Count == 4 && snapshot.Branches.Single(branch => branch.IsCurrent).Name == "main"
                && snapshot.Branches.All(branch => DateTimeOffset.TryParse(branch.CommitDate, out _) && branch.Subject.Length > 0),
                $"{shell} branches expose current/local/remote, commit dates and subjects, excluding remote HEAD aliases");
            GitBranch feature = snapshot.Branches.Single(branch => !branch.IsRemote && branch.Name == "feature");
            GitBranch duplicate = snapshot.Branches.Single(branch => branch.Name == "origin/feature");
            try
            {
                GitBranches.SwitchCommand(git, duplicate, snapshot.Branches);
                throw new InvalidOperationException("Remote selection must not replace an existing local branch");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
            }

            File.WriteAllText(tracked, "my uncommitted changes\n");
            snapshot = await GitBranches.ReadAsync(git, folder, Run);
            Require(snapshot.DirtyFiles == 1, $"{shell} dirty working tree is reported");
            CommandResult refused = await Run(GitBranches.SwitchCommand(git, feature, snapshot.Branches));
            Require(!refused.Succeeded && File.ReadAllText(tracked) == "my uncommitted changes\n"
                && (await Git("branch", "--show-current")).Output.Trim() == "main",
                $"{shell} conflicting branch switches preserve uncommitted files and current branch");
            File.WriteAllText(tracked, "main version\n");
            CommandResult switched = await Run(GitBranches.SwitchCommand(git, feature, snapshot.Branches));
            Require(switched.Succeeded && File.ReadAllText(tracked) == "feature version\n", $"{shell} local branch switches normally");
            GitBranch remote = snapshot.Branches.Single(branch => branch.Name == "origin/remote-feature");
            switched = await Run(GitBranches.SwitchCommand(git, remote, snapshot.Branches));
            Require(switched.Succeeded && (await Git("branch", "--show-current")).Output.Trim() == "remote-feature"
                && (await Git("rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}")).Output.Trim() == "origin/remote-feature",
                $"{shell} remote selection creates a new local tracking branch without resetting existing branches");

            string probeResult = Path.Combine(folder, "interactive.json");
            string batch = Path.Combine(folder, "fake-copilot.cmd");
            File.WriteAllText(batch, $"@echo off\r\n\"{Environment.ProcessPath}\" --probe interactive \"{probeResult}\"\r\nexit /b %errorlevel%\r\n",
                new UTF8Encoding(false));
            Task<CommandResult> interactive = terminal.RunInteractiveAsync(GitBranches.CopilotCommand(batch, folder));
            await WaitUntilAsync(() => ConsoleOutput(console).Contains("interactive-ready"), $"{shell} interactive client starts visibly");
            Require(console.FocusTarget == ConsoleFocusTarget.Output && console.InputMode == ConsoleInputMode.SendToSession,
                $"{shell} interactive launch routes raw input to the child terminal");
            await console.SendInputAsync("mouse-launched-session\r");
            CommandResult completed = await interactive.WaitAsync(TimeSpan.FromSeconds(30));
            using (JsonDocument result = JsonDocument.Parse(File.ReadAllText(probeResult)))
            {
                Require(completed.Succeeded && result.RootElement.GetProperty("Directory").GetString() == folder
                    && result.RootElement.GetProperty("Input").GetString() == "mouse-launched-session",
                    $"{shell} interactive client starts in the selected repository and receives input");
            }
            Require(console.InputMode == ConsoleInputMode.RaisePromptSubmitted && console.FocusTarget == ConsoleFocusTarget.Prompt,
                $"{shell} exiting an interactive client restores the editable prompt");
            File.WriteAllText(batch, "@echo off\r\nexit /b 9\r\n");
            Require((await terminal.RunAsync(new ShellCommand(batch, []))).ExitCode == 9,
                $"{shell} batch launchers return their real exit code to the tracked wrapper");
            Console.WriteLine($"PASS: {shell} real Git branches, dirty-switch protection, remote tracking and interactive Copilot-style launch");
        }
        finally
        {
            if (terminal.IsBusy)
            {
                await terminal.InterruptAsync();
                await terminal.RestartAsync(shell);
            }
            await terminal.RunTextAsync(shell == ShellKind.PowerShell
                ? $"Set-Location -LiteralPath {ShellCommand.Quote(Path.GetTempPath(), shell)}"
                : $"cd /d {ShellCommand.Quote(Path.GetTempPath(), shell)}");
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(Path.GetDirectoryName(folder)!, recursive: true);
        }
    }
}
