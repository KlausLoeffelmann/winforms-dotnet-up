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
    private RecordingConsoleSession? _recording;
    private CancellationTokenSource? _activeCommand;
    private volatile bool _exited;
    private readonly string _promptMarker = "dotnetup-" + Guid.NewGuid().ToString("N");

    public ShellKind Shell { get; private set; }
    public bool IsBusy => _activeCommand is not null;
    public event EventHandler? BusyChanged;

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
        string? dotnetup = SystemInventory.FindDotnetUp();
        if (dotnetup is not null)
        {
            start.Environment["PATH"] = Path.GetDirectoryName(dotnetup) + Path.PathSeparator
                + Environment.GetEnvironmentVariable("PATH");
        }

        if (shell == ShellKind.PowerShell)
        {
            start.ArgumentList.Add("-NoLogo");
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-ExecutionPolicy");
            start.ArgumentList.Add("Bypass");
            start.ArgumentList.Add("-NoExit");
        }
        else
        {
            start.ArgumentList.Add("/D");
            start.ArgumentList.Add("/Q");
            start.ArgumentList.Add("/V:OFF");
            start.Environment["PROMPT"] = $"$E]133;A;{_promptMarker}$E\\$P$G";
        }

        _exited = false;
        _session = new ConPtyConsoleSession(start);
        _session.Exited += Session_Exited;
        _recording = new RecordingConsoleSession(_session);
        await console.AttachSession(_recording, sendInputToSession: false, _lifetime.Token);
        if (shell == ShellKind.CommandPrompt)
        {
            using CancellationTokenSource ready = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            ready.CancelAfter(TimeSpan.FromSeconds(15));
            while (!ReadRecordedOutput().Contains(_promptMarker, StringComparison.Ordinal))
            {
                await Task.Delay(50, ready.Token);
            }
        }

        console.FocusPrompt();
    }

    private void Session_Exited(object? sender, ConsoleSessionExitedEventArgs e) => _exited = true;

    public Task<CommandResult> RunAsync(ShellCommand command, bool captureJson = false) =>
        RunCoreAsync(command.Render(Shell), captureJson, nativeCommand: true);

    public Task<CommandResult> RunTextAsync(string command) =>
        RunCoreAsync(command, captureJson: false, nativeCommand: false);

    public Task<CommandResult> RunInteractiveAsync(ShellCommand command) =>
        RunCoreAsync(command.Render(Shell), captureJson: false, nativeCommand: true, interactive: true);

    private async Task<CommandResult> RunCoreAsync(string command, bool captureJson, bool nativeCommand, bool interactive = false)
    {
        if (!await _commandGate.WaitAsync(0))
        {
            throw new InvalidOperationException("A command is already running. Wait or stop it before starting another.");
        }

        string folder = Path.Combine(Path.GetTempPath(), "DotnetUp.WinForms", Guid.NewGuid().ToString("N"));
        string resultPath = Path.Combine(folder, "exit.txt");
        string outputPath = Path.Combine(folder, "output.json");
        string scriptPath = Path.Combine(folder, Shell == ShellKind.PowerShell ? "command.ps1" : "command.cmd");
        try
        {
            if (_session is null || _exited)
            {
                throw new InvalidOperationException("The terminal has exited. Use Tools / Restart terminal.");
            }

            Directory.CreateDirectory(folder);
            _recording?.Clear();
            _activeCommand = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            BusyChanged?.Invoke(this, EventArgs.Empty);
            if (interactive)
            {
                console.InputMode = ConsoleInputMode.SendToSession;
                console.FocusOutput();
            }
            CancellationToken cancellation = _activeCommand.Token;
            string script = BuildScript(command, Shell, resultPath, captureJson ? outputPath : null, nativeCommand);
            if (Shell == ShellKind.CommandPrompt)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "display.txt"), command + "\r\n",
                    new UTF8Encoding(false), cancellation);
            }

            if (!nativeCommand && Shell == ShellKind.CommandPrompt)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "input.cmd"), command + "\r\n",
                    new UTF8Encoding(false), cancellation);
            }

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

                if (Shell == ShellKind.CommandPrompt && ReadRecordedOutput().Contains(_promptMarker, StringComparison.Ordinal))
                {
                    // A cmd syntax error can abort every batch frame without executing their cleanup.
                    exitCode = 1;
                    break;
                }

                await Task.Delay(100, cancellation);
            }

            await Task.Delay(100, cancellation);
            await console.FlushAsync(cancellation);
            string screen = ReadRecordedOutput();
            string output = captureJson && File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellation)
                : EscapeCodes().Replace(screen, "");
            string? lastError = exitCode == 0 ? null
                : EscapeCodes().Replace(screen, "")
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault(line => !line.Contains("exit.txt", StringComparison.OrdinalIgnoreCase)
                        && !line.StartsWith("PS ", StringComparison.Ordinal) && !line.EndsWith('>'))?.Trim();
            return new CommandResult(exitCode, output, lastError ?? (exitCode == 0 ? null : $"Command failed (exit {exitCode})."));
        }
        finally
        {
            _activeCommand?.Dispose();
            _activeCommand = null;
            if (interactive && !_lifetime.IsCancellationRequested)
            {
                console.InputMode = ConsoleInputMode.RaisePromptSubmitted;
                console.FocusPrompt();
            }
            BusyChanged?.Invoke(this, EventArgs.Empty);
            _recording?.Clear();
            try
            {
                if (Directory.Exists(folder))
                {
                    await DeleteCommandFolderAsync(folder);
                }
            }
            finally
            {
                _commandGate.Release();
            }
        }
    }

    private static async Task DeleteCommandFolderAsync(string folder)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
                return;
            }
            // cmd can still hold its batch file briefly after publishing the exit code.
            catch (IOException ex) when (attempt < 20 && (ex.HResult & 0xFFFF) is 32 or 33)
            {
                await Task.Delay(50);
            }
        }
    }

    private string ReadRecordedOutput() => _recording is null ? ""
        : string.Concat(_recording.Records.Where(record => !record.IsInput).Select(record => record.RawText));

    internal static string BuildScript(string command, ShellKind shell, string resultPath, string? outputPath, bool nativeCommand)
    {
        string result = ShellCommand.Quote(resultPath, shell);
        string capture = outputPath is null ? "" : " > " + ShellCommand.Quote(outputPath, shell);
        if (shell == ShellKind.PowerShell)
        {
            string output = outputPath is null ? "" : $"Get-Content -LiteralPath {ShellCommand.Quote(outputPath, shell)} -Raw\n";
            string code = nativeCommand ? "$LASTEXITCODE" : "$(if ($?) { 0 } else { 1 })";
            string invocation = nativeCommand ? command : $". ([scriptblock]::Create({ShellCommand.Quote(command, shell)}))";
            return $"$global:LASTEXITCODE = 0\n$__dotnetupCode = 1\ntry {{\n"
                + $"Write-Host {ShellCommand.Quote(command, shell)}\n{invocation}{capture}\n"
                + $"$__dotnetupCode = {code}\n{output}}} catch {{ Write-Error $_; $__dotnetupCode = 1 }}\n"
                + $"finally {{ [IO.File]::WriteAllText({result}, [string]$__dotnetupCode) }}\n";
        }

        string displayPath = ShellCommand.Quote(Path.Combine(Path.GetDirectoryName(resultPath)!, "display.txt"), shell);
        string print = outputPath is null ? "" : $"type {ShellCommand.Quote(outputPath, shell)}\r\n";
        string execute = nativeCommand ? command : $"call {ShellCommand.Quote(Path.Combine(Path.GetDirectoryName(resultPath)!, "input.cmd"), shell)}";
        return $"@echo off\r\nchcp 65001 >nul\r\ntype {displayPath}\r\n{execute}{capture}\r\n"
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
            if (_recording is not null)
            {
                await _recording.DisposeAsync();
                _recording = null;
            }
            else
            {
                await _session.DisposeAsync();
            }

            _session = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        await StopSessionAsync();
        await _commandGate.WaitAsync();
        _commandGate.Release();
        _lifetime.Dispose();
        _commandGate.Dispose();
    }

    public static string? FindExecutable(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Select(path => Path.Combine(path.Trim('"'), name)).FirstOrDefault(File.Exists);

    [GeneratedRegex(@"\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\a]*(?:\a|\x1B\\))")]
    private static partial Regex EscapeCodes();
}
