---
name: warp-winforms-application-builder
description: >-
  Bootstraps or retrofits a WinForms app with WARP Generic Host services,
  registers scoped Forms/UserControls and fixes Designer component service
  resolution occurring before the DI constructor has finished.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.AppServices\\WinFormsHost; src\\WarpToolkit.WinForms.Primitives\\BindableServiceProviderComponent.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Hosted WinForms startup

## Critical constraints

- `WinFormsApplication.Run()` owns the single message loop, host start/stop/disposal
  and `Current` cleanup. Do not also run `Application.Run` or manually dispose the
  running application in Main.
- Keep a Designer-safe parameterless constructor; chain the DI constructor with
  `: this()` so InitializeComponent runs once. Put the façade and helpers in the
  regular partial, not the Designer partial.
- **EndInit is still inside InitializeComponent.** It cannot resolve through a
  provider assigned in the later DI constructor body. A closure wrapper does not
  change this ordering. Store provider references first; resolve services lazily
  after construction, normally OnLoad for UI/async work.
- `IServiceProvider.GetService` may return null for an optional/unregistered service.
  Use `GetRequiredService` at the actual required-service boundary; do not turn
  every optional lookup into an exception.
- Framework startup/layout/Designer rules belong to `winforms-development`,
  `winforms-high-dpi-fluent-layout`, `winforms-designer-code`; .NET11 options to
  `winforms-net11-apis`. Do not copy their recipes into feature code.

## Bootstrap

Package: `WarpToolkit.WinForms.AppServices`; builder namespace
`Microsoft.Extensions.WinForms`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WinForms;
using WarpToolkit.WinForms.AppServices.ServiceExtensions;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = WinFormsApplication.CreateBuilder(args);
        builder.UseColorMode(SystemColorMode.System)
            .UseHighDpiMode(HighDpiMode.PerMonitorV2)
            .UseLegacyVisualStyles(true)
            .UseTextRenderingV2(true)
            .UseStartupForm<MainForm>();
        builder.Services.AddWinFormsDialogService();
        builder.Build().Run();
    }
}
```

`UseStartupForm<TForm>() where TForm : Form, new()` uses `TryAddScoped<TForm>`.
Run creates a startup scope and resolves the Form there. Register other
DI-resolved Forms/UserControls explicitly, choosing their scope/lifetime deliberately.
Do not describe the root provider as a universal Form scope.

Builder options own visual styles, text rendering, DPI, color and default font.
Do not duplicate static Application configuration. JSON settings are controlled by
`AllowWinFormsJsonAppSettings(bool)`; inspect builder precedence before overriding
code with configuration. `UseProjectSettings()` is available. .NET11 builds also
expose `UseVisualStylesMode`; do not call nonexistent `UseVisualStyles`.

## Designer-safe Form façade

This regular partial pairs with the ordinary Designer-generated MainForm partial:

```csharp
public partial class MainForm : Form, IServiceProvider
{
    private IServiceProvider? _serviceProvider;

    public MainForm()
    {
        InitializeComponent();
    }

    public MainForm(IServiceProvider serviceProvider) : this()
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return _serviceProvider?.GetService(serviceType);
    }
}
```

The same pattern applies to UserControls that host DI-aware components. Assigning
the real provider after `: this()` makes later lookups usable: store-only components
retain the Form reference. Before assignment, optional GetService returns null;
required-service boundaries must wait and report missing runtime setup. A nested
`DeferredServiceProvider` can be retained for compatibility with a consuming
template, but it is only a forwarding wrapper—not an initialization scheduler.
Do not add framework-version conditionals around this regular-partial structure.

The Designer serializer can emit `_component.SetServiceProvider(this)`.
`BindableServiceProviderComponent` lives in `WarpToolkit.WinForms.Primitives`;
its base implementation stores a plausible provider and uses a null-object provider
at design time. Derived setters must call the base and must not eagerly resolve
required dependencies. Current `AIChatClientComponent` has empty BeginInit/EndInit,
lazy ResolvedChatClient, but an optional logger lookup in SetServiceProvider.
Current ChatView.SetServiceProvider requires ILoggerFactory immediately and wires
UI/events; it is **not** store-only. Its generated/runtime assignment order needs
testing or a safe runtime initialization path; do not claim this façade fixes
an eager required lookup during InitializeComponent.

The plain façade does not implement IKeyedServiceProvider. Components resolving
keyed clients must receive the real scoped provider at a safe boundary or use a
reviewed façade that also forwards keyed lookup; IServiceProvider alone is insufficient.

## Retrofit and safe phases

1. Reference the matching hosting stack; replace the old Main/loop with the builder.
2. Preserve each parameterless constructor; add the chained DI constructor/façade
   only where DI-aware components require it.
3. Register views and domain services; move ViewModel message calls to AppServices.
4. In constructors wire named handlers and own state only. After provider assignment,
   synchronous non-UI services may be resolved if their dependencies are safe.
5. Use OnLoad for HWND/message-pump/other-view/async dependencies (including any
   WebView2-backed feature); OnShown for visible focus/first-presentation work.

For **VB choosing this builder**, use an explicit `<STAThread>` Sub Main (for
example Program.vb), disable the VB Application Framework and select that startup
entry. The builder calls Application.Run itself. No AppFramework integration
is supplied by this verified lifecycle; do not invent an ApplicationEvents adapter.
Existing VB apps that retain AppFramework use framework startup guidance instead
of adding a second builder-owned loop.

## Diagnostics and conditional references

| Symptom | Check |
|---|---|
| Required lookup during InitializeComponent/EndInit | Provider is not assigned yet; fix component eager lookup, not a fake deferral wrapper |
| Missing provider at runtime | Was the Form made with `new` rather than resolved in its scope? |
| Null-object services at runtime | Design-mode fallback or missing façade; do not silently claim initialization succeeded |
| Designer throws | Constructor performs real work, derived setter bypasses base, or required-service access is premature |
| No startup Form / duplicate Current | Missing `UseStartupForm` or leaked/competing host lifetime |

Read `warp-app-services` for dialogs/settings/keys/exceptions, `warp-winforms-ai`
for current AI components, `warp-fluent-tab-control` for DI-resolved tab views,
and `winforms-async-apis` for async event exception boundaries. Do not swallow
required runtime dependency failures as successful “degraded” initialization.
Optional features can degrade only with explicit diagnostics.

## Final checks

1. Designer construction performs no service/network/native initialization.
2. No required/eager runtime resolution before DI assignment; no duplicated
   InitializeComponent. Optional early lookups may legitimately return null.
3. Runtime views resolve in a valid scope; host alone owns lifetime and loop.
