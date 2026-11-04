using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.DirectX.Controls.Terminal;

namespace DotnetUp.WinFormsApp;

internal sealed partial class HostedTerminal(D2DConsole console) : IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private ConPtyConsoleSession? _session;
    private CancellationTokenSource? _activeCommand;
    private bool _exited;

    public ShellKind Shell { get; private set; }
    public bool IsBusy => _activeCommand is not null;

    public async Task StartAsync(ShellKind shell)
    {
        Shell = shell;
        string executable = shell == ShellKind.CommandPrompt
            ? Environment.GetEnvironmentVariable("ComSpec") ?? Path.Combine(Environment.SystemDirectory, "cmd.exe")
            : FindExecutable("pwsh.exe") ?? Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        ProcessStartInfo start = new(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        if (shell == ShellKind.PowerShell)
        {
            start.ArgumentList.Add("-NoLogo");
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NoExit");
        }
        else
        {
            start.ArgumentList.Add("/D");
            start.ArgumentList.Add("/Q");
            start.ArgumentList.Add("/V:OFF");
        }

        _exited = false;
        _session = new ConPtyConsoleSession(start);
        _session.Exited += Session_Exited;
        await console.AttachSession(_session, sendInputToSession: false, _lifetime.Token);
        console.FocusPrompt();
    }

    private void Session_Exited(object? sender, ConsoleSessionExitedEventArgs e) => _exited = true;

    public Task<CommandResult> RunAsync(ShellCommand command, bool captureJson = false) =>
        RunCoreAsync(command.Render(Shell), captureJson, nativeCommand: true);

    public Task<CommandResult> RunTextAsync(string command) =>
        RunCoreAsync(command, captureJson: false, nativeCommand: false);

    private async Task<CommandResult> RunCoreAsync(string command, bool captureJson, bool nativeCommand)
    {
        if (!await _commandGate.WaitAsync(0))
        {
            throw new InvalidOperationException("A command is already running. Wait or stop it before starting another.");
        }

        string folder = Path.Combine(Path.GetTempPath(), "DotnetUp.WinForms", Guid.NewGuid().ToString("N"));
        string resultPath = Path.Combine(folder, "exit.txt");
        string outputPath = Path.Combine(folder, "output.json");
        string scriptPath = Path.Combine(folder, Shell == ShellKind.PowerShell ? "command.ps1" : "command.cmd");
        int outputStart = console.OutputText.Length;
        try
        {
            if (_session is null || _exited)
            {
                throw new InvalidOperationException("The terminal has exited. Use Tools / Restart terminal.");
            }

            Directory.CreateDirectory(folder);
            _activeCommand = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            CancellationToken cancellation = _activeCommand.Token;
            string script = BuildScript(command, Shell, resultPath, captureJson ? outputPath : null, nativeCommand);
            await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(Shell == ShellKind.PowerShell), cancellation);
            string invocation = Shell == ShellKind.PowerShell
                ? $". {ShellCommand.Quote(scriptPath, Shell)}\r"
                : $"call {ShellCommand.Quote(scriptPath, Shell)}\r";
            await console.SendInputAsync(invocation, cancellation);
            int exitCode;
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                if (_exited)
                {
                    throw new InvalidOperationException("The hosted shell exited before reporting a command result.");
                }

                if (File.Exists(resultPath)
                    && int.TryParse(await File.ReadAllTextAsync(resultPath, cancellation), out exitCode))
                {
                    break;
                }

                await Task.Delay(100, cancellation);
            }

            await console.FlushAsync(cancellation);
            string screen = console.OutputText;
            string output = captureJson && File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellation)
                : screen[Math.Min(outputStart, screen.Length)..];
            string? lastError = exitCode == 0 ? null
                : EscapeCodes().Replace(screen[Math.Min(outputStart, screen.Length)..], "")
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault(line => !line.Contains("exit.txt", StringComparison.OrdinalIgnoreCase)
                        && !line.StartsWith("PS ", StringComparison.Ordinal) && !line.EndsWith('>'))?.Trim();
            return new CommandResult(exitCode, output, lastError ?? (exitCode == 0 ? null : $"Command failed (exit {exitCode})."));
        }
        finally
        {
            _activeCommand?.Dispose();
            _activeCommand = null;
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            _commandGate.Release();
        }
    }

    internal static string BuildScript(string command, ShellKind shell, string resultPath, string? outputPath, bool nativeCommand)
    {
        string result = ShellCommand.Quote(resultPath, shell);
        string capture = outputPath is null ? "" : " > " + ShellCommand.Quote(outputPath, shell);
        if (shell == ShellKind.PowerShell)
        {
            string output = outputPath is null ? "" : $"Get-Content -LiteralPath {ShellCommand.Quote(outputPath, shell)} -Raw\n";
            string code = nativeCommand ? "$LASTEXITCODE" : "$(if ($?) { 0 } else { 1 })";
            return $"$global:LASTEXITCODE = 0\n$__dotnetupCode = 1\ntry {{\n"
                + $"Write-Host {ShellCommand.Quote(command, shell)}\n{command}{capture}\n"
                + $"$__dotnetupCode = {code}\n{output}}} catch {{ Write-Error $_; $__dotnetupCode = 1 }}\n"
                + $"finally {{ [IO.File]::WriteAllText({result}, [string]$__dotnetupCode) }}\n";
        }

        string display = command.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
            .Replace("<", "^<").Replace(">", "^>");
        string print = outputPath is null ? "" : $"type {ShellCommand.Quote(outputPath, shell)}\r\n";
        return $"@echo off\r\necho {display}\r\n{command}{capture}\r\n"
            + $"set \"__dotnetupCode=%errorlevel%\"\r\n{print}> {result} echo %__dotnetupCode%\r\n";
    }

    public async Task InterruptAsync()
    {
        if (_session is null)
        {
            return;
        }

        await _session.InterruptAsync(_lifetime.Token);
        if (IsBusy)
        {
            // Restart after Ctrl+C so no interrupted script can later complete a different operation.
            await Task.Delay(500);
            await StopSessionAsync();
            _activeCommand?.Cancel();
        }
    }

    public async Task RestartAsync(ShellKind shell)
    {
        if (IsBusy)
        {
            throw new InvalidOperationException("Stop the current command before restarting the terminal.");
        }

        await StopSessionAsync();
        await StartAsync(shell);
    }

    private async Task StopSessionAsync()
    {
        console.DetachSession();
        if (_session is not null)
        {
            _session.Exited -= Session_Exited;
            await _session.DisposeAsync();
            _session = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        await StopSessionAsync();
        _lifetime.Dispose();
    }

    public static string? FindExecutable(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Select(path => Path.Combine(path.Trim('"'), name)).FirstOrDefault(File.Exists);

    [GeneratedRegex(@"\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\a]*(?:\a|\x1B\\))")]
    private static partial Regex EscapeCodes();
}
