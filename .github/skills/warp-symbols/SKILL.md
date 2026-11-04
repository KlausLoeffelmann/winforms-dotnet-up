---
name: warp-symbols
description: >-
  Selects WARP font, SVG and bitmap symbol assets, accessible metadata and shared
  bitmap caching. Diagnoses missing source fonts, enum aliases, renderer registration,
  theme/DPI refresh and image/font ownership.
metadata:
  source: "WARP src\\WarpToolkit.WinForms\\SymbolFactory\\FluentSymbols.*.cs; Symbols\\FontSymbol.cs; FontSymbolRenderer.cs; SymbolRenderer.cs; WinFormsSymbolCache.cs; src\\WarpToolkit.WinForms.Drawing\\Symbols"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Symbol catalogs and architecture boundary

## Critical constraints

- FluentSymbols lives in WarpToolkit.WinForms.Symbols, shipped by
  WarpToolkit.WinForms; namespace is not another NuGet package.
- Catalog enums are nested: use `FluentSymbols.AllSymbols`, not a top-level type.
- Glyphs require their declared font. Font sources scale, but the current ToolStrip
  renderer produces bitmap images.
- Backend-neutral ISymbol, FontSymbol, SvgSymbol, BitmapSymbol and unified bitmap
  caching **are implemented in current source**, not merely planned. Check installed
  assemblies separately; source presence does not establish published package parity.
- Borrowed cached bitmaps must not be mutated/disposed; clone for an independently
  owned image slot. Keep source images/fonts alive through rendering.
- Accessibility belongs first to the host; asset metadata supplies an unset-host
  fallback, not an override of an explicit control/item name or description.

## Catalog and workflow

Each enum carries `[SourceFontName("Segoe Fluent Icons")]`; the attribute type is
WarpToolkit.WinForms.Extensions.SourceFontNameAttribute.

| Nested enum | Purpose / examples |
|---|---|
| AllSymbols | GlobalNavButton=0xE700, Wifi=0xE701, Copy, SaveAs, Stopwatch |
| CommonToolStripSymbols | Send=0xE724, Accept=0xE8FB, Cancel=0xE711, Edit=0xE70F; New=AddBold, Open=OpenFile |
| DevelopmentSymbols | Code=0xE943, Component=0xE950, DeveloperTools=0xEC7A, CommandPrompt=0xE756 |
| TreeViewSymbols | ChevronDown=0xE70D, ChevronRight=0xE76C, Folder=0xE8B7, FolderOpen=0xE838 |

Aliases intentionally share code points; do not delete them as duplicate assets.
Segoe Fluent Icons ships with Windows 11; missing fonts can cause fallback/tofu.
Do not invent code points or silently substitute a font.

Pick a member, check its font and read `warp-toolstrip-icons` for ConfigureItem,
owner/detached rendering context, theme refresh and DPI. That renderer lives in
WarpToolkit.WinForms.Extensions.UI. Custom enums require real code points and
SourceFontNameAttribute; Generic.GetFont throws InvalidOperationException without it.

## Current backend-neutral contract

In WarpToolkit.WinForms.Drawing.Symbols, ISymbol describes AccessibilityName,
AccessibilityDescription and GetPreferredSize(Size proposedSize), not rendering.
Size.Empty is unconstrained; one zero axis is unconstrained; constrained sizing
preserves natural aspect ratio. Rendering is separate ISymbolRenderer<TSymbol>
in Symbols.Rendering. The former architecture goal is now implemented without
a Direct2D dependency in GDI+/ToolStrip command infrastructure.

| Asset | Current contract and ownership |
|---|---|
| FontSymbol (WinForms.Symbols) | Parameterless or char/font/offset constructor; FromFluentSymbol(Enum,float pixels); SymbolFont/Character/Offset, SymbolScaling/Value, rotation, HasGlyph, ContentVersion |
| SvgSymbol (Drawing.Symbols) | FromMarkup/FromFile/FromStream or parsed-document constructor; TryParse; validates **WARP Symbol SVG Profile 1.0**, not arbitrary browser SVG; Document and root title/desc metadata |
| BitmapSymbol (Drawing.Symbols) | Empty or Image constructor; caller owns constructor/property images; FromFile/FromStream create owned images disposed with symbol; every Image assignment advances ContentVersion |

SvgSymbol factory parsing throws SvgSymbolException on profile violation; TryParse
returns false/null. BitmapSymbol.FromStream clones decoded data so the stream need
not remain alive. Replacing a symbol-owned image disposes it; replacing a borrowed
image does not dispose the caller's old image. In-place bitmap mutation alone does
not signal cache content changes.

FontSymbol scaling is Parent (natural font, host scales bitmap), ChildOffset
(host size + value), ChildScaled (host size * value, default1). Rotation normalizes
modulo360; pixel offset fine-tunes placement. Host-aware SymbolRenderer.Measure/Draw
is more accurate than context-free preferred sizing. FromFluentSymbol allocates a
Font; FontSymbol is not IDisposable, so the consumer owns that font. GetEffectiveFont
returns a borrowed font in Parent mode and a newly allocated font in child modes.
SymbolRenderer disposes its temporary effective fonts and RenderToBitmap returns
an independently owned bitmap.

## Shared bitmap cache

Public Drawing.Symbols.Caching.SymbolBitmapCache pre-registers SVG/bitmap GDI
renderers; a standalone cache needs RegisterRenderer(new FontSymbolRenderer()) for
fonts. Dispatch is by **exact runtime type**, not base/interface assignability.
Duplicate registration throws unless replaceExisting=true; replacement does not
invalidate old entries—Remove/Clear explicitly. Unregistered types throw
NotSupportedException instead of silently producing blank symbols.

GetOrCreate keys identity/content version, device-pixel size, DPI scale, current
color, theme/high contrast and HostFont. Equivalent requests are coalesced; failed
lazy entries are removed for recoverable retries. WarmUpAsync supports cancellation
of remaining work. Remove/Clear/Dispose invalidate and dispose cache-owned bitmaps;
coordinate readers before eviction. Thread-safe cache operations do not make
mutable symbols or their source image/font lifetimes race-safe.

SymbolRenderContext carries those inputs. SVG currentColor resolves from the host;
bitmap content is not automatically recolored. WinFormsSymbolCache.Shared is
**internal**, registers FontSymbolRenderer once and is used by WARP symbol ToolStrip
items. Its reference counts evict assets when the last item state releases them.
SymbolToolStripSupport clones cached images across the item ownership boundary.
Read `warp-toolstrip-icons` for those item hosts versus the older Enum helper.

## Final checks

1. Nested member/font metadata exists in the installed package.
2. Font fallback and bitmap refresh are explicit.
3. Renderer/cache registration, content invalidation and borrowed-resource lifetimes
   are explicit; current source is not confused with installed-package availability.
