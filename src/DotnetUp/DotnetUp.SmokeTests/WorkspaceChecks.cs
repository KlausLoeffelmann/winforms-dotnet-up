using System.Reflection;
using System.Text;
using System.Text.Json;
using DotnetUp.WinFormsApp;
using WarpToolkit.WinForms.DirectX.Controls;

namespace DotnetUp.SmokeTests;

internal static partial class Program
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private static async Task CheckWorkspaceToolsAsync(Form host)
    {
        string folder = Path.Combine(Path.GetTempPath(), "DotnetUp.SmokeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string workspace = Path.Combine(folder, "workspace with spaces");
            Directory.CreateDirectory(workspace);
            SdkTaskOptions options = new(false, "10.0.103", Path.Combine(workspace, ".dotnet"), workspace);
            WorkspaceSdkConfiguration configuration = WorkspaceSdkConfiguration.Prepare(options);
            Require(!File.Exists(configuration.Path), "Preparing local SDK configuration does not modify files");
            configuration.Save();
            using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(configuration.Path)))
            {
                Require(json.RootElement.GetProperty("sdk").GetProperty("version").GetString() == "10.0.103"
                    && json.RootElement.GetProperty("sdk").GetProperty("paths")[0].GetString() == options.InstallPath,
                    "Dedicated SDK configuration pins the requested version and installation root");
            }

            configuration = WorkspaceSdkConfiguration.Prepare(options);
            File.AppendAllText(configuration.Path, " ");
            byte[] changed = File.ReadAllBytes(configuration.Path);
            try
            {
                configuration.Save();
                throw new InvalidOperationException("Concurrent global.json changes must be rejected");
            }
            catch (IOException)
            {
                Require(File.ReadAllBytes(configuration.Path).SequenceEqual(changed), "Concurrent configuration is not overwritten");
            }

            string child = Path.Combine(workspace, "child");
            Directory.CreateDirectory(child);
            configuration = WorkspaceSdkConfiguration.Prepare(options with { RepositoryPath = child, InstallPath = Path.Combine(child, ".dotnet"), UseGlobalJson = true });
            configuration.Save();
            Require(File.ReadAllBytes(Path.Combine(workspace, "global.json")).SequenceEqual(changed),
                "A project-local override never edits an ancestor global.json");
            try
            {
                WorkspaceSdkConfiguration.Prepare(options with { Channel = "lts" });
                throw new InvalidOperationException("Named channels cannot silently become exact project pins");
            }
            catch (ArgumentException)
            {
            }

            File.WriteAllText(Path.Combine(workspace, "global.json"), """
                {"sdk":{"version":"10.0.103","rollForward":"disable","paths":["cached-sdk"]},"custom":"\u00e4"}
                """.Replace("\n", "\r\n"), Encoding.Unicode);
            WorkspaceSdkConfiguration local = WorkspaceSdkConfiguration.Prepare(options with { Channel = "10.0.104" });
            local.Save();
            byte[] unicodeConfiguration = File.ReadAllBytes(local.Path);
            Require(unicodeConfiguration.Take(2).SequenceEqual(Encoding.Unicode.GetPreamble()),
                "Local SDK configuration preserves its UTF-16 BOM");
            string inheritedChild = Path.Combine(workspace, "inherited");
            Directory.CreateDirectory(inheritedChild);
            WorkspaceSdkConfiguration inherited = WorkspaceSdkConfiguration.Prepare(options with
            { RepositoryPath = inheritedChild, InstallPath = Path.Combine(inheritedChild, ".dotnet"), UseGlobalJson = true });
            inherited.Save();
            using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(inherited.Path)))
            {
                JsonElement sdk = json.RootElement.GetProperty("sdk");
                Require(sdk.GetProperty("version").GetString() == "10.0.104" && sdk.GetProperty("rollForward").GetString() == "disable"
                    && sdk.GetProperty("paths").EnumerateArray().Any(item => item.GetString() == Path.Combine(workspace, "cached-sdk"))
                    && json.RootElement.GetProperty("custom").GetString() == "\u00e4"
                    && File.ReadAllBytes(local.Path).SequenceEqual(unicodeConfiguration),
                    "A local override preserves custom values/roll-forward and resolves inherited relative SDK paths without editing the ancestor");
            }

            string solutionPath = Path.Combine(workspace, "Legacy.sln");
            File.WriteAllText(solutionPath, "Project(\"{TYPE}\") = \"App\", \"src\\App.csproj\", \"{ID}\"\r\nEndProject\r\n");
            Require(WorkspaceScanner.ReadSolutionProjects(new(solutionPath, WorkspaceKind.Solution, null)).Single().Path
                == Path.Combine(workspace, "src", "App.csproj"), "Classic solutions expose project references");
            string xmlSolution = Path.Combine(workspace, "Invalid.slnx");
            File.WriteAllText(xmlSolution, "<!DOCTYPE Solution [<!ENTITY x 'unsafe'>]><Solution><Project Path='&x;' /></Solution>");
            try
            {
                WorkspaceScanner.ReadSolutionProjects(new(xmlSolution, WorkspaceKind.Solution, null));
                throw new InvalidOperationException("Solution XML must not process DTD entities");
            }
            catch (System.Xml.XmlException)
            {
            }

            RepositoryInfo repo = new("repo", workspace, "git@github.com:owner/.github.git", null);
            Require(WorkspaceTools.RepositoryWebsite(repo).AbsoluteUri == "https://github.com/owner/.github",
                "GitHub website conversion preserves repository names containing .git");
            Require(WorkspaceTools.LaunchInfo("devenv.exe", Path.Combine(workspace, "Example.slnx"), workspace)
                .ArgumentList.Single() == Path.Combine(workspace, "Example.slnx"), "IDE paths remain one safe argument");
            string ideRoot = Path.Combine(folder, "Visual Studio");
            Directory.CreateDirectory(Path.Combine(ideRoot, "Common7", "IDE"));
            File.WriteAllText(Path.Combine(ideRoot, "Common7", "IDE", "devenv.exe"), "");
            string instances = JsonSerializer.Serialize(new[] {
                new { displayName = "Visual Studio", installationVersion = "18.0", installationPath = ideRoot, isComplete = true },
                new { displayName = "Build Tools", installationVersion = "18.0", installationPath = folder, isComplete = true } });
            Require(WorkspaceTools.ParseVisualStudio(instances).Single().Name.Contains("18.0"),
                "Visual Studio instances include versions but exclude installations without an IDE");

            string path = Path.Combine(workspace, "Example.csproj");
            string text = "<Project>\r\n  <!-- preserved \u00e4 -->\n</Project>\r\n";
            File.WriteAllText(path, text, Encoding.Unicode);
            byte[] original = File.ReadAllBytes(path);
            using WorkspaceEditor editor = new(path);
            editor.Show(host);
            await WaitUntilAsync(() => editor.MainMenuStrip!.Items.OfType<ToolStripMenuItem>().Single()
                .DropDownItems[0].Enabled, "Internal editor loads the selected project");
            D2DTextEditor control = Descendants(editor).OfType<D2DTextEditor>().Single();
            await editor.SaveFileAsync();
            Require(File.ReadAllBytes(path).SequenceEqual(original), "Internal editor preserves UTF-16 BOM and mixed newline bytes");
            control.Text = control.Text.Replace("preserved", "edited", StringComparison.Ordinal);
            await editor.SaveFileAsync();
            Require(File.ReadAllText(path, Encoding.Unicode).Contains("edited") && !control.IsDocumentModified,
                "Internal editor saves edits and clears the document dirty state");
            File.AppendAllText(path, "external change", Encoding.Unicode);
            try
            {
                await editor.SaveFileAsync();
                throw new InvalidOperationException("External editor changes must be protected");
            }
            catch (IOException)
            {
                Require(File.ReadAllText(path, Encoding.Unicode).Contains("external change"), "External changes survive rejected editor save");
            }
            editor.Close();
            Console.WriteLine("PASS: dedicated SDK pins, configuration conflicts, IDE discovery/arguments and fidelity-preserving internal editor");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static async Task CheckWorkspaceActionsAsync(MainForm main, TreeView tree, ToolStrip toolbar)
    {
        string path = Path.Combine(Path.GetTempPath(), "DotnetUp.SmokeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            string projectPath = Path.Combine(path, "Test.csproj");
            File.WriteAllText(projectPath, "<Project />");
            WorkspaceEntry entry = new(projectPath, WorkspaceKind.Project, null);
            MethodInfo create = typeof(MainForm).GetMethod("CreateWorkspaceNode", PrivateInstance)!;
            TreeNode node = (TreeNode)create.Invoke(main, [entry])!;
            tree.Nodes[0].Nodes.Add(node);
            tree.SelectedNode = node;
            await Task.Delay(50);
            Require(node.Nodes.Count == 0, "Project nodes never expose source files");
            Require(toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Open in internal editor").Enabled
                && toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Open project in VS Code").Enabled
                && !toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Visit GitHub website").Enabled,
                "Project-only actions enable without requiring GitHub, while GitHub actions stay disabled");
            RepositoryInfo repo = new("Test", path, "https://github.com/example/test.git", null);
            TreeNode github = (TreeNode)create.Invoke(main, [entry with { Repository = repo }])!;
            tree.Nodes[0].Nodes.Add(github);
            tree.SelectedNode = github;
            await Task.Delay(50);
            Require(toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Visit GitHub website").Enabled,
                "Workspace nodes inherit their GitHub repository actions");
            DataGridView grid = Descendants(main).OfType<DataGridView>().Single();
            grid.DataSource = new List<GitBranch> {
                new("main", false, true, "", "abc", "Current", 0, "refs/heads/main", path),
                new("feature", false, false, "", "def", "Feature", 0, "refs/heads/feature", path) };
            grid.ClearSelection();
            grid.Rows[1].Selected = true;
            Require(toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Switch to selected branch").Enabled
                && toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Start Copilot on selected branch").Enabled
                && grid.ContextMenuStrip!.Items.Cast<ToolStripItem>().Where(item => item.Text!.Contains("selected branch")).All(item => item.Enabled),
                "A selected non-current branch enables switch and Copilot commands");
            grid.Rows[0].Selected = true;
            Require(!toolbar.Items.Cast<ToolStripItem>().Single(item => item.Text == "Start Copilot on selected branch").Enabled,
                "Multiple branches cannot start an ambiguous Copilot session");
            await CheckBranchUiWorkflowAsync(main, repo, grid, toolbar);
            tree.SelectedNode = tree.Nodes[0];
            node.Remove();
            github.Remove();
        }
        finally
        {
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(path, recursive: true);
        }
    }

    private static async Task CheckBranchUiWorkflowAsync(MainForm main, RepositoryInfo repo, DataGridView grid, ToolStrip toolbar)
    {
        HostedTerminal terminal = (HostedTerminal)typeof(MainForm).GetField("_terminal", PrivateInstance)!.GetValue(main)!;
        D2DConsole console = Descendants(main).OfType<D2DConsole>().Single();
        ToolStripButton show = toolbar.Items.OfType<ToolStripButton>().Single(item => item.Text == "Show branches");
        ToolStripButton copilot = toolbar.Items.OfType<ToolStripButton>().Single(item => item.Text == "Start Copilot on selected branch");
        string git = HostedTerminal.FindExecutable("git.exe")!;
        string? originalPath = Environment.GetEnvironmentVariable("PATH");
        string worktree = repo.Path + "-worktree";
        async Task Git(params string[] args)
        {
            CommandResult result = await terminal.RunAsync(new ShellCommand(git, ["-C", repo.Path, .. args]), captureJson: true)
                .WaitAsync(TimeSpan.FromSeconds(30));
            Require(result.Succeeded, $"UI Git fixture setup: {result.LastError}");
        }
        bool WorkflowFinished() => !(bool)typeof(MainForm).GetField("_workflowRunning", PrivateInstance)!.GetValue(main)!;
        void SelectBranch(string name)
        {
            grid.ClearSelection();
            grid.Rows.Cast<DataGridViewRow>().Single(row => row.DataBoundItem is GitBranch branch && branch.Name == name).Selected = true;
        }

        try
        {
            await Git("init", "--initial-branch=main");
            await Git("config", "core.hooksPath", Path.Combine(repo.Path, ".git", "empty-hooks"));
            await Git("add", "Test.csproj");
            await Git("-c", "user.name=Smoke Test", "-c", "user.email=smoke@example.invalid",
                "-c", "commit.gpgsign=false", "commit", "-m", "Project");
            await Git("branch", "feature");
            string fake = Path.Combine(repo.Path, ".git", "fake-copilot");
            Directory.CreateDirectory(fake);
            string resultPath = Path.Combine(fake, "result.json");
            File.WriteAllText(Path.Combine(fake, "copilot.cmd"),
                $"@echo off\r\n\"{Environment.ProcessPath}\" --probe interactive \"{resultPath}\"\r\nexit /b %errorlevel%\r\n");
            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, fake, Path.GetDirectoryName(git), Environment.SystemDirectory));
            show.PerformClick();
            await WaitUntilAsync(() => WorkflowFinished() && grid.Rows.Cast<DataGridViewRow>().Any(row => row.DataBoundItem is GitBranch { Name: "feature" }),
                "Show branches populates the real UI from Git");
            SelectBranch("feature");
            copilot.PerformClick();
            await WaitUntilAsync(() => ConsoleOutput(console).Contains("interactive-ready"), "Toolbar Copilot action starts an interactive client");
            await console.SendInputAsync("ui-copilot-session\r");
            await WaitUntilAsync(() => WorkflowFinished() && File.Exists(resultPath), "Toolbar-launched client completes");
            using (JsonDocument result = JsonDocument.Parse(File.ReadAllText(resultPath)))
            {
                Require(result.RootElement.GetProperty("Directory").GetString() == repo.Path
                    && result.RootElement.GetProperty("Branch").GetString() == "ref: refs/heads/feature",
                    "The actual UI command switches to the selected branch before starting Copilot in its repository");
            }

            File.Delete(resultPath);
            await Git("worktree", "add", worktree, "main");
            SelectBranch("main");
            copilot.PerformClick();
            await WaitUntilAsync(WorkflowFinished, "Failed branch switch completes without launching Copilot");
            Require(!File.Exists(resultPath) && File.ReadAllText(Path.Combine(repo.Path, ".git", "HEAD")).Trim() == "ref: refs/heads/feature",
                "A branch checked out in another worktree prevents Copilot launch without losing the current branch");
            StatusStrip status = Descendants(main).OfType<StatusStrip>().Single();
            Require(status.Items[2].ForeColor == Color.IndianRed, "The failed Git command remains visible as an error in the status strip");
            Console.WriteLine("PASS: real toolbar branch-to-Copilot workflow and switch-failure launch prevention");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            if (terminal.IsBusy)
            {
                await terminal.InterruptAsync();
                await terminal.RestartAsync(terminal.Shell);
            }
            if (Directory.Exists(worktree))
            {
                await Git("worktree", "remove", worktree);
            }
            await terminal.RunTextAsync(terminal.Shell == ShellKind.PowerShell
                ? $"Set-Location -LiteralPath {ShellCommand.Quote(Path.GetTempPath(), terminal.Shell)}"
                : $"cd /d {ShellCommand.Quote(Path.GetTempPath(), terminal.Shell)}");
        }
    }
}
