---
name: warp-text-spelling
description: >-
  Adds WARP spelling squiggles, overview markers, correction menus and dictionary
  commands to accelerated text/editors or chat prompts. Diagnoses missing language
  dictionaries, empty native requests, stale results, language/checker races and
  asynchronous STA-provider shutdown.
metadata:
  source: "WARP src\\WarpToolkit.Text.Spelling; src\\WarpToolkit.Windows.Spelling; src\\WarpToolkit.WinForms.DirectX\\D2DElements\\TextBoxBase.Spelling.cs; src\\WarpToolkit.WinForms.Chat\\ChatView\\ChatView.Spelling.cs"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# Spelling checks and lifetime

## Critical constraints

- WindowsSpellingProvider owns a dedicated **STA COM apartment**. Do not move its
  native interfaces into arbitrary Task.Run; preserve owner lifetime through cleanup.
- Three prerequisites: matching named culture/dictionary, SpellCheckEnabled=true,
  and a live UI-hosted text surface with canonical text. Enabling does not install dictionaries.
- Current Windows native Check("") rejects empty input; keep enabled preference
  separate from checker attachment and detach for empty text.
- Language changes/initialization are generation-checked or serialized; an older
  completion must never attach the wrong checker. Dictionary commands use their
  **captured command.Checker**, not whichever checker is attached later.
- Diagnostic ranges are half-open **UTF-16 canonical source offsets**, not graphemes,
  visual columns or rendered Markdown offsets; do not split surrogate pairs.
- Detach/cancel/await outstanding work and DisposeAsync before final close; a native
  call already running cannot be interrupted. Timeout is not successful disposal.

## Layers and controls

| Layer | Role |
|---|---|
| WarpToolkit.Text.Spelling | Backend-neutral ISpellChecker/dictionaries/immutable requests/results/SpellingEngine |
| WarpToolkit.Windows.Spelling | Installed dictionaries/mutation; dedicated STA provider/checker |
| WarpToolkit.WinForms.DirectX | Text integration, stale-result rejection, squiggles/overview/correction UI |

Windows.Spelling is **IsPackable=false** at the source baseline: use a matching
ProjectReference, not a fabricated package. Native-free custom/fake checkers only
need backend-neutral contracts.
Integration lives in retained WARP TextBoxBase, unrelated to WinForms TextBoxBase.
D2DTextBox forwards it; D2DTextEditor/CodeEditor/MarkdownEditor inherit integration.
Other hosts of that element do not automatically gain the wrapper correction menu
or dictionary-command handling. Do not add a second engine or hand-painted squiggles.

## Attach a matching dictionary

Create the provider in regular code after UI context/OnLoad, not InitializeComponent.
Keep a Form/controller cancellation token and provider alive while checkers are used.

```csharp
WindowsSpellCheckerCreationResult result =
    await provider.CreateSpellCheckerAsync(language, lifetimeToken);
lifetimeToken.ThrowIfCancellationRequested();
if (result.SpellChecker is { } checker)
{
    activeChecker = checker;
    editor.SpellCheckEnabled = true;
    editor.SpellChecker = editor.Text.Length == 0 ? null : checker;
}
```

This is a host-method fragment, not the complete lifetime controller. A named culture
such as en-US is required; invariant culture is invalid. DiscoverAsync reports
installed dictionaries; creation reports MissingLanguage/ProviderUnavailable and
Status/Error. Missing dictionaries are a normal result, not rendering failure.
Never silently substitute a language or manufacture successful diagnostics.

Reapply attachment on TextChanged/after initialization/options changes:
attach activeChecker only when enabled and text is nonempty; use identity checks
to avoid expensive redundant assignment. Retain preference/checker while detached.
This guard remains necessary while native empty batching is unhandled.

For language switch: detach, set SpellCheckCulture, create matching checker, attach
only if lifetime/generation/requested language still matches. Catch cancellation
only when the actual lifetime token was canceled; unexpected errors reach an async
event boundary with diagnostics via `warp-app-services` or `warp-messaging`.

## Presentation, menus and dictionary mutation

SpellingUnderlineColor=Color.Empty selects theme red;
SpellingUnderlineAmplitude (e.g.0.5f) is DIPs.
SpellingOverviewMarkersEnabled is independent from underline visibility.
SpellingContextMenuEnabled activates wrapper right-click/keyboard correction UI.
Corrections require advertised dictionary capability and an accepted current snapshot
covering the selected canonical source range.

Host callback:

```csharp
private async ValueTask<bool> AddWordAsync(D2DSpellingDictionaryCommand command,
    CancellationToken cancellationToken)
{
    if (command.Checker is not WindowsSpellChecker checker)
    {
        throw new InvalidOperationException("Expected a Windows dictionary.");
    }
    WindowsDictionaryMutationStatus status =
        await checker.AddWordAsync(command.Word, cancellationToken);
    if (status != WindowsDictionaryMutationStatus.Success)
    {
        throw new InvalidOperationException($"Dictionary mutation failed: {status}.");
    }
    return true;
}
```

Assign SpellingDictionaryCommandHandler in regular runtime code.
Command captures checker/source-version/generation from the menu; success is true
only after mutation. Also available: IgnoreWordAsync/RemoveWordAsync. Respect
SpellingDictionaryCapabilities and returned statuses; not every dictionary can
remove/add/ignore. Adding changes the user's Windows dictionary; tests must not.
Report failures through the wrapper failure event/application boundary.

## Hosted elements, chat and diagnostics

For an actual retained text element, assign SpellCheckCulture from
checker.Dictionary.Identity.Language, SpellChecker, SpellCheckEnabled and markers.
It requires a real host/dispatcher, not independent Control parenting. Implement
context/dictionary UI explicitly if not using D2DTextBox.

ChatView.InitializePromptSpellCheckingAsync plus PromptSpellingStatus/Changed
reports prompt dictionary state; an application-supplied checker is retained.
Read `warp-winforms-ai` for chat hosting.

SpellingProviderFailed reports e.Exception. SpellingDiagnostics is an accepted
immutable snapshot, not a mutable diagnostics list. WaitForSpellingIdleAsync
awaits checks **and UI publication**; keep a pumping UI and never block it.

## Shutdown and custom-provider contracts

Use a guarded asynchronous close: cancel the first FormClosing, confirm/save,
detach editor.SpellChecker/SpellingDictionaryCommandHandler, cancel lifetime,
await initialization/check consumers and provider.DisposeAsync, then close again
under a completion flag. Only afterward dispose the token source. Synchronous
Dispose may block COM cleanup; prefer DisposeAsync on UI. Read
`winforms-async-apis` / `winforms-com-apartments` for the boundary mechanics.

ISpellChecker requires Dictionary and
`ValueTask<SpellingCheckResult> CheckAsync(SpellingCheckRequest, CancellationToken)`.
A headless request uses text, dictionary language, sourceDocumentVersion and generation.
Return `new SpellingCheckResult(request, issues)` to enforce request identity,
nonoverlap and requested-range coverage.

SpellingEngine (for a host without integrated adapter) supplies 250ms debounce,
cancellation/generation/source-version checks, RequestCheck(checker, immutableText,
documentVersion, culture), SnapshotChanged, ProviderFailed and CurrentSnapshot.
Dispatch engine events to the host UI dispatcher; do not duplicate an editor's engine.

## Final checks

1. Fake providers validate UTF-16/ranges and stale text/language/cancellation behavior.
2. Failure, generation switches and disposal are deterministic; wait for UI publication,
   not sleeps.
3. Native smoke tests handle missing languages and never mutate real dictionaries.
