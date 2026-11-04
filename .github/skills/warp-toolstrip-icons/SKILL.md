---
name: warp-toolstrip-icons
description: >-
  Adds WARP symbol ToolStrip/MenuStrip items or configures classic items with enum
  glyph images and handlers. Handles detached commands, theme/DPI refresh and image
  ownership; diagnoses null Owner, source fonts and duplicate click subscriptions.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.Extensions\\UI\\ToolStripExtensions.cs; Generic.cs; src\\WarpToolkit.WinForms\\ToolStrips\\ISymbolToolStripItem.cs; SymbolToolStripSupport.cs; Symbols\\WinFormsSymbolCache.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# ToolStrip glyph configuration

## Critical constraints

- Owner-inferred ConfigureItem requires Owner; the **explicit renderingHost overload**
  supports detached items without changing ownership **in audited source**.
  Local prebuilt .NET10/.NET11 DLLs lacked that overload; verify/rebuild the matching
  source or check the installed public assembly before using it.
- Configuration is regular runtime code, not Designer serialization. Add ordinary
  owned items declaratively first; read `winforms-designer-code`.
- The tuple is `(clickHandler, removeBeforeAdd)`, not addHandler.
- The Enum helper's font source is scalable, but its image is a bitmap: regenerate after
  theme/DPI changes instead of promising automatic live retinting.
- Current backend-neutral assets/cache belong to `warp-symbols`; select an actual
  symbol item host or the older Enum helper, not an invented overload.

## Symbol item hosts versus enum helper

Current WarpToolkit.WinForms.ToolStrips has SymbolToolStrip/SymbolMenuStrip/
SymbolStatusStrip and SymbolToolStripButton/SymbolToolStripMenuItem/
SymbolStatusStripLabel. Its ISymbolToolStripItem.Symbol is ISymbol?, accepting
current font/SVG/bitmap assets. The shared GDI+ cache is implemented: item support
clones cached bitmaps for owned image slots, preserves host-font/current-color/
theme/DPI inputs, and propagates an unregistered type's NotSupportedException
instead of hiding it. Use `warp-symbols` for assets/cache/accessibility and source
image/font lifetimes. Verify those types in the installed public assembly.

The helper below instead configures ordinary ToolStripItem.Image from an Enum.
It does not accept ISymbol or automatically join the unified symbol-item cache.

## API and workflow

Packages: WarpToolkit.WinForms for FluentSymbols/symbol item hosts;
WarpToolkit.WinForms.Extensions for SourceFontNameAttribute and Enum rendering;
WarpToolkit.WinForms.Drawing for backend-neutral assets/cache.
Import WarpToolkit.WinForms.Extensions.UI and WarpToolkit.WinForms.Symbols.

Both ConfigureItem overloads return void:

```csharp
void ConfigureItem(this ToolStripItem item, Enum symbol,
    (EventHandler clickHandler, bool removeBeforeAdd)? eventHandler = null,
    string? tooltipText = default, int size = 0,
    Color? foreColor = null, Color? backColor = null);
void ConfigureItem(this ToolStripItem item, ToolStrip renderingHost, Enum symbol,
    (EventHandler clickHandler, bool removeBeforeAdd)? eventHandler = null,
    string? tooltipText = default, int size = 0,
    Color? foreColor = null, Color? backColor = null);
```

These are source signature excerpts. The first throws NullReferenceException if the
item/Owner is null; the second requires a non-null rendering host but not Owner.
The host supplies image size, colors and HWND/DPI context.

```csharp
// Regular code, after the Designer has added saveItem to its strip.
saveItem.ConfigureItem(FluentSymbols.CommonToolStripSymbols.Save,
    eventHandler: (OnSaveClick, removeBeforeAdd: true),
    tooltipText: "Save (Ctrl+S)");
// Source-version-gated: only when the installed overload has been verified.
detachedItem.ConfigureItem(strip, FluentSymbols.AllSymbols.Copy,
    tooltipText: "Copy");
```

If Click is already wired in Designer, omit eventHandler. For ImageAndText,
set DisplayStyle and TextImageRelation deliberately after configuration.
Repeated setup with the **same handler** can remove-before-add; this is not a
general cleanup of every old/different handler.

## Defaults, custom fonts and lower-level APIs

- size=0 uses host.ImageScalingSize.Width; rendered font size is roughly 80% of
  bitmap size. Prefer the default unless command-strip metrics differ.
- foreColor defaults to host.ForeColor.
- ConfigureItem defaults backColor to **Transparent**, unlike GetSymbolImage,
  whose default is host.BackColor. Nontransparent tiles are intentional opt-ins.
- Enum values must be code points in an enum carrying SourceFontNameAttribute.
  Custom Segoe MDL2 enums can use Play=0xE768, Pause=0xE769, Stop=0xE71A.
  Missing attribute makes Generic.GetFont throw InvalidOperationException.
- GetSymbolImage(strip, symbol, size=0, foreColor=null, backColor=null) returns Image.
  GetSymbolIcon(strip, symbol, size=0) returns Icon. Check native-handle/image
  ownership when adapting these to NotifyIcon/ImageList; do not assume transfer.

For Enum-helper theme refresh, rerun configuration with current host colors and the same
handler replacement policy. Source assigns a new image on every call; handle
old-image lifetime explicitly according to the consuming UI ownership.
Read `winforms-rendering` for GDI/image resources and `winforms-net11-apis`
only when using framework-specific strip styles.

## Final checks

1. Owner or explicit rendering context exists at configuration time.
2. One handler executes; intended text/image arrangement and transparent backdrop remain.
3. Theme/DPI refresh and image/native-icon disposal are accounted for.
