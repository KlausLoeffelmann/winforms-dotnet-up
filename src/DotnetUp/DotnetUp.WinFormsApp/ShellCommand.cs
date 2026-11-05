namespace DotnetUp.WinFormsApp;

public sealed record ShellCommand(string Executable, IReadOnlyList<string> Arguments, string? WorkingDirectory = null)
{
    public string Render(ShellKind shell)
    {
        bool batch = Executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || Executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
        string command = (shell == ShellKind.PowerShell ? "& " : batch ? "call " : "")
            + string.Join(" ", new[] { Executable }.Concat(Arguments).Select(value =>
                shell == ShellKind.CommandPrompt
                    ? QuoteArgument(value)
                    : Quote(value, shell)));
        if (WorkingDirectory is not null)
        {
            command = shell == ShellKind.PowerShell
                ? $"Set-Location -LiteralPath {Quote(WorkingDirectory, shell)} -ErrorAction Stop; {command}"
                : $"cd /d {Quote(WorkingDirectory, shell)} && {command}";
        }

        return command;
    }

    public static string Quote(string value, ShellKind shell)
    {
        if (value.IndexOfAny(['\r', '\n', '\0']) >= 0)
        {
            throw new ArgumentException("Command arguments cannot contain line breaks or null characters.");
        }

        if (shell == ShellKind.PowerShell)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        // cmd expands these even inside quotes. Reject rather than change the requested path.
        if (value.IndexOfAny(['"', '%', '!']) >= 0)
        {
            throw new ArgumentException("This argument requires PowerShell (contains a quote, % or !).");
        }

        return "\"" + value + "\"";
    }

    private static string QuoteArgument(string value)
    {
        string quoted = Quote(value, ShellKind.CommandPrompt);
        if (value.Length > 0 && value.All(c => char.IsAsciiLetterOrDigit(c) || "_-./:=".Contains(c)))
        {
            return value;
        }

        int trailingSlashes = value.Length - value.TrimEnd('\\').Length;
        return quoted.Insert(quoted.Length - 1, new string('\\', trailingSlashes));
    }
}

public sealed record CommandResult(int ExitCode, string Output, string? LastError)
{
    public bool Succeeded => ExitCode == 0;
}
