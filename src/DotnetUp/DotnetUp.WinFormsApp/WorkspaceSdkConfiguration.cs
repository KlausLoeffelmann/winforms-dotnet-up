using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DotnetUp.WinFormsApp;

internal sealed partial record WorkspaceSdkConfiguration(string Path, byte[]? Original, byte[] Updated)
{
    public static WorkspaceSdkConfiguration Prepare(SdkTaskOptions options)
    {
        string directory = options.RepositoryPath ?? throw new ArgumentException("Select a solution or project.");
        string path = System.IO.Path.Combine(directory, "global.json");
        string? inherited = RepositoryScanner.FindGlobalJson(directory);
        byte[]? original = File.Exists(path) ? File.ReadAllBytes(path) : null;
        Encoding encoding = new UTF8Encoding(false, true);
        string text = "";
        JsonObject document = new();
        if (inherited is not null)
        {
            using StreamReader reader = new(inherited, encoding, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
            if (original is not null)
            {
                encoding = reader.CurrentEncoding;
            }

            document = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
            { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject
                ?? throw new ArgumentException("global.json must contain a JSON object.");
        }

        if (document["sdk"] is not null and not JsonObject)
        {
            throw new ArgumentException("global.json sdk must contain a JSON object.");
        }

        JsonObject sdk = document["sdk"] as JsonObject ?? new JsonObject();
        string version = options.UseGlobalJson ? sdk["version"]?.GetValue<string>() ?? "" : options.Channel;
        if (!ExactVersion().IsMatch(version))
        {
            throw new ArgumentException("To configure local global.json, enter an exact SDK version (for example 10.0.103), or use an existing global.json requirement.");
        }

        sdk["version"] = version;
        JsonArray paths = new(options.InstallPath ?? System.IO.Path.Combine(directory, ".dotnet"));
        if (sdk["paths"] is not null and not JsonArray)
        {
            throw new ArgumentException("global.json sdk.paths must contain an array.");
        }
        if (sdk["paths"] is JsonArray existing)
        {
            foreach (JsonNode? item in existing)
            {
                string entry = item?.GetValue<string>() ?? throw new ArgumentException("sdk.paths must contain nonempty strings.");
                if (string.IsNullOrWhiteSpace(entry) || entry == "$host$")
                {
                    throw new ArgumentException("The current dotnetup preview does not support empty or $host$ sdk.paths entries.");
                }

                string resolved = System.IO.Path.GetFullPath(entry, System.IO.Path.GetDirectoryName(inherited!)!);
                if (!paths.Any(node => string.Equals(node?.GetValue<string>(), resolved, StringComparison.OrdinalIgnoreCase)))
                {
                    paths.Add(resolved);
                }
            }
        }

        sdk["paths"] = paths;
        if (document["sdk"] is null)
        {
            document["sdk"] = sdk;
        }

        string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string updated = document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings(newline) + newline;
        return new(path, original, encoding.GetPreamble().Concat(encoding.GetBytes(updated)).ToArray());
    }

    public void Save()
    {
        byte[]? current = File.Exists(Path) ? File.ReadAllBytes(Path) : null;
        if (current is null != (Original is null) || (current is not null && !current.SequenceEqual(Original!)))
        {
            throw new IOException("The SDK was installed, but global.json changed meanwhile. It was not overwritten; reopen the installation dialog.");
        }

        string temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, Updated);
            if (Original is null)
            {
                File.Move(temporary, Path);
            }
            else
            {
                File.Replace(temporary, Path, null);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex ExactVersion();
}
