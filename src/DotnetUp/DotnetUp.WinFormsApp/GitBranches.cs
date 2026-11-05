using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DotnetUp.WinFormsApp;

internal sealed record GitBranch(string Name, bool IsRemote, bool IsCurrent, string CommitDate,
    string Commit, string Subject, int UncommittedChanges,
    [property: Browsable(false)] string FullRef, [property: Browsable(false)] string RepositoryPath)
{
    public override string ToString() => $"{Name}{(IsCurrent ? " (current)" : "")}: {Subject}";
}

internal sealed record GitBranchSnapshot(IReadOnlyList<GitBranch> Branches, int DirtyFiles);

internal static partial class GitBranches
{
    public static async Task<GitBranchSnapshot> ReadAsync(string git, string directory,
        Func<ShellCommand, Task<CommandResult>> run)
    {
        async Task<CommandResult> Query(params string[] arguments)
        {
            CommandResult result = await run(new ShellCommand(git, ["--no-pager", "-C", directory, .. arguments]));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.LastError ?? $"Git failed with exit code {result.ExitCode}.");
            }
            return result;
        }

        CommandResult refs = await Query("for-each-ref", "--sort=-committerdate", "refs/heads", "refs/remotes");
        CommandResult head = await run(new ShellCommand(git, ["--no-pager", "-C", directory, "symbolic-ref", "--quiet", "HEAD"]));
        if (head.ExitCode is not (0 or 1))
        {
            throw new InvalidOperationException(head.LastError ?? "Unable to determine the current branch.");
        }

        string? current = head.Succeeded ? head.Output.Trim() : null;
        CommandResult log = string.IsNullOrWhiteSpace(refs.Output) ? new CommandResult(0, "", null)
            : await Query("log", "--all", "--no-walk=unsorted", "--format=fuller", "--date=iso-strict",
                "--no-show-signature", "--no-color", "--no-decorate");
        CommandResult status = await Query("status", "--porcelain=v1", "--untracked-files=normal");
        int dirty = status.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        return new GitBranchSnapshot(Parse(directory, refs.Output, current, log.Output, dirty), dirty);
    }

    internal static IReadOnlyList<GitBranch> Parse(string directory, string refs, string? current, string log, int dirty)
    {
        Dictionary<string, (string Date, string Subject)> commits = new(StringComparer.Ordinal);
        foreach (Match match in CommitBlock().Matches(log))
        {
            string block = match.Groups["body"].Value;
            string date = block.Split('\n').FirstOrDefault(line => line.StartsWith("CommitDate:", StringComparison.Ordinal))?["CommitDate:".Length..].Trim() ?? "";
            string subject = block.Split('\n').FirstOrDefault(line => line.StartsWith("    ", StringComparison.Ordinal))?.Trim() ?? "";
            commits[match.Groups["sha"].Value] = (date, subject);
        }

        List<GitBranch> branches = [];
        foreach (string line in refs.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split('\t');
            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Git returned an unrecognized reference record.");
            }

            string sha = parts[0].Split(' ')[0];
            string fullRef = parts[1];
            bool remote = fullRef.StartsWith("refs/remotes/", StringComparison.Ordinal);
            if ((!remote && !fullRef.StartsWith("refs/heads/", StringComparison.Ordinal))
                || (remote && fullRef.EndsWith("/HEAD", StringComparison.Ordinal)))
            {
                continue;
            }

            string name = fullRef[(remote ? "refs/remotes/".Length : "refs/heads/".Length)..];
            if (!commits.TryGetValue(sha, out var commit))
            {
                throw new InvalidOperationException($"Git did not return commit metadata for {name}.");
            }

            bool isCurrent = fullRef == current;
            branches.Add(new(name, remote, isCurrent, commit.Date, sha, commit.Subject, isCurrent ? dirty : 0, fullRef, directory));
        }

        return branches;
    }

    public static ShellCommand SwitchCommand(string git, GitBranch branch, IReadOnlyList<GitBranch> branches)
    {
        if (branch.IsRemote)
        {
            int slash = branch.Name.IndexOf('/');
            if (slash < 1 || slash == branch.Name.Length - 1)
            {
                throw new ArgumentException("Select a remote tracking branch, not its remote HEAD.");
            }

            string local = branch.Name[(slash + 1)..];
            if (branches.Any(candidate => !candidate.IsRemote && candidate.Name == local))
            {
                throw new InvalidOperationException($"Local branch '{local}' already exists. Select that local branch rather than replacing it from the remote.");
            }

            return new ShellCommand(git, ["--no-pager", "-C", branch.RepositoryPath, "switch", "--no-guess",
                "--track", "-c", local, "--", branch.FullRef], branch.RepositoryPath);
        }

        return new ShellCommand(git, ["--no-pager", "-C", branch.RepositoryPath, "switch", "--no-guess", "--", branch.Name], branch.RepositoryPath);
    }

    public static ShellCommand CopilotCommand(string executable, string directory) => new(executable, [], directory);

    [GeneratedRegex(@"^commit (?<sha>[0-9a-f]+)[^\r\n]*\r?\n(?<body>.*?)(?=^commit [0-9a-f]+|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CommitBlock();
}
