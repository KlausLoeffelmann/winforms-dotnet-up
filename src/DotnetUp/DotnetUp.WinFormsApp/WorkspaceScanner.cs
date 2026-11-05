using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DotnetUp.WinFormsApp;

public enum WorkspaceKind { Solution, Project, WebApplication }

public sealed record WorkspaceEntry(string Path, WorkspaceKind Kind, RepositoryInfo? Repository)
{
    public string DirectoryPath => Kind == WorkspaceKind.WebApplication ? Path : System.IO.Path.GetDirectoryName(Path)!;
    public string Name => System.IO.Path.GetFileName(Path);

    public RepositoryInfo SdkContext() => new(Name, DirectoryPath, Repository?.Remote ?? "",
        RepositoryScanner.FindGlobalJson(DirectoryPath));

    public IReadOnlyList<DetailRow> GetDetails() =>
        new[] { new DetailRow(Name, Kind.ToString(), "Workspace", Path, Repository?.Remote ?? "Local workspace") }
            .Concat(SdkContext().GetDetails().Where(row => row.Name != "Repository" && row.Name != "GitHub")).ToArray();
}

public sealed record WorkspaceScan(IReadOnlyList<WorkspaceEntry> Entries,
    IReadOnlyList<RepositoryInfo> Repositories, int UnreadableFolders);

internal static partial class WorkspaceScanner
{
    private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".nuget", ".dotnet", "node_modules", "bin", "obj", "$Recycle.Bin", "System Volume Information"
    };

    public static WorkspaceScan Scan(string root, IProgress<string>? progress, CancellationToken cancellation, int maxDepth = 0)
    {
        if (maxDepth is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Use 0 (unlimited) through 100 folder levels.");
        }

        List<WorkspaceEntry> entries = [];
        List<RepositoryInfo> repositories = [];
        Stack<(string Folder, int Depth, RepositoryInfo? Repository)> pending = new();
        pending.Push((root, 0, null));
        int unreadable = 0;
        int visited = 0;
        while (pending.TryPop(out var current))
        {
            cancellation.ThrowIfCancellationRequested();
            if (++visited % 200 == 0)
            {
                progress?.Report($"Scanning {current.Folder} ({entries.Count} solutions/projects/applications)...");
            }

            try
            {
                RepositoryInfo? repository = current.Repository;
                try
                {
                    if (RepositoryScanner.ReadRepository(current.Folder) is { } ownRepository)
                    {
                        repository = ownRepository;
                        repositories.Add(ownRepository);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    unreadable++;
                }

                string[] files = Directory.GetFiles(current.Folder);
                foreach (string path in files)
                {
                    string extension = System.IO.Path.GetExtension(path);
                    if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
                    {
                        entries.Add(new WorkspaceEntry(path, WorkspaceKind.Solution, repository));
                    }
                    else if (IsProject(path))
                    {
                        entries.Add(new WorkspaceEntry(path, WorkspaceKind.Project, repository));
                    }
                }

                if (files.Any(path => System.IO.Path.GetFileName(path).Equals("web.config", StringComparison.OrdinalIgnoreCase)
                    || System.IO.Path.GetFileName(path).Equals("Global.asax", StringComparison.OrdinalIgnoreCase))
                    || (Directory.Exists(System.IO.Path.Combine(current.Folder, "wwwroot"))
                        && files.Any(path => System.IO.Path.GetFileName(path).Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))))
                {
                    entries.Add(new WorkspaceEntry(current.Folder, WorkspaceKind.WebApplication, repository));
                }

                if (maxDepth != 0 && current.Depth >= maxDepth)
                {
                    continue;
                }

                foreach (string child in Directory.EnumerateDirectories(current.Folder))
                {
                    if (!ExcludedFolders.Contains(System.IO.Path.GetFileName(child))
                        && (File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push((child, current.Depth + 1, repository));
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                unreadable++;
            }
        }

        return new WorkspaceScan(entries.OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray(),
            repositories.Where(repo => entries.Any(entry => IsWithin(entry.DirectoryPath, repo.Path))).ToArray(), unreadable);
    }

    public static bool IsWithin(string path, string folder) =>
        path.Equals(folder, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);

    public static bool IsProject(string path) => System.IO.Path.GetExtension(path).EndsWith("proj", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<WorkspaceEntry> ReadSolutionProjects(WorkspaceEntry solution)
    {
        string[] paths;
        if (System.IO.Path.GetExtension(solution.Path).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            using XmlReader reader = XmlReader.Create(solution.Path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            paths = XDocument.Load(reader).Descendants().Where(element => element.Name.LocalName == "Project")
                .Select(element => (string?)element.Attribute("Path")).OfType<string>().ToArray();
        }
        else
        {
            paths = SolutionProject().Matches(File.ReadAllText(solution.Path)).Select(match => match.Groups[1].Value).ToArray();
        }

        return paths.Where(IsProject).Select(path => new WorkspaceEntry(
            System.IO.Path.GetFullPath(path.Replace('/', '\\'), solution.DirectoryPath), WorkspaceKind.Project, solution.Repository))
            .DistinctBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [GeneratedRegex("^Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]*\",\\s*\"([^\"]+)\"", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SolutionProject();
}
