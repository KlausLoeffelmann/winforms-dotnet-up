namespace DotnetUp.WinFormsApp;

public sealed record SdkTaskOptions(
    bool Update,
    string Channel,
    string? InstallPath = null,
    string? RepositoryPath = null,
    bool UseGlobalJson = false,
    bool UpdateGlobalJson = false,
    bool Migrate = false,
    bool IncludeRuntimes = false,
    bool OverrideUpdateChannel = false,
    string Component = "sdk");

public static class DotnetUpCommands
{
    public const string Bootstrap = "irm https://aka.ms/dotnetup/get-dotnetup.ps1 | iex";

    public static IReadOnlyList<ShellCommand> Plan(string executable, SdkTaskOptions options)
    {
        SdkChannels.Validate(options.Channel);
        if (options.Component is not ("sdk" or "runtime" or "aspnetcore" or "windowsdesktop"))
        {
            throw new ArgumentException("Select a supported .NET component.");
        }

        if (options.UseGlobalJson && (options.RepositoryPath is null || options.Component != "sdk"))
        {
            throw new ArgumentException("global.json installation requires a selected repository and the SDK component.");
        }

        if (options.Component != "sdk" && options.Channel.Contains("xx", StringComparison.Ordinal))
        {
            throw new ArgumentException("Feature bands apply to SDKs, not runtimes. Use a runtime channel or version.");
        }

        if (options.UpdateGlobalJson && options.RepositoryPath is null)
        {
            throw new ArgumentException("Select a repository before changing global.json.");
        }

        if (!string.IsNullOrWhiteSpace(options.InstallPath) && !Path.IsPathFullyQualified(options.InstallPath))
        {
            throw new ArgumentException("The installation path must be absolute.");
        }

        List<ShellCommand> commands = [];
        if (!options.Update || options.OverrideUpdateChannel)
        {
            List<string> args = [options.Component == "sdk" ? "sdk" : "runtime", "install"];
            if (!options.UseGlobalJson)
            {
                args.Add(options.Component == "sdk" ? options.Channel : $"{options.Component}@{options.Channel}");
            }

            AddPath(args, options.InstallPath ?? (options.RepositoryPath is null ? SystemInventory.DefaultInstallRoot : null));
            if (options.Migrate)
            {
                args.Add("--migrate-from-system");
            }

            if (options.UpdateGlobalJson && options.Component == "sdk")
            {
                args.Add("--update-global-json");
            }

            args.AddRange(["--interactive", "false", "--no-progress"]);
            commands.Add(new ShellCommand(executable, args, options.RepositoryPath));
        }

        if (options.Update)
        {
            List<string> args = ["sdk", "update"];
            AddPath(args, options.InstallPath);
            if (options.IncludeRuntimes)
            {
                args.Add("--all");
            }

            if (options.UpdateGlobalJson)
            {
                args.Add("--update-global-json");
            }

            args.AddRange(["--interactive", "false", "--no-progress"]);
            commands.Add(new ShellCommand(executable, args, options.RepositoryPath));
        }

        return commands;
    }

    private static void AddPath(List<string> args, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            args.AddRange(["--install-path", path]);
        }
    }
}
