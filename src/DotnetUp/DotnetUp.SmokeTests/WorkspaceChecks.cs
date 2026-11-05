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
            tree.SelectedNode = tree.Nodes[0];
            node.Remove();
            github.Remove();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
