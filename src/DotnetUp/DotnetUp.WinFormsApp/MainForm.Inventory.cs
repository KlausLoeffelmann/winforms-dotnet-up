namespace DotnetUp.WinFormsApp;

public partial class MainForm
{
    private sealed record NavigationItem(string Kind, string? Path = null, object? Value = null);
    private readonly CancellationTokenSource _inventoryLifetime = new();
    private CancellationTokenSource? _scanCancellation;
    private IReadOnlyList<DetailRow> _sdkRows = [];
    private ManagedInventory _managed = new([], []);
    private TreeNode? _sdkNode;
    private bool _refreshing;

    private void InitializeNavigation()
    {
        InitializeNavigationIcons();
        navigation.BeforeExpand += Navigation_BeforeExpand;
        TreeNode root = new("System") { Tag = new NavigationItem("system") };
        root.Nodes.Add(new TreeNode("GAC") { Tag = new NavigationItem("gac") });
        _sdkNode = new TreeNode(".NET SDKs") { Tag = new NavigationItem("sdks") };
        root.Nodes.Add(_sdkNode);
        foreach (string drive in new[] { @"C:\", @"N:\" })
        {
            TreeNode node = new($"Drive {drive[0]}") { Tag = new NavigationItem("drive", drive) };
            node.Nodes.Add("Expand to discover solutions and projects");
            root.Nodes.Add(node);
        }

        navigation.Nodes.Add(root);
        root.Expand();
        navigation.SelectedNode = root;
    }

    private async Task RefreshInventoryAsync(bool verify = false)
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            SetStatus("Reading SDK inventory...");
            IReadOnlyList<DetailRow> machine = await Task.Run(SystemInventory.GetMachineSdks, _inventoryLifetime.Token);
            string? executable = SystemInventory.FindDotnetUp();
            if (executable is not null && _terminal is not null)
            {
                List<string> args = ["list", "--format", "json"];
                if (!verify)
                {
                    args.Add("--no-verify");
                }

                CommandResult result = await _terminal.RunAsync(new ShellCommand(executable, args), captureJson: true);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.LastError ?? $"Inventory failed (exit {result.ExitCode}).");
                }

                _managed = ManagedInventory.Parse(result.Output);
            }

            machine = machine.Where(row => !_managed.Installations.Any(managed =>
                string.Equals(managed.Location.TrimEnd('\\'), row.Location.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))).ToArray();
            _sdkRows = machine.Concat(_managed.Installations).Concat(_managed.Specifications).ToArray();
            if (_sdkNode is not null)
            {
                string? previousVersion = navigation.SelectedNode?.Parent == _sdkNode ? navigation.SelectedNode.Text : null;
                _sdkNode.Nodes.Clear();
                foreach (IGrouping<string, DetailRow> group in _sdkRows.GroupBy(row => row.Value)
                    .OrderByDescending(group => group.Key, StringComparer.OrdinalIgnoreCase))
                {
                    _sdkNode.Nodes.Add(new TreeNode(group.Key)
                    {
                        Tag = new NavigationItem("sdk", Value: group.ToArray())
                    });
                }

                if (previousVersion is not null)
                {
                    navigation.SelectedNode = _sdkNode.Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Text == previousVersion) ?? _sdkNode;
                }
            }

            await ShowNodeDetailsAsync(navigation.SelectedNode);
            SetStatus($"Inventory: {_managed.Specifications.Count} tracked requirements, {_managed.Installations.Count} managed components, {machine.Count} external SDKs.");
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task ShowNodeDetailsAsync(TreeNode? node)
    {
        if (node?.Tag is not NavigationItem item)
        {
            details.DataSource = null;
            return;
        }

        IReadOnlyList<DetailRow> rows = item.Kind switch
        {
            "sdks" => _sdkRows,
            "sdk" when item.Value is DetailRow[] values => values,
            "repository" when item.Value is RepositoryInfo repo => repo.GetDetails(),
            "workspace" when item.Value is WorkspaceEntry workspace => workspace.GetDetails(),
            "gac" => await Task.Run(SystemInventory.GetGac, _inventoryLifetime.Token),
            "folder" => node.Nodes.Cast<TreeNode>()
                .Where(child => child.Tag is NavigationItem { Value: RepositoryInfo })
                .Select(child => ((NavigationItem)child.Tag!).Value).Cast<RepositoryInfo>()
                .Select(repo => new DetailRow(repo.Name, repo.Remote, "GitHub repository", repo.Path, repo.GlobalJson ?? "No global.json")).ToArray(),
            "drive" => [new DetailRow("Drive", item.Path ?? "", "Repository discovery", item.Path ?? "",
                Directory.Exists(item.Path) ? "Expand to scan; Stop cancels discovery" : "Drive is not available")],
            _ =>
            [
                new DetailRow("dotnetup", SystemInventory.FindDotnetUp() ?? "Not installed - use Tools / Setup wizard",
                    "User-level SDK manager", "", "Public preview"),
                new DetailRow("SDKs", _sdkRows.Count.ToString(), "Inventory", "", "Select .NET SDKs"),
                new DetailRow("Solutions / projects", "Expand Drive C or Drive N", "Discovery", "", "Visual Studio / .NET / ASP.NET"),
                new DetailRow("GAC", ".NET Framework assemblies (read-only)", "System", "", "Separate from modern .NET SDKs")
            ]
        };
        if (navigation.SelectedNode == node)
        {
            details.DataSource = rows.ToList();
            details.ClearSelection();
            UpdateSelectionStatus();
        }
    }

    private async void Navigation_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        TreeNode? node = e.Node;
        if (node?.Tag is NavigationItem { Value: WorkspaceEntry { Kind: WorkspaceKind.Solution } solution })
        {
            if (node.Nodes.Count == 1 && node.Nodes[0].Tag is null)
            {
                try
                {
                    IReadOnlyList<WorkspaceEntry> projects = await Task.Run(() => WorkspaceScanner.ReadSolutionProjects(solution), _inventoryLifetime.Token);
                    node.Nodes.Clear();
                    foreach (WorkspaceEntry project in projects)
                    {
                        node.Nodes.Add(CreateWorkspaceNode(project));
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Unable to read solution: {ex.Message}");
                }
            }

            return;
        }

        if (node?.Tag is not NavigationItem { Kind: "drive", Path: string path }
            || node.Nodes.Count != 1 || node.Nodes[0].Tag is not null)
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            node.Nodes.Clear();
            node.Nodes.Add("Drive unavailable");
            ShowError($"Drive {path} is not available.");
            return;
        }

        if (_scanCancellation is not null)
        {
            e.Cancel = true;
            ShowError("A workspace scan is already running. Wait or stop it first.");
            return;
        }

        _scanCancellation = CancellationTokenSource.CreateLinkedTokenSource(_inventoryLifetime.Token);
        try
        {
            node.Nodes[0].Text = "Scanning...";
            Progress<string> progress = new(SetStatus);
            int maxDepth = _preferences.RepositoryScanDepth;
            WorkspaceScan scan = await Task.Run(() => WorkspaceScanner.Scan(path, progress, _scanCancellation.Token, maxDepth));
            node.Nodes.Clear();
            PopulateWorkspaceTree(node, path, scan);

            if (node.Nodes.Count == 0)
            {
                node.Nodes.Add(new TreeNode("No solutions, projects or ASP.NET applications found") { Tag = new NavigationItem("empty") });
            }

            SetStatus($"Found {scan.Entries.Count} solutions/projects/applications on {path}; {scan.UnreadableFolders} inaccessible folders.");
        }
        catch (OperationCanceledException)
        {
            node.Nodes.Clear();
            node.Nodes.Add("Scan cancelled - collapse and expand to retry");
            SetStatus("Workspace discovery cancelled.");
        }
        catch (Exception ex)
        {
            node.Nodes.Clear();
            node.Nodes.Add("Discovery failed - collapse and expand to retry");
            ShowError(ex.Message);
        }
        finally
        {
            _scanCancellation.Dispose();
            _scanCancellation = null;
        }
    }

    private TreeNode CreateWorkspaceNode(WorkspaceEntry entry)
    {
        string icon = entry.Kind switch { WorkspaceKind.Solution => "solution", WorkspaceKind.Project => "project", _ => "web" };
        if (entry.Kind == WorkspaceKind.Solution && File.Exists(entry.Path))
        {
            string key = Path.GetExtension(entry.Path).ToLowerInvariant();
            if (!navigation.ImageList!.Images.ContainsKey(key))
            {
                using Icon? associated = Icon.ExtractAssociatedIcon(entry.Path);
                if (associated is not null)
                {
                    navigation.ImageList.Images.Add(key, associated);
                }
            }

            if (navigation.ImageList.Images.ContainsKey(key))
            {
                icon = key;
            }
        }

        TreeNode node = new(entry.Name) { Tag = new NavigationItem("workspace", entry.Path, entry), ImageKey = icon, SelectedImageKey = icon };
        if (entry.Kind == WorkspaceKind.Solution)
        {
            node.Nodes.Add("Expand to show projects");
        }

        return node;
    }

    private void PopulateWorkspaceTree(TreeNode drive, string root, WorkspaceScan scan)
    {
        Dictionary<string, TreeNode> folders = new(StringComparer.OrdinalIgnoreCase) { [Path.GetFullPath(root).TrimEnd('\\')] = drive };
        TreeNode EnsureFolder(string path)
        {
            string key = Path.GetFullPath(path).TrimEnd('\\');
            if (folders.TryGetValue(key, out TreeNode? existing))
            {
                return existing;
            }

            TreeNode parent = EnsureFolder(Path.GetDirectoryName(key)!);
            RepositoryInfo? repo = scan.Repositories.FirstOrDefault(repo => repo.Path.Equals(key, StringComparison.OrdinalIgnoreCase));
            string icon = repo is null ? "folder" : "github";
            TreeNode folder = new(Path.GetFileName(key))
            {
                Tag = repo is null ? new NavigationItem("folder", key) : new NavigationItem("repository", key, repo),
                ImageKey = icon,
                SelectedImageKey = icon
            };
            folders.Add(key, folder);
            parent.Nodes.Add(folder);
            return folder;
        }

        foreach (WorkspaceEntry entry in scan.Entries)
        {
            EnsureFolder(entry.DirectoryPath).Nodes.Add(CreateWorkspaceNode(entry));
        }
    }
    private async void Tree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        try
        {
            await ShowNodeDetailsAsync(e.Node);
        }
        catch (OperationCanceledException) when (_inventoryLifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void Refresh_Click(object? sender, EventArgs e)
    {
        await RunWorkflowAsync(() => RefreshInventoryAsync());
    }

    private void Navigation_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            navigation.SelectedNode = e.Node;
        }
    }

    private void Details_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && !details.Rows[e.RowIndex].Selected)
        {
            details.ClearSelection();
            details.Rows[e.RowIndex].Selected = true;
        }
    }
}
