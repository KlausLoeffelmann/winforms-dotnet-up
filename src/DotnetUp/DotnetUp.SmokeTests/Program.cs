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
                CheckInventory();
                CheckRepositoryDiscovery();
                CheckCommandPlans();
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

    private static void CheckInventory()
    {
        ManagedInventory inventory = ManagedInventory.Parse("""
            {
              "installSpecs": [{"component":"SDK","versionOrChannel":"lts","installRoot":"C:\\managed","source":"Explicit","future":42}],
              "installations": [{"component":"SDK","version":"10.0.103","installRoot":"C:\\managed","architecture":"x64"}]
            }
            """);
        Require(inventory.Specifications.Single().Channel == "lts", "Stored channel is distinct from installed version");
        Require(inventory.Installations.Single().Value == "10.0.103", "Actual versions populate inventory");
        try
        {
            ManagedInventory.Parse("{}");
            throw new InvalidOperationException("Malformed inventory must not look like an empty successful result.");
        }
        catch (System.Text.Json.JsonException)
        {
        }

        Console.WriteLine("PASS: structured dotnetup inventory and unsupported schema errors");
    }

    private static void CheckRepositoryDiscovery()
    {
        string folder = Path.Combine(Path.GetTempPath(), "DotnetUp.SmokeTests", Guid.NewGuid().ToString("N"));
        try
        {
            string repository = Path.Combine(folder, "repos", "example");
            Directory.CreateDirectory(Path.Combine(repository, ".git"));
            File.WriteAllText(Path.Combine(repository, ".git", "config"),
                "[remote \"origin\"]\r\n url = git@github.com:example/project.git\r\n");
            File.WriteAllText(Path.Combine(folder, "global.json"), """{"sdk":{"version":"10.0.103"}}""");
            string unrelated = Path.Combine(folder, "unrelated");
            Directory.CreateDirectory(Path.Combine(unrelated, ".git"));
            File.WriteAllText(Path.Combine(unrelated, ".git", "config"),
                "[remote \"origin\"]\r\n url = https://github.com.evil.example/repo\r\n");
            RepositoryScan scan = RepositoryScanner.Scan(folder, null, CancellationToken.None);
            Require(scan.Repositories.Count == 1, "Only genuine GitHub remotes are discovered");
            RepositoryInfo repo = scan.Repositories.Single();
            Require(repo.GlobalJson == Path.Combine(folder, "global.json"), "Nearest ancestor global.json is used");
            Require(repo.GetDetails().Any(row => row.Name == "sdk.version" && row.Value == "10.0.103"), "Repository SDK details are displayed");
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            try
            {
                RepositoryScanner.Scan(folder, null, cancellation.Token);
                throw new InvalidOperationException("Discovery must observe cancellation.");
            }
            catch (OperationCanceledException)
            {
            }

            Console.WriteLine("PASS: GitHub discovery, SDK requirements and scan cancellation");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static void CheckCommandPlans()
    {
        AppPreferences preferences = new();
        using SdkTaskDialog dialog = new("dotnetup.exe", ShellKind.PowerShell, preferences.Channel, false, null, null);
        ComboBox channel = (ComboBox)typeof(SdkTaskDialog)
            .GetField("channelChoice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(dialog)!;
        channel.Text = "preview";
        Require(dialog.GetOptions().Channel == "preview" && preferences.Channel == "lts", "Dialog override does not persist");
        IReadOnlyList<ShellCommand> plan = DotnetUpCommands.Plan("dotnetup.exe",
            new SdkTaskOptions(true, "preview", OverrideUpdateChannel: true, IncludeRuntimes: true));
        Require(plan.Count == 2 && plan[0].Arguments.Take(3).SequenceEqual(["sdk", "install", "preview"]),
            "Update channel override installs explicitly first");
        Require(plan[1].Arguments.Take(2).SequenceEqual(["sdk", "update"])
            && !plan[1].Arguments.Contains("preview") && plan[1].Arguments.Contains("--all"),
            "Update uses documented options, not an unsupported channel argument");
        plan = DotnetUpCommands.Plan("dotnetup.exe",
            new SdkTaskOptions(false, "lts", RepositoryPath: @"C:\repo", UseGlobalJson: true));
        Require(!plan[0].Arguments.Contains("lts") && plan[0].WorkingDirectory == @"C:\repo",
            "Repository install resolves nearest global.json without overriding its channel");
        plan = SetupWizard.SetupCommands("dotnetup.exe", new SetupOptions("lts", "none", false, false, true));
        Require(plan[1].Arguments.SequenceEqual(["env", "set", "none", "--dotnetup-on-path", "false"]),
            "Safe setup does not modify PATH by default");
        Require(plan.All(command => !command.Arguments.Contains("init") && !command.Arguments.Contains("everywhere")),
            "Safe setup does not enter interactive recommended machine-wide mode");
        try
        {
            DotnetUpCommands.Plan("dotnetup.exe", new SdkTaskOptions(false, "lts; Remove-Item"));
            throw new InvalidOperationException("Channel shell syntax must be rejected");
        }
        catch (ArgumentException)
        {
        }

        Console.WriteLine("PASS: documented SDK/setup plans, safe defaults and dialog-only channel overrides");
    }
}
