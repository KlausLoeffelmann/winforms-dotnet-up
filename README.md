# winforms-dotnet-up

A Windows 11 / .NET 11 WinForms front end for the public-preview
[dotnetup](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnetup/getting-started)
toolchain manager, using WARP's `WarpDataGridView` and editable, ConPTY-backed
`D2DConsole`.

## Run

Install a .NET 11 SDK and run:

```powershell
dotnet run --project src\DotnetUp\DotnetUp.WinFormsApp
```

The first launch presents a three-page setup wizard. Nothing is downloaded or
changed until **Finish**. If dotnetup is missing, the hosted PowerShell session runs:

```powershell
irm https://aka.ms/dotnetup/get-dotnetup.ps1 | iex
```

Setup then verifies the executable, configures the selected environment mode,
optionally installs an initial SDK, and displays verification commands.
The conservative defaults are **lts**, **none** access mode, and **no PATH
changes**. Shell-profile configuration and machine-wide `everywhere` mode are
explicit choices; the latter warns about elevation and overriding Visual Studio
or other machine-wide SDKs. PowerShell-profile setup requires PowerShell 7.
Cancel postpones setup; reopen it from **Tools / Setup wizard**.

## Workflows

- **File / Install new Global SDK...** installs SDKs or core, ASP.NET Core, and
  Windows Desktop runtimes into a user-level managed root. Channels, major/minor
  versions, SDK feature bands, and exact versions can be entered. A blank global
  installation root explicitly selects dotnetup's default data directory, never
  the last repository's local installation.
- **File / Update existing SDKs...** updates tracked SDK requirements, optionally
  including runtimes and applicable `global.json` files. Exact versions remain
  pinned. Because update has no channel argument, the optional channel override
  explicitly installs that channel before updating. This creates a tracked
  requirement; it does not change the app's selected channel.
- **Channel** selects the app default (`lts`, `latest`, `preview`, `daily`).
  Changes in task dialogs are temporary and do not change this default.
- **File / Uninstall selected requirement...** removes the selected tracked
  specification, not an arbitrary installed version. Confirmation explains that
  garbage collection can remove other unreferenced components, and shared
  requirements can keep versions installed. External SDKs and the GAC are
  read-only.
- **Tools** includes installation verification, environment inspection,
  tool information, and self-update. Self-update is separate from SDK updates.

The left tree contains **System / GAC / .NET SDKs / Drive C / Drive N**. SDK nodes
show actual versions and tracked requirements. Expanding a drive asynchronously
discovers repositories with GitHub remotes, grouping them under their parent
folders. Discovery includes worktrees, skips reparse points and dependency/build
folders (`.git`, `.vs`, `.nuget`, `node_modules`, `bin`, `obj`), and reports
inaccessible folders. **Stop** cancels discovery; **Reset repository discovery**
allows another scan. Missing drives are reported without blocking startup.

Selecting a repository shows its remote and nearest applicable `global.json`.
The **Repository** menu and tree context menu install its required SDK, switch the
terminal to its directory, or open its folder/configuration. Changing
`global.json` is always opt-in. The grid context menu supports scoped updates and
uninstalling tracked requirements. Grid selection takes precedence over tree
selection in the status strip; it also shows the channel, latest status/error,
and local date/time with seconds.

## Hosted terminal and options

Every GUI dotnetup operation runs in the visible hosted shell, including JSON
inventory queries; no hidden CLI process modifies installations. Successful
mutations refresh the UI. Partial failures refresh inventory and preserve the
last error in the status strip. Inventory is read from `dotnetup list --format
json`; dotnetup's state files are never edited.

Type commands in the editable prompt. While a command is waiting for input, a
submitted line is forwarded to it. GUI commands are serialized and never inserted
into an active command. **Stop** sends Ctrl+C and stops the active shell if needed;
**Tools / Restart terminal** opens a fresh session afterward. The session retains
working-directory and environment changes between commands. In `none` access mode,
use `dotnetup dotnet -- <arguments>` (or the full dotnetup path shown under System).
Saved PATH changes apply to new external terminals, not already-running processes.
The app adds the discovered dotnetup directory to its hosted shell's process-local
PATH so typed `dotnetup` commands work even when persistent PATH changes are off.

**Tools / Options** chooses PowerShell (PowerShell 7 when available, otherwise
Windows PowerShell) or Command Prompt, and independent UI/monospace console fonts.
Changing shell restarts the hosted terminal. Preferences and window bounds are
saved through WARP's user settings service. Generated scripts use a
process-local execution-policy bypass, not a persisted policy change.
Command Prompt rejects structured arguments containing `"`, `%`, or `!` rather
than silently expanding them; use PowerShell for those paths.

## Development checks

```powershell
dotnet build src\DotnetUp\DotnetUp.slnx
dotnet run --project src\DotnetUp\DotnetUp.SmokeTests
dotnet format src\DotnetUp\DotnetUp.slnx --verify-no-changes --no-restore
```

The regression executable opens real WinForms controls and both ConPTY shells.
It checks structured inventory, GitHub discovery and cancellation, documented
command plans, dialog-only channel overrides, argument quoting, JSON capture,
exit/error reporting, malformed commands, command serialization, cancellation and
restart, first-run wizard navigation/cancellation, menus, selection status,
dialogs, and terminal shutdown. It uses local fixtures and never runs the
network installer, changes PATH, or installs/removes real SDKs. It requires an
interactive Windows desktop; Visual Studio Designer round-tripping, elevation,
and actual network installation remain manual checks.

Command behavior follows the
[dotnetup reference](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnetup/reference/dotnetup)
and [installation model](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnetup/concepts/how-dotnetup-works).
The UI/workflow design was discussed with Grok 4.7; command semantics were checked
against the Microsoft reference and upstream JSON model rather than relying on
the consultation alone.
