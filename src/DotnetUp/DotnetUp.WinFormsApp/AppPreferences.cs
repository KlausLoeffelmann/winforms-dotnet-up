using System.Text.RegularExpressions;

namespace DotnetUp.WinFormsApp;

public enum ShellKind
{
    PowerShell,
    CommandPrompt
}

public sealed class AppPreferences
{
    public string Channel { get; set; } = "lts";
    public ShellKind Shell { get; set; }
    public string UiFontFamily { get; set; } = "Segoe UI";
    public float UiFontSize { get; set; } = 10;
    public string ConsoleFontFamily { get; set; } = "Consolas";
    public float ConsoleFontSize { get; set; } = 11;
    public bool SetupCompleted { get; set; }
    public int RepositoryScanDepth { get; set; }

    public void Validate()
    {
        SdkChannels.Validate(Channel);
        if (RepositoryScanDepth is < 0 or > 100)
        {
            throw new ArgumentException("Repository scan depth must be between 0 (unlimited) and 100.");
        }

        if (!Enum.IsDefined(Shell) || string.IsNullOrWhiteSpace(UiFontFamily)
            || string.IsNullOrWhiteSpace(ConsoleFontFamily)
            || !float.IsFinite(UiFontSize) || UiFontSize is < 6 or > 36
            || !float.IsFinite(ConsoleFontSize) || ConsoleFontSize is < 6 or > 36)
        {
            throw new ArgumentException("Select valid fonts (6-36 pt) and a supported shell.");
        }
    }
}

public static partial class SdkChannels
{
    public static IReadOnlyList<string> Named { get; } = ["lts", "latest", "preview", "daily"];

    public static void Validate(string value)
    {
        if (!Named.Contains(value) && !VersionPattern().IsMatch(value))
        {
            throw new ArgumentException("Use lts, latest, preview, daily, a major/minor, feature band, or exact SDK version.");
        }
    }

    [GeneratedRegex(@"^\d+(?:\.\d+)?(?:\.(?:\d+|\dxx))?(?:-(?:preview|rc)\.\d+(?:\.\d+)*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
