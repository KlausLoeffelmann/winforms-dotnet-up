using System.Diagnostics;
using System.Text.Json;

namespace DotnetUp.WinFormsApp;

internal sealed record VisualStudioInstance(string Name, string Executable);

internal static class WorkspaceTools
{
    public static string? FindVsWhere()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        return File.Exists(path) ? path : HostedTerminal.FindExecutable("vswhere.exe");
    }

    public static IReadOnlyList<VisualStudioInstance> ParseVisualStudio(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Where(item => !item.TryGetProperty("isComplete", out JsonElement complete) || complete.GetBoolean())
            .Where(item => !item.TryGetProperty("isLaunchable", out JsonElement launchable) || launchable.GetBoolean())
            .Select(item => new VisualStudioInstance(
                item.GetProperty("displayName").GetString() + " (" + item.GetProperty("installationVersion").GetString() + ")",
                Path.Combine(item.GetProperty("installationPath").GetString()!, "Common7", "IDE", "devenv.exe")))
            .Where(instance => File.Exists(instance.Executable))
            .OrderByDescending(instance => instance.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string? FindVsCode()
    {
        string[] candidates = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe")
        ];
        return candidates.FirstOrDefault(File.Exists) ?? HostedTerminal.FindExecutable("Code.exe")
            ?? HostedTerminal.FindExecutable("code.cmd");
    }

    public static Uri RepositoryWebsite(RepositoryInfo repo)
    {
        string remote = repo.Remote.Trim();
        if (remote.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            remote = "https://github.com/" + remote["git@github.com:".Length..];
        }
        else if (remote.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
        {
            remote = "https://github.com/" + remote["ssh://git@github.com/".Length..];
        }

        if (!Uri.TryCreate(remote, UriKind.Absolute, out Uri? uri) || uri.Host != "github.com"
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("This is not a supported github.com repository remote.");
        }

        string path = uri.AbsolutePath.Trim('/');
        if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^4];
        }

        if (path.Split('/').Length != 2 || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("The remote must identify a GitHub owner/repository.");
        }

        return new Uri("https://github.com/" + path);
    }

    public static ProcessStartInfo LaunchInfo(string executable, string target, string workingDirectory)
    {
        ProcessStartInfo start = new(executable) { WorkingDirectory = workingDirectory, UseShellExecute = false };
        start.ArgumentList.Add(target);
        return start;
    }
}
