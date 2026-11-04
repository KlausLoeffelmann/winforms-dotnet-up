# Regular-code conventions and migration

## C# and file ownership

Use the project's supported language version. Modern C# can use file-scoped namespaces, nullable references, pattern matching, switch expressions, records, target-typed `new`, collection expressions and raw strings when the compiler supports them. Do not move that syntax indiscriminately into Designer serialization.

Prefer primitive aliases (`int`, `string`, `bool`), clear explicit short types and `var` where the type is obvious or unwieldy. Follow existing import/global-using policy; standalone examples state their imports. Nullable sender parameters and change-notification events accurately reflect optional values. Use framework throw helpers when available.

Single-expression members can be expression-bodied; side-effect-heavy logic deserves clear blocks. Format for readability, not extreme line compression.

Property lifetime matters: `Items => new List<T>()` produces a fresh collection on each access; `Items { get; } = new()` preserves one instance; `Items => _items ??= new()` initializes lazily. Allocations are not automatically a memory leak, but changing identity/lifetime can break bindings or retained state.

Raw strings support single-line and multiline forms. For multiline raw literals, opening-delimiter line, closing indentation and delimiter length govern content/whitespace; do not impose multiline rules on single-line syntax.

Document exposed public/internal/protected APIs and interface implementations according to consumer style: summary, meaningful parameters/results/exceptions, remarks with `<para>`, and `<inheritdoc/>` where appropriate. Avoid comments that merely repeat code.

Forms/UserControls normally use main and Designer partials plus resources. Models/ViewModels/services can live in separate projects for clean boundaries; separate ViewModels project is architectural preference, not a generator necessity.

## VB application defaults

This fragment belongs in an **existing Application Framework** `MyApplication` partial. Its generated enclosing class/namespace and event infrastructure are project-owned:

```vb
Private Sub ApplyDefaults(
        sender As Object,
        e As Microsoft.VisualBasic.ApplicationServices.ApplyApplicationDefaultsEventArgs
    ) Handles Me.ApplyApplicationDefaults

    e.HighDpiMode = System.Windows.Forms.HighDpiMode.PerMonitorV2
    e.ColorMode = System.Windows.Forms.SystemColorMode.System
End Sub
```

Set `e.Font` deliberately if a runtime application font is required, with ownership consistent with the framework. Avoid setting arbitrary fonts on every designed child. VB without Application Framework needs an explicit startup route; it is not required to omit Program.vb.

Use `Friend WithEvents` and `Handles` for designed control events where compatible with the existing project. Existing `AddHandler/AddressOf` code may remain. The main partial owns event bodies, not the Designer file.

## Migration sequence

1. Inventory project format, framework, Windows version, native architecture, packaging, resources, manifests and supported Designer tooling.
2. Preserve current behavior with a baseline build and focused UI scenarios. Scope framework/package upgrades explicitly.
3. Move to supported SDK project/startup conventions where requested. Preserve desktop execution-level/compatibility requirements and translate legacy DPI configuration deliberately.
4. Introduce nullable/language conventions in regular code without breaking generated serialization, reflection or public API contracts.
5. Modernize async, binding, command and theme behavior through their owners; do not replace functioning event/legacy binding architectures without a need.
6. Rebuild and roundtrip Forms/UserControls; test font/DPI/monitor, keyboard/translation, native and deployment paths.

VB6 modernization needs explicit mapping of events, validation, data access, resources, control behavior and lifetime; renaming generated files is not a behavioral migration.

## Version-sensitive features

Use the exact target SDK/reference assembly for overloads, attributes and obsoletion diagnostics. .NET 10 typed clipboard and capture policy need schema/OS gates; .NET 11 renderer/kiosk additions need an explicit release boundary. These are capability decisions, not reasons to force every app onto the newest installed SDK.
