---
name: winforms-development
description: >-
  Configures WinForms applications and libraries: project targeting, C# or VB
  startup, DPI and default fonts, exception reporting, regular-code conventions,
  and legacy modernization. Applies when adding a project, changing framework
  or Windows API targets, configuring app defaults, clipboard formats or capture
  privacy, or fixing startup configuration; specialized skills own UI layout,
  Designer serialization, binding, threading, and rendering.
---

# Project targeting and application foundations

## Durable rules

- Preserve the consuming project's target, language, imports, build/signing conventions and supported Windows versions; upgrades need a requirement.
- Modern regular code and Designer serialization have different constraints. Do not refactor generated UI using regular-code shortcuts.
- Configure DPI/default font once, before creating windows; use generated application defaults rather than conflicting later calls.
- Pin compatible stable dependencies using the repository's package policy. Do not publish unbounded package ranges.
- Report exceptions appropriately; application-wide logging is not a guarantee of safe recovery.

## Project and startup

For a **new .NET 10 application when that target is appropriate**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
  </PropertyGroup>
</Project>
```

`ApplicationConfiguration.Initialize()` applies generated visual styles, DPI and default-font settings. Set `ApplicationDefaultFont` deliberately if needed; don't override a layout's measured font using arbitrary child fonts.

```csharp
using System;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new Form());
    }
}
```

This chooses system color mode, not an unconditional dark palette. `.NET 10.0.14` reference metadata confirms `SetColorMode` and `IsDarkModeEnabled` are non-experimental; .NET 9 has `WFO5001`. Maintain target-qualified use/suppressions.

Windows API consumption may need a versioned Windows TFM such as `net10.0-windows10.0.22000.0`; choose the API's actual minimum contract, not that example blindly. Windows-targeted class libraries are valid. Keep portable models/services free of Windows dependencies.

For VB with the Application Framework enabled, use `MyApplication.ApplyApplicationDefaults` for `e.HighDpiMode`, `e.Font`, and supported `e.ColorMode`. VB projects without that framework can use an explicit entry point. Read [conventions and migration](references/conventions.md) for VB defaults, language gates, XML docs and file organization.

Modern DPI setup belongs in generated/code defaults. Existing .NET Framework app.config/manifest DPI settings and desktop manifest execution-level/compatibility declarations require migration-aware treatment, not a universal deletion ban.

## Current .NET 10 capability gates

- Clipboard: prefer typed `Clipboard.TryGetData<T>` and supported JSON formats for new object interchange. Validate format/schema/input and retain required interoperable text/image formats. Never enable unsafe BinaryFormatter fallback merely to preserve legacy serialization.
- Capture privacy: evaluate `Form.ScreenCaptureMode` only when required. Verify enum members, supported Windows behavior and target SDK; unsupported capture protection is not a security guarantee.
- Legacy controls: investigate obsolete APIs such as older menu/status/toolbar families using the target's diagnostics; migrate to supported controls without losing commands, shortcuts, accessibility or resources.
- API details: use [official .NET 10 changes](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net100) and target reference assemblies. Do not copy .NET 11 APIs into .NET 10.

## Application lifetime and errors

`Application.ThreadException` handles configured WinForms UI-loop failures; `AppDomain.UnhandledException` can log fatal unhandled failures but cannot generally prevent termination. Register policy before the message loop and avoid continuing after corrupted state. Async event boundaries still need their own cancellation/fault observation; see [async](../winforms-async-apis/SKILL.md) when implementing them.

## Final checks

1. Build intended target/architecture without silent upgrades or newly introduced package warnings.
2. Startup has one coherent DPI/font/color policy; test different monitors and runtime fonts.
3. Preserve resources, manifests, Designer editability and required legacy behaviors during migration.
