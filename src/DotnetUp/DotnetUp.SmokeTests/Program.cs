using DotnetUp.WinFormsApp;
using WarpToolkit.WinForms.DirectX.Controls;

namespace DotnetUp.SmokeTests;

internal static class Program
{
    private static int _exitCode;

    [STAThread]
    private static int Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        using Form host = new() { Width = 900, Height = 600, Text = "dotnetup regression checks" };
        D2DConsole console = new() { Dock = DockStyle.Fill };
        host.Controls.Add(console);
        host.Shown += async (_, _) =>
        {
            try
            {
                foreach (ShellKind shell in Enum.GetValues<ShellKind>())
                {
                    await using HostedTerminal terminal = new(console);
                    await terminal.StartAsync(shell);
                    CommandResult result = await terminal.RunAsync(new ShellCommand(
                        Path.Combine(Environment.SystemDirectory, "cmd.exe"), ["/d", "/c", "echo", "terminal-pass"])).WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.Succeeded, $"{shell} command succeeds: {result.LastError}\n{console.OutputText}");
                    await Task.Delay(300);
                    await console.FlushAsync();
                    Require(console.OutputText.Contains("terminal-pass"), $"{shell} output is visible");
                    result = await terminal.RunAsync(new ShellCommand(
                        Path.Combine(Environment.SystemDirectory, "cmd.exe"), ["/d", "/c", "exit", "7"])).WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.ExitCode == 7, $"{shell} preserves native exit code");
                    result = await terminal.RunTextAsync("echo typed-command").WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.Succeeded, $"{shell} accepts editable commands");
                    Console.WriteLine($"PASS: {shell} hosted terminal, output, failure and typed command");
                }
            }
            catch (Exception ex)
            {
                _exitCode = 1;
                Console.Error.WriteLine(ex);
            }
            finally
            {
                host.Close();
            }
        };
        Application.Run(host);
        return _exitCode;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
