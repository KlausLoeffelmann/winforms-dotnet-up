using System.Runtime.InteropServices;

namespace DotnetUp.WinFormsApp;

internal static class SelectedSdkUpdates
{
    public static bool CanUpdate(DetailRow row) =>
        row.Component == "SDK" && row.Kind is "Managed installation" or "Tracked requirement"
        && Path.IsPathFullyQualified(row.Location)
        && (string.IsNullOrEmpty(row.Architecture)
            || row.Architecture.Equals(RuntimeInformation.ProcessArchitecture.ToString(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<ShellCommand> Plan(string executable, IReadOnlyList<DetailRow> selection)
    {
        if (selection.Count == 0 || selection.Any(row => !CanUpdate(row)))
        {
            throw new ArgumentException("Select only managed SDKs for this process architecture. External SDKs and runtimes cannot be updated here.");
        }

        return selection.Select(row =>
        {
            string channel = row.Channel ?? row.Value;
            if (Version.TryParse(channel.Split('-')[0], out Version? version) && version.Build >= 0)
            {
                channel = $"{version.Major}.{version.Minor}";
            }

            SdkChannels.Validate(channel);
            return (Root: row.Location, Channel: channel);
        })
            .DistinctBy(target => (target.Root.ToUpperInvariant(), target.Channel))
            .Select(target => new ShellCommand(executable,
                ["sdk", "install", target.Channel, "--install-path", target.Root, "--interactive", "false", "--no-progress"]))
            .ToArray();
    }
}
