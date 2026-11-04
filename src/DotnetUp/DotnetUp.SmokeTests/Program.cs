using DotnetUp.WinFormsApp;
using WarpToolkit.WinForms.DirectX.Controls;

namespace DotnetUp.SmokeTests;

internal static class Program
{
    private static int _exitCode;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.FirstOrDefault() == "--probe")
        {
            return RunProbe(args.Skip(1).ToArray());
        }

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
                    await CheckTerminalPathsAsync(terminal, console, shell);
                    Console.WriteLine($"PASS: {shell} hosted terminal, output, failure and typed command");
                }

                await CheckApplicationUiAsync(host);
            }
            catch (Exception ex)
            {
                _exitCode = 1;
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine(console.OutputText);
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

    private static int RunProbe(string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "inventory":
                Console.WriteLine("""{"installSpecs":[],"installations":[{"component":"SDK","version":"10.0.103","installRoot":"C:\\managed","architecture":"x64"}]}""");
                return 0;
            case "fail":
                Console.WriteLine("A diagnostic before failure.");
                Console.Error.WriteLine("precise-last-error");
                return 23;
            case "wait":
                Thread.Sleep(15000);
                return 0;
            default:
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(args));
                return 0;
        }
    }

    private static async Task CheckTerminalPathsAsync(HostedTerminal terminal, D2DConsole console, ShellKind shell)
    {
        string executable = Environment.ProcessPath!;
        CommandResult result = await terminal.RunAsync(new ShellCommand(executable, ["--probe", "inventory"]), captureJson: true)
            .WaitAsync(TimeSpan.FromSeconds(30));
        Require(result.Succeeded && ManagedInventory.Parse(result.Output).Installations.Count == 1,
            $"{shell} captures real executable JSON output");
        Require(console.OutputText.Contains("10.0.103"), $"{shell} captured JSON is also visible in terminal");
        string[] values = ["spaces & apostrophe's", @"C:\space folder\", "unicode-\u00e4"];
        result = await terminal.RunAsync(new ShellCommand(executable, ["--probe", .. values]), captureJson: true)
            .WaitAsync(TimeSpan.FromSeconds(30));
        Require(result.Succeeded && System.Text.Json.JsonSerializer.Deserialize<string[]>(result.Output)!.SequenceEqual(values),
            $"{shell} preserves argument boundaries, Unicode and trailing backslashes");
        result = await terminal.RunAsync(new ShellCommand(executable, ["--probe", "fail"]))
            .WaitAsync(TimeSpan.FromSeconds(30));
        Require(result.ExitCode == 23 && result.LastError?.Contains("precise-last-error") == true,
            $"{shell} reports actual last error: {result.LastError}");
        result = await terminal.RunTextAsync(shell == ShellKind.PowerShell ? "Write-Output 'unterminated" : "if (")
            .WaitAsync(TimeSpan.FromSeconds(30));
        Require(!result.Succeeded, $"{shell} malformed editable commands report failure without hanging");
        Task<CommandResult> pending = terminal.RunAsync(new ShellCommand(executable, ["--probe", "wait"]));
        await Task.Delay(300);
        try
        {
            await terminal.RunTextAsync("echo must-not-overlap");
            throw new InvalidOperationException("Overlapping commands must be rejected");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already running"))
        {
        }

        await terminal.InterruptAsync();
        try
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (OperationCanceledException)
        {
        }

        await terminal.RestartAsync(shell);
        result = await terminal.RunAsync(new ShellCommand(executable, ["--probe", "restarted"])).WaitAsync(TimeSpan.FromSeconds(30));
        Require(result.Succeeded, $"{shell} can restart after cancellation");
        Console.WriteLine($"PASS: {shell} JSON, quoting, errors, malformed input, serialization and cancellation/restart");
    }

    private static async Task CheckApplicationUiAsync(Form host)
    {
        using MainForm main = new();
        main.Show(host);
        WarpToolkit.WinForms.Containers.Wizard.WizardContainer? wizard = null;
        await WaitUntilAsync(() =>
        {
            wizard = Application.OpenForms.Cast<Form>().SelectMany(Descendants)
                .OfType<WarpToolkit.WinForms.Containers.Wizard.WizardContainer>().FirstOrDefault();
            return wizard is not null;
        }, "First launch opens the setup wizard");
        Require(wizard!.PageCount == 3, "Setup offers welcome, choices and review pages");
        Require(await wizard.GoNextAsync(), "Wizard can navigate to setup choices");
        Require(await wizard.GoNextAsync(), "Wizard can navigate to command review");
        string review = Descendants(wizard).OfType<TextBox>().Single(box => box.Multiline).Text;
        Require(review.Contains("env") && review.Contains("none") && !review.Contains("'everywhere'"),
            "First-run review shows the safe environment plan");
        Form wizardForm = wizard.FindForm()!;
        wizardForm.Close();
        await WaitUntilAsync(() => wizardForm.IsDisposed, "Wizard can be postponed without executing installation");
        MenuStrip menu = main.MainMenuStrip!;
        Require(menu.Items.Cast<ToolStripItem>().Select(item => item.Text).SequenceEqual(["&File", "&Channel", "&Repository", "&Tools"]),
            "All requested top-level menus are present");
        ToolStripMenuItem channels = (ToolStripMenuItem)menu.Items[1];
        Require(channels.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Checked).Text == "lts",
            "LTS is the conservative default");
        TreeView tree = Descendants(main).OfType<TreeView>().Single();
        Require(tree.Nodes[0].Text == "System"
            && tree.Nodes[0].Nodes.Cast<TreeNode>().Select(node => node.Text).SequenceEqual(["GAC", ".NET SDKs", "Drive C", "Drive N"]),
            "System navigation has GAC, SDKs and both drives");
        DataGridView grid = Descendants(main).OfType<DataGridView>().Single();
        Require(grid.GetType().Name == "WarpDataGridView", "Details use the actual WARP grid");
        Require(grid.Rows.Count > 0, "System selection populates details");
        grid.Rows[0].Selected = true;
        StatusStrip status = Descendants(main).OfType<StatusStrip>().Single();
        Require(status.Items[0].Text?.StartsWith("dotnetup:") == true, "Selected grid row takes status precedence");
        Require(DateTime.TryParse(status.Items[^1].Text, out _), "Far-right status shows date and seconds");
        using (SdkTaskDialog dialog = new("dotnetup.exe", ShellKind.PowerShell, "lts", false, null, null))
        {
            dialog.Show(host);
            await Task.Delay(100);
            Require(Descendants(dialog).OfType<TextBox>().Single(box => box.Multiline).Height >= 80,
                "Install command preview has a usable viewport");
            Require(Descendants(dialog).OfType<Button>().Where(button => button.Text is "&Run" or "Cancel")
                .All(button => button.Visible && dialog.ClientRectangle.Contains(dialog.RectangleToClient(button.RectangleToScreen(button.ClientRectangle)))),
                "Install dialog actions remain in the client area");
            dialog.Close();
        }

        using (OptionsDialog options = new(new AppPreferences()))
        {
            options.Show(host);
            await Task.Delay(100);
            Require(Descendants(options).OfType<ComboBox>().Single().Items.Count == 2, "Options exposes both shell types");
            options.Close();
        }

        main.Close();
        await WaitUntilAsync(() => main.IsDisposed, "Application closes and disposes hosted terminal");
        Console.WriteLine("PASS: real first-run wizard, menus, WARP grid, status precedence, dialogs and clean shutdown");
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string message)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        while (!condition())
        {
            Require(!timeout.IsCancellationRequested, message);
            await Task.Delay(50);
        }
    }
}
