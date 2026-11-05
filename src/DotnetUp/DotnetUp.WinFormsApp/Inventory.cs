using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotnetUp.WinFormsApp;

public sealed record DetailRow(string Name, string Value, string Kind, string Location, string Source)
{
    public string Information { get; init; } = "";
    [Browsable(false)] public string? Channel { get; init; }
    [Browsable(false)] public string? Component { get; init; }
    [Browsable(false)] public string? Architecture { get; init; }
    public override string ToString() => $"{Name}: {Value} - {Location}";
}

public sealed record ManagedInventory(IReadOnlyList<DetailRow> Specifications, IReadOnlyList<DetailRow> Installations)
{
    public static ManagedInventory Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("installSpecs", out JsonElement specs) || specs.ValueKind != JsonValueKind.Array
            || !root.TryGetProperty("installations", out JsonElement installs) || installs.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("dotnetup returned an unsupported inventory: installSpecs and installations arrays are required.");
        }

        return new ManagedInventory(
            specs.EnumerateArray().Select(item => new DetailRow(
                Text(item, "component"), Text(item, "versionOrChannel"), "Tracked requirement",
                Text(item, "installRoot"), Text(item, "source"))
            {
                Channel = Text(item, "versionOrChannel"),
                Component = Text(item, "component"),
                Architecture = Text(item, "architecture"),
                Information = Text(item, "globalJsonPath")
            }).ToArray(),
            installs.EnumerateArray().Select(item => new DetailRow(
                Text(item, "component"), Text(item, "version"), "Managed installation",
                Text(item, "installRoot"), Text(item, "architecture"))
            {
                Component = Text(item, "component"),
                Architecture = Text(item, "architecture"),
                Information = item.TryGetProperty("isValid", out JsonElement validity) ? validity.ValueKind switch
                {
                    JsonValueKind.True => "Verified",
                    JsonValueKind.False => $"Invalid: {Text(item, "validationFailure")}",
                    _ => "Not verified"
                } : "Not verified"
            }).ToArray());
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) ? value.ToString() : "";
}

internal static class SystemInventory
{
    public static string DefaultInstallRoot => Path.Combine(
        Environment.GetEnvironmentVariable("DOTNET_DOTNETUP_DATA_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dotnetup"), "dotnet");

    public static string? FindDotnetUp()
    {
        string? executable = HostedTerminal.FindExecutable("dotnetup.exe");
        string standard = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnetup", "dotnetup.exe");
        return executable ?? (File.Exists(standard) ? standard : null);
    }

    public static IReadOnlyList<DetailRow> GetMachineSdks()
    {
        string[] roots =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"),
            Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? "",
            Environment.GetEnvironmentVariable("DOTNET_ROOT_X64") ?? "",
            Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64") ?? ""
        ];
        return roots.Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(root => Directory.Exists(Path.Combine(root, "sdk")))
            .SelectMany(root => Directory.EnumerateDirectories(Path.Combine(root, "sdk"))
                .Select(sdk => new DetailRow(".NET SDK", Path.GetFileName(sdk), "External installation", root, "Not managed by dotnetup")))
            .ToArray();
    }

    public static IReadOnlyList<DetailRow> GetGac()
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] roots = [Path.Combine(windows, "Microsoft.NET", "assembly"), Path.Combine(windows, "assembly")];
        List<DetailRow> rows = [];
        foreach (string root in roots.Where(Directory.Exists))
        {
            foreach (string path in Directory.EnumerateFiles(root, "*.dll", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                try
                {
                    AssemblyName name = AssemblyName.GetAssemblyName(path);
                    rows.Add(new DetailRow(name.Name ?? Path.GetFileName(path), name.Version?.ToString() ?? "",
                        ".NET Framework GAC (read-only)", path, "Windows"));
                }
                catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
                {
                    rows.Add(new DetailRow(Path.GetFileName(path), ex.Message, "Unreadable GAC entry", path, "Windows"));
                }
            }
        }

        return rows;
    }
}

public sealed record RepositoryInfo(string Name, string Path, string Remote, string? GlobalJson)
{
    public IReadOnlyList<DetailRow> GetDetails()
    {
        List<DetailRow> rows =
        [
            new("Repository", Name, "GitHub repository", Path, Remote),
            new("GitHub", Remote, "Remote", Path, "Git config")
        ];
        if (GlobalJson is not null)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GlobalJson), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            rows.Add(new DetailRow("global.json", GlobalJson, "SDK requirement", Path, "Repository"));
            if (document.RootElement.TryGetProperty("sdk", out JsonElement sdk))
            {
                rows.AddRange(sdk.EnumerateObject().Select(property =>
                    new DetailRow($"sdk.{property.Name}", property.Value.ToString(), "SDK requirement", GlobalJson, "Repository")));
            }
        }
        else
        {
            rows.Add(new DetailRow("global.json", "No applicable global.json found", "SDK requirement", Path, "Repository"));
        }

        return rows;
    }
}

public sealed record RepositoryScan(IReadOnlyList<RepositoryInfo> Repositories, int UnreadableFolders);

internal static partial class RepositoryScanner
{
    private static readonly HashSet<string> ExcludedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".nuget", "node_modules", "bin", "obj", "$Recycle.Bin", "System Volume Information"
    };

    public static RepositoryScan Scan(string root, IProgress<string>? progress, CancellationToken cancellation, int maxDepth = 0)
    {
        if (maxDepth is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Use 0 (unlimited) through 100 folder levels.");
        }

        List<RepositoryInfo> repositories = [];
        Stack<(string Folder, int Depth)> pending = new();
        pending.Push((root, 0));
        int unreadable = 0;
        int visited = 0;
        while (pending.TryPop(out var entry))
        {
            (string folder, int depth) = entry;
            cancellation.ThrowIfCancellationRequested();
            if (++visited % 200 == 0)
            {
                progress?.Report($"Scanning {folder} ({repositories.Count} GitHub repositories)...");
            }

            try
            {
                string? config = GetConfigPath(folder);
                if (config is not null && File.Exists(config))
                {
                    Match remote = GitHubRemote().Match(File.ReadAllText(config));
                    if (remote.Success)
                    {
                        repositories.Add(new RepositoryInfo(new DirectoryInfo(folder).Name,
                            folder, remote.Groups["url"].Value, FindGlobalJson(folder)));
                    }
                }

                if (maxDepth != 0 && depth >= maxDepth)
                {
                    continue;
                }

                foreach (string child in Directory.EnumerateDirectories(folder))
                {
                    if (!ExcludedFolders.Contains(Path.GetFileName(child))
                        && (File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push((child, depth + 1));
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                unreadable++;
            }
        }

        return new RepositoryScan(repositories.OrderBy(repo => repo.Path, StringComparer.OrdinalIgnoreCase).ToArray(), unreadable);
    }

    private static string? GetConfigPath(string folder)
    {
        string git = Path.Combine(folder, ".git");
        if (Directory.Exists(git))
        {
            return Path.Combine(git, "config");
        }

        if (File.Exists(git))
        {
            string line = File.ReadAllText(git).Trim();
            if (line.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
            {
                string directory = Path.GetFullPath(line["gitdir:".Length..].Trim(), folder);
                string commonDir = Path.Combine(directory, "commondir");
                if (File.Exists(commonDir))
                {
                    directory = Path.GetFullPath(File.ReadAllText(commonDir).Trim(), directory);
                }

                return Path.Combine(directory, "config");
            }
        }

        return null;
    }

    public static string? FindGlobalJson(string folder)
    {
        for (DirectoryInfo? directory = new(folder); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "global.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^\s*url\s*=\s*(?<url>(?:https?://github\.com/|git@github\.com:|ssh://git@github\.com/)[^\s]+)\s*$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GitHubRemote();
}
