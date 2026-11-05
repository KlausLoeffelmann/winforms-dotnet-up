using DotnetUp.WinFormsApp;
using WarpToolkit.WinForms.DirectX.Controls;

namespace DotnetUp.SmokeTests;

internal static partial class Program
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
        Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
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
                CheckSelectedSdkUpdates();
                await CheckWorkspaceToolsAsync(host);
                foreach (ShellKind shell in args.Contains("--ui-only") ? Array.Empty<ShellKind>() : Enum.GetValues<ShellKind>())
                {
                    await using HostedTerminal terminal = new(console);
                    await terminal.StartAsync(shell);
                    CommandResult result = await terminal.RunAsync(new ShellCommand(
                        Path.Combine(Environment.SystemDirectory, "cmd.exe"), ["/d", "/c", "echo", "terminal-pass"])).WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.Succeeded, $"{shell} command succeeds: {result.LastError}\n{ConsoleOutput(console)}");
                    await Task.Delay(300);
                    await console.FlushAsync();
                    Require(ConsoleOutput(console).Contains("terminal-pass"), $"{shell} output is visible");
                    result = await terminal.RunAsync(new ShellCommand(
                        Path.Combine(Environment.SystemDirectory, "cmd.exe"), ["/d", "/c", "exit", "7"])).WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.ExitCode == 7, $"{shell} preserves native exit code");
                    result = await terminal.RunTextAsync("echo typed-command").WaitAsync(TimeSpan.FromSeconds(30));
                    Require(result.Succeeded, $"{shell} accepts editable commands");
                    await CheckTerminalPathsAsync(terminal, console, shell);
                    await CheckGitBranchesAsync(terminal, console, shell);
                    Console.WriteLine($"PASS: {shell} hosted terminal, output, failure and typed command");
                }

                await CheckApplicationUiAsync(host);
            }
            catch (Exception ex)
            {
                _exitCode = 1;
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine(ConsoleOutput(console));
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

    private static string ConsoleOutput(D2DConsole console) => string.Join(Environment.NewLine,
        Enumerable.Range(0, console.Buffer.LineCount).Select(console.Buffer.GetLineText));

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
            File.WriteAllText(Path.Combine(repository, "Example.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(folder, "global.json"), """{"sdk":{"version":"10.0.103"}}""");
            string unrelated = Path.Combine(folder, "unrelated");
            Directory.CreateDirectory(Path.Combine(unrelated, ".git"));
            File.WriteAllText(Path.Combine(unrelated, ".git", "config"),
                "[remote \"origin\"]\r\n url = https://github.com.evil.example/repo\r\n");
            WorkspaceScan scan = WorkspaceScanner.Scan(folder, null, CancellationToken.None);
            Require(scan.Repositories.Count == 1, "Only genuine GitHub remotes are discovered");
            RepositoryInfo repo = scan.Repositories.Single();
            Require(repo.GlobalJson == Path.Combine(folder, "global.json"), "Nearest ancestor global.json is used");
            Require(repo.GetDetails().Any(row => row.Name == "sdk.version" && row.Value == "10.0.103"), "Repository SDK details are displayed");
            Require(WorkspaceScanner.Scan(folder, null, CancellationToken.None, maxDepth: 1).Repositories.Count == 0,
                "Depth 1 does not inspect second-level repositories");
            Require(WorkspaceScanner.Scan(folder, null, CancellationToken.None, maxDepth: 2).Repositories.Count == 1,
                "Repositories at the exact depth limit are inspected");
            string deep = Path.Combine(repository, "nested");
            Directory.CreateDirectory(Path.Combine(deep, ".git"));
            File.WriteAllText(Path.Combine(deep, ".git", "config"),
                "[remote \"origin\"]\r\n url = https://github.com/example/nested.git\r\n");
            File.WriteAllText(Path.Combine(deep, "Nested.fsproj"), "<Project />");
            Require(WorkspaceScanner.Scan(folder, null, CancellationToken.None, maxDepth: 2).Repositories.Count == 1,
                "Scanning stops below the depth boundary, including inside repositories");
            Require(WorkspaceScanner.Scan(folder, null, CancellationToken.None, maxDepth: 0).Repositories.Count == 2,
                "Zero preserves unlimited discovery");
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            try
            {
                WorkspaceScanner.Scan(folder, null, cancellation.Token);
                throw new InvalidOperationException("Discovery must observe cancellation.");
            }
            catch (OperationCanceledException)
            {
            }

            string local = Path.Combine(folder, "local");
            Directory.CreateDirectory(local);
            File.WriteAllText(Path.Combine(local, "Local.slnx"), "<Solution><Project Path=\"App.csproj\" /></Solution>");
            File.WriteAllText(Path.Combine(local, "App.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(local, "Program.cs"), "// must not be a tree item");
            string web = Path.Combine(folder, "website");
            Directory.CreateDirectory(web);
            File.WriteAllText(Path.Combine(web, "web.config"), "<configuration />");
            scan = WorkspaceScanner.Scan(folder, null, CancellationToken.None);
            Require(scan.Entries.Any(entry => entry.Kind == WorkspaceKind.Solution && entry.Repository is null)
                && scan.Entries.Any(entry => entry.Kind == WorkspaceKind.WebApplication),
                "Local solutions and ASP.NET applications are discovered without GitHub");
            WorkspaceEntry solution = scan.Entries.Single(entry => entry.Kind == WorkspaceKind.Solution);
            Require(WorkspaceScanner.ReadSolutionProjects(solution).Single().Path == Path.Combine(local, "App.csproj")
                && scan.Entries.All(entry => !entry.Path.EndsWith("Program.cs")),
                "Solutions expose project references, never source files");
            Console.WriteLine("PASS: depth-limited solution/project/web discovery, GitHub annotations and cancellation");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static void CheckCommandPlans()
    {
        AppPreferences preferences = new();
        foreach (int invalidDepth in new[] { -1, 101 })
        {
            try
            {
                new AppPreferences { RepositoryScanDepth = invalidDepth }.Validate();
                throw new InvalidOperationException("Out-of-range scan depth must be rejected.");
            }
            catch (ArgumentException)
            {
            }
        }

        Require(SetupWizard.IsRequired(preferences, null), "Missing tool on first launch needs setup");
        Require(!SetupWizard.IsRequired(preferences, "dotnetup.exe"), "Existing tool does not need first-run setup");
        Require(!SetupWizard.IsRequired(new AppPreferences { SetupCompleted = true }, null),
            "Completed setup is not automatically repeated");
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

    private static void CheckSelectedSdkUpdates()
    {
        DetailRow sdk = new("SDK", "10.0.103", "Managed installation", @"C:\managed", "x64") { Component = "SDK" };
        DetailRow requirement = new("SDK", "lts", "Tracked requirement", @"N:\private-dotnet", "Explicit")
        { Component = "SDK", Channel = "lts" };
        IReadOnlyList<ShellCommand> plan = SelectedSdkUpdates.Plan("dotnetup.exe", [sdk, requirement, sdk]);
        Require(plan.Count == 2 && plan[0].Arguments.Take(3).SequenceEqual(["sdk", "install", "10.0"])
            && plan[0].Arguments.Contains(@"C:\managed") && plan[1].Arguments.Contains(@"N:\private-dotnet"),
            "Multi-select updates target only selected SDK channels and their exact roots, without duplicates");
        Require(plan.All(command => !command.Arguments.Contains("update") && !command.Arguments.Contains("--all")),
            "Selected updates never invoke root-wide SDK/runtime updates");
        Require(!SelectedSdkUpdates.CanUpdate(sdk with { Kind = "External installation" })
            && !SelectedSdkUpdates.CanUpdate(sdk with { Component = "Runtime" }),
            "External SDKs and runtimes cannot be updated through SDK selection");
        try
        {
            SelectedSdkUpdates.Plan("dotnetup.exe", [sdk, sdk with { Kind = "External installation" }]);
            throw new InvalidOperationException("Mixed unsupported selections must be rejected");
        }
        catch (ArgumentException)
        {
        }

        Console.WriteLine("PASS: strictly selected SDK update scope and read-only external installations");
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
            case "interactive":
                Console.WriteLine("interactive-ready");
                string? input = Console.ReadLine();
                File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(new
                {
                    Directory = Environment.CurrentDirectory,
                    Input = input,
                    Branch = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, ".git", "HEAD")).Trim()
                }));
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
        Require(ConsoleOutput(console).Contains("10.0.103"), $"{shell} captured JSON is also visible in terminal");
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
        await WaitUntilAsync(() => Descendants(main).OfType<DataGridView>().Single().Rows.Count > 0,
            "Startup inventory is displayed");
        if (SystemInventory.FindDotnetUp() is null)
        {
            SetupWizard? startupWizard = null;
            await WaitUntilAsync(() =>
            {
                startupWizard = Application.OpenForms.OfType<SetupWizard>().FirstOrDefault();
                return startupWizard is not null;
            }, "First launch with no tool opens setup");
            startupWizard!.Close();
            await WaitUntilAsync(() => startupWizard.IsDisposed, "Startup setup is postponed");
        }
        else
        {
            await Task.Delay(200);
            Require(!Application.OpenForms.OfType<SetupWizard>().Any(), "Installed dotnetup skips startup setup");
        }

        Console.WriteLine("PASS: startup wizard eligibility");
        using SetupWizard wizard = new("lts", host.Font, alreadyInstalled: false);
        Task<WarpToolkit.WinForms.Containers.Wizard.WizardDialogResult> wizardTask = wizard.ShowSetupAsync(host);
        await Task.Delay(100);
        Button next = Descendants(wizard).OfType<Button>().Single(button => button.Text == "&Next");
        next.PerformClick();
        Require(Descendants(wizard).OfType<ComboBox>().All(box => box.Visible), "Wizard navigates to setup choices");
        Require(Descendants(wizard).OfType<CheckBox>().All(box => box.Appearance == Appearance.ToggleSwitch),
            "Setup options use .NET 11 toggle switches");
        next.PerformClick();
        string review = Descendants(wizard).OfType<TextBox>().Single(box => box.Multiline).Text;
        Require(review.Contains("env") && review.Contains("none") && !review.Contains("'everywhere'"),
            "First-run review shows the safe environment plan");
        Require(next.Text == "&Finish", "Review page offers Finish");
        Console.WriteLine("PASS: wizard navigation and review");
        foreach (float size in new[] { 10F, 18F, 24F, 36F })
        {
            using Font font = new("Segoe UI", size);
            wizard.Font = font;
            wizard.PerformLayout();
            Require(Descendants(wizard).OfType<Button>().All(button =>
                button.Visible && button.Height >= button.GetPreferredSize(Size.Empty).Height
                && wizard.ClientRectangle.Contains(wizard.RectangleToClient(button.RectangleToScreen(button.ClientRectangle)))),
                $"Wizard captions fit inside the client area at {size} pt");
            wizard.Font = host.Font;
        }

        Descendants(wizard).OfType<Button>().Single(button => button.Text == "&Cancel").PerformClick();
        Require(await wizardTask.WaitAsync(TimeSpan.FromSeconds(10)) == WarpToolkit.WinForms.Containers.Wizard.WizardDialogResult.Cancelled,
            "Wizard can be postponed without executing installation");
        Console.WriteLine("PASS: wizard font layout and cancellation");
        MenuStrip menu = main.MainMenuStrip!;
        Require(menu.Items.Cast<ToolStripItem>().Select(item => item.Text).SequenceEqual(["&File", "&Channel", "&Workspace", "&Tools"]),
            "All requested top-level menus are present");
        ToolStripMenuItem channels = (ToolStripMenuItem)menu.Items[1];
        Require(channels.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Checked).Text == "lts",
            "LTS is the conservative default");
        ToolStrip toolbar = Descendants(main).OfType<ToolStrip>().Single(strip => strip is not MenuStrip && strip is not StatusStrip);
        Require(toolbar.Items.OfType<ToolStripButton>().All(button =>
            button.Image is not null && button.DisplayStyle == ToolStripItemDisplayStyle.Image
            && !string.IsNullOrWhiteSpace(button.AccessibleName) && !string.IsNullOrWhiteSpace(button.ToolTipText)),
            "Toolbar commands have images, accessible names and explanatory tooltips");
        Require(toolbar.AutoSize && toolbar.LayoutStyle == ToolStripLayoutStyle.HorizontalStackWithOverflow,
            "Toolbar uses an autosizing mixed-item layout with overflow");
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
        ToolStripItem updateSelected = toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Update selected SDKs to latest");
        Require(!updateSelected.Enabled, "Update selected is disabled for non-SDK selections");
        grid.DataSource = new List<DetailRow>
        {
            new("SDK", "10.0.103", "Managed installation", @"C:\managed", "x64") { Component = "SDK" },
            new("SDK", "11.0.100", "Managed installation", @"N:\managed", "x64") { Component = "SDK" }
        };
        grid.ClearSelection();
        grid.Rows[0].Selected = true;
        grid.Rows[1].Selected = true;
        Require(grid.MultiSelect && updateSelected.Enabled
            && grid.ContextMenuStrip!.Items.Cast<ToolStripItem>().Single(item => item.Text!.Contains("selected SDKs")).Enabled
            && ((ToolStripMenuItem)menu.Items[0]).DropDownItems.Cast<ToolStripItem>().Single(item => item.Text!.Contains("selected SDKs")).Enabled,
            "Multi-select enables matching toolbar, context and menu commands together");
        grid.ClearSelection();
        Require(!updateSelected.Enabled, "Clearing SDK selection disables update selected again");
        await CheckWorkspaceActionsAsync(main, tree, toolbar);
        using (SdkTaskDialog dialog = new("dotnetup.exe", ShellKind.PowerShell, "lts", false, null, null))
        {
            dialog.Show(host);
            await Task.Delay(100);
            Require(Descendants(dialog).OfType<TextBox>().Single(box => box.Multiline).Height >= 80,
                "Install command preview has a usable viewport");
            Require(Descendants(dialog).OfType<Button>().Where(button => button.Text is "&Run" or "Cancel")
                .All(button => button.Visible && dialog.ClientRectangle.Contains(dialog.RectangleToClient(button.RectangleToScreen(button.ClientRectangle)))),
                "Install dialog actions remain in the client area");
            Require(Descendants(dialog).OfType<CheckBox>().All(box => box.Appearance == Appearance.ToggleSwitch),
                "SDK options use .NET 11 toggle switches");
            dialog.Close();
        }

        using (OptionsDialog options = new(new AppPreferences()))
        {
            options.Show(host);
            await Task.Delay(100);
            Require(Descendants(options).OfType<ComboBox>().Single().Items.Count == 2, "Options exposes both shell types");
            WarpToolkit.WinForms.Containers.FluentTabControl tabs =
                Descendants(options).OfType<WarpToolkit.WinForms.Containers.FluentTabControl>().Single();
            Require(tabs.Tabs.Count() == 2 && !Descendants(options).OfType<TabControl>().Any(),
                "Separate options views use WARP Fluent tabs, not native tabs");
            Require(Descendants(options).OfType<TextBox>().Count(box => box.ReadOnly) == 2,
                "Font pickers show their values in read-only editors");
            tabs.CurrentTabIndex = 1;
            NumericUpDown depth = Descendants(options).OfType<NumericUpDown>().Single();
            Require(depth.Visible && depth.Value == 0, "Repository options preserve unlimited default");
            depth.Value = 3;
            AppPreferences changed = new();
            options.ApplyTo(changed);
            Require(changed.RepositoryScanDepth == 3, "Options apply the chosen scan depth");
            AppPreferences restored = System.Text.Json.JsonSerializer.Deserialize<AppPreferences>(
                System.Text.Json.JsonSerializer.Serialize(changed))!;
            Require(restored.RepositoryScanDepth == 3, "Scan depth survives preferences persistence");
            options.Close();
        }

        foreach (float size in new[] { 18F, 24F, 36F })
        {
            using Font font = new("Segoe UI", size);
            using OptionsDialog options = new(new AppPreferences()) { Font = font };
            options.Show(host);
            await Task.Delay(100);
            Require(Descendants(options).OfType<Button>().Where(button => button.Text is "&OK" or "&Cancel").All(button =>
                button.Height >= button.GetPreferredSize(Size.Empty).Height
                && options.ClientRectangle.Contains(options.RectangleToClient(button.RectangleToScreen(button.ClientRectangle)))),
                $"Options footer remains usable at {size} pt");
            Require(Descendants(options).OfType<UserControl>().All(page => page.AutoScroll),
                "Option pages own their overflow scrolling");
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
