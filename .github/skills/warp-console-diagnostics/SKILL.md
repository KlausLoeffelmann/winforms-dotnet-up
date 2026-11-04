---
name: warp-console-diagnostics
description: >-
  Embeds a WARP in-process console output/command pane, formats incremental byte
  dumps and creates disambiguated export paths. Handles channel/style overloads,
  partial final dump rows and filename generation strategies.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.Specialized\\Console; Tools; IO"
---

# Console, dumps and export filenames

## Critical constraints

- ConsoleControl is a RichTextBox-based surface, not a promise of terminal/PTY
  emulation or automatic interactive CLI integration.
- Await write/run/style Tasks; respect UI-thread ownership and cancellation/lifetime
  at the application boundary. Read `winforms-async-apis` for marshaling.
- Incremental byte dumps retain a partial row: emit GetRemaining at completion.
- Filename Extension must be non-null. Generated filenames are not a transactional
  reservation against another process creating the same file.
- Static UI placement belongs in Designer-safe declarations; read
  `winforms-designer-code` and `winforms-high-dpi-fluent-layout` when embedding.

## ConsoleControl

Package/namespace `WarpToolkit.WinForms.Specialized`.

```csharp
Task WriteAsync(string text, Color? textColor = null, CustomFontStyle? style = null, FontSize? size = null, bool keepStyles = false);
Task WriteLineAsync(string? text = null, Color? textColor = null, CustomFontStyle? style = null, FontSize? size = null, bool keepStyles = false);
Task WriteLineAsync(string? text, ConsoleChannel channel);
Task WriteMessageAsync(string text, ConsoleMessageKind kind = ConsoleMessageKind.Standard, bool includeTimestamp = false);
Task SetStyleAsync(Color? textColor = null, CustomFontStyle? style = null, FontSize? size = null, bool keepSetting = false);
Task ResetStylesAsync();
Task ResetAsync();
Task RunCommandAsync(string command, string arguments);
Task RunCommandAsync(string command, string arguments, ConsoleChannel channel);
```

These are signature excerpts, not class implementations.
`ConsoleOut` and `ConsoleOutAsync` are TextWriter getters.

| Enum/helper | Values / contract |
|---|---|
| ConsoleChannel | Default=0, Trace, Debug, Information, Warning, Error, Git, Sql |
| ConsoleMessageKind | Standard, Command, Output, Information, Success, Warning, Error |
| CustomFontStyle | Normal=0, Bold=1, Italic=2, Underline=4, StrikeThrough=8; combinable |
| FontSize | Normal=0, Smaller, Small, Larger, Large |
| ConsoleChannelColors | Predefined colors; GetStdOut(channel, fallback), GetStdErr(channel) |

```csharp
await console.SetStyleAsync(textColor: Color.LightSkyBlue,
    style: CustomFontStyle.Bold, size: FontSize.Normal, keepSetting: true);
await console.WriteLineAsync("Running build…", ConsoleChannel.Information);
await console.RunCommandAsync("dotnet", "build", ConsoleChannel.Git);
```

Configure the Designer-owned console as a RichTextBox: Dock Fill, desired ReadOnly,
BorderStyle and WordWrap; choose a monospace font (e.g. Cascadia Mono) and host theme.
Channel colors are explicit WARP defaults, not proof that every custom color updates
automatically after a theme switch.

## HexAsciiDumper

Default constructor; writable BytesPerRow, DataFormat and ShowText.
`DataFormat`: Hex/Oct/Decimal; `ShowText`: None/ASCII/Unicode.
`TryGetString(byte[] newData, out string? result)` and string overload buffer rows.
`GetRemaining()` returns the residual string; `Flush()` is also available.

```csharp
HexAsciiDumper dumper = new();
dumper.BytesPerRow = 16;
dumper.DataFormat = DataFormat.Hex;
dumper.ShowText = ShowText.ASCII;
if (dumper.TryGetString(buffer, out string? rows) && rows is not null)
{
    await console.WriteAsync(rows);
}
await console.WriteAsync(dumper.GetRemaining());
```

Do not call Flush instead of emitting the residual output and claim the last bytes
were displayed; inspect its behavior when using it to reset a dump.

## FilenameDisambiguator

`GenerationStrategy`: None, DateBased, GuidBase, DateAmended.
Constructors retained by the API inventory:

- `()`;
- `(title, extension, requestPathForFile = false)`;
- `(title, basePath, extension, requestPathForFile = false)`;
- `(generationStrategy, extension, requestPathForFile = false)`;
- `(title, extension, generationStrategy, combineUniquePath = false)`;
- `(title, basePath, extension, generationStrategy, requestPathForFile = false)`.

Title/BasePath are nullable strings; Extension is non-null. Writable members:
GenerationStrategy, CombineUniquePath, DateFilenameAmendmentFormat,
MaxLengthFilename, MaxLengthFolder.
Getters: FilenameCandidate, FolderCandidate, FullFilename.
Methods: GetResultingFilenameParts returns `(folder, filename, extension)`,
GetResultingFoldername, TryDelete; static ExpandPath/ShrinkPath and
FromFilename(fullPathAndFilename) / FromFilename(basePath, filename).

```csharp
FilenameDisambiguator disambiguator = new(
    title: "ChatExport", basePath: chatFolder, extension: ".md",
    generationStrategy: GenerationStrategy.DateAmended);
string fullPath = disambiguator.FullFilename;
```

There is no verified TimestampSuffix strategy. Nonvisual dumper/path helpers have
no DPI requirements. Historical GitScanner/Chatty examples are examples, not
required filesystem locations in a consumer.

## Final checks

1. Async output/commands finish or report failure before disposing the control.
2. Final dump bytes and intended formatting are emitted.
3. Export path generation, write failures and filename/resource ownership are explicit.
