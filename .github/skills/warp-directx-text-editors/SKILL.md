---
name: warp-directx-text-editors
description: >-
  Embeds and configures WARP accelerated text, code and Markdown editors, preserves
  document encoding/newlines/metadata, synchronizes source and preview and handles
  completion/options without changing the live document on Cancel.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.DirectX\\Controls\\D2DText*.cs; D2DCodeEditor.cs; D2DMarkdown*.cs; src\\WarpToolkit.WinForms.Roslyn\\Roslyn\\RoslynCodeEditorService.cs"
---

# Accelerated document editing

## Critical constraints

- Verify the installed document surface, then use it rather than deriving an editor
  merely to change options.
- Editable Markdown remains canonical source; preview/plain rendered text must not
  replace it. Relative resources resolve from the **document file URI**.
- Use document APIs for encoding/newline/history/metadata fidelity; do not force
  Modified=false after asynchronous save when concurrent edits may remain.
- Edit a detached options copy, validate and apply only differences on OK;
  Cancel must not mutate the live editor/document or persist typed-text logging consent.
- Source and packages may differ. Windows.Spelling currently is not packable;
  a matching source reference is required for that backend.
- Shared Designer/layout/async/COM rules belong to `winforms-designer-code`,
  `winforms-high-dpi-fluent-layout`, `winforms-async-apis`,
  `winforms-com-apartments`. Accelerated typing does not require a background mode.

## Surface and dependency

Controls are in WarpToolkit.WinForms.DirectX.Controls:

| Need | Surface |
|---|---|
| Single-line/lightweight multiline field | D2DTextBox, single-line default |
| Plain documents/logs/notes | D2DTextEditor, document persistence/journal/gutters/bookmarks/comments, no automatic classification |
| Classified source code | D2DCodeEditor, asynchronous classification/edit assistance; provide language service |
| Attributed editable raw Markdown | D2DMarkdownEditor, source delimiters retained |
| Rendered read-only Markdown links/images/tables | D2DMarkdownPreview; assign Markdown, not Text |
| Retained hosted text element | WARP TextBoxBase, unrelated to System.Windows.Forms.TextBoxBase |

D2DTextEditor derives D2DTextEditorBase; D2DTextBox exposes TextBoxElement, selection,
clipboard/history and spelling forwarding. DirectX brings Drawing/Text.Spelling
contracts transitively. Add WinForms.Roslyn for C#/VB; Windows.Spelling source only
for Windows dictionaries. Windows 11 build 22000 still uses windows10.0 in the TFM.
Do not mix an older DirectX binary with newer source text/spelling contracts.
The inspected local .NET10/.NET11 DLLs lacked D2DTextEditor and D2DTextEditorBase;
document/persistence fragments below are current-source guidance, not a verified
drop-in example for those binaries. Check a matching rebuilt/published assembly.

Designer fragment, backing field declared in the Designer partial:

```csharp
_editor = new WarpToolkit.WinForms.DirectX.Controls.D2DTextEditor();
_editor.Name = "_editor";
_editor.Dock = DockStyle.Fill;
Controls.Add(_editor);
```

Parameterless constructor only initializes Designer-safe state. Load settings and
configure after construction, normally OnLoad. DI-aware hosts follow
`warp-winforms-application-builder`. Default render mode is the ordinary starting
point; background/native resources obey `warp-directx-getting-started`.

## Options inventory

| Group | Properties worth preserving |
|---|---|
| Appearance/input | Font, AutoWrap, ReadOnly, EmojiAutoDisplay, BorderStyle, ScrollBars, VerticalScrollBarMode |
| Cursor/alignment | SmoothSnapStrategy/Speed, CaretTypingPause, CaretBlinkHalfPeriod, CursorMovementStyle, CursorHighlightWhenUnfocused/OnFocus/Level |
| Completion | AutoCompleteEnabled, MinPrecedingCharsSuggestionRequestSensitivity, MinTimeSuggestionRequestSensitivity, SuggestionFontStyle/Opacity/Color |
| Spelling | SpellCheckEnabled/Culture/Checker, SpellingOverviewMarkersEnabled, SpellingContextMenuEnabled, SpellingUnderlineAmplitude/Color |
| Document-only | ShowLineNumbers, ShowIndicatorMargin, GutterPreviewDelay, DocumentEncodingKind, DocumentNewLineConvention, AutoFormatJson |
| Diagnostics | Diagnostics.Enabled, CopyReportToClipboard, InputLogLevel, IncludeTextInput |

```csharp
editor.AutoWrap = true;
editor.ShowLineNumbers = true;
editor.ShowIndicatorMargin = true;
editor.VerticalScrollBarMode = D2DTextScrollBarMode.Wide;
editor.AutoCompleteEnabled = true;
editor.MinPrecedingCharsSuggestionRequestSensitivity = 2;
editor.MinTimeSuggestionRequestSensitivity = 0.35f;
```

This fragment targets a document editor, not every D2DTextBox. Validate finite
numeric values, legal enums, positive timing/font sizes and named culture before
application. Theme-following defaults avoid a fixed white background.
Dynamically created fonts remain alive while controls use them and superseded fonts
are disposed afterward. Completion/spelling enabled flags need a provider/handler.

The source example is TurboGraphics EditorOptionsSnapshot: snapshot/copy/validate/
apply differences; JSON explicitly excludes IncludeTextInput consent.

## Document I/O and editing

```csharp
using WarpToolkit.WinForms.DirectX.TextEditing.Persistence;

string path = Path.GetFullPath(fileName);
var metadata = new ApplicationMetadataSidecarStore(path, "MyApplication");
await editor.LoadDocumentAsync(path, metadata, cancellationToken);
await editor.SaveDocumentAsync(path, metadata, cancellationToken: cancellationToken);
```

Use a stable sidecar application name. Bookmarks/comments/session state stay
separate from the original text. Fresh documents can load a UTF-8 MemoryStream
through LoadDocumentAsync to reset history/document state.
Before discard/close, TextBoxElement.FlushPendingTyping then inspect
IsDocumentModified. Confirm before New/Open; update path/recent history only after
successful save and recheck modified state if edits occurred during I/O.
Selection/SelectedText/Cut/Paste/Undo/Redo preserves editing operations; navigate
with TextBoxElement.GoToLine(oneBasedLine).

## Markdown source/preview

Use Designer-created SplitContainer panels. Source can be plain D2DTextEditor or
raw attributed D2DMarkdownEditor. Named TextChanged handler updates preview.Markdown;
perform initial sync and refresh DocumentBaseUri after Open/Save As.

```csharp
preview.DocumentBaseUri = fileName is null
    ? null
    : new Uri(Path.GetFullPath(fileName));
preview.Markdown = editor.Text;
```

Do not use an arbitrary working directory as the document base. Collapse preview
for non-Markdown documents or when disabled; debounce large source updates.
MarkdownTemplate, LinkOpening/LinkOpenFailed and ImageLoadFailed support semantic
links/anchors/images/tables; surface failures and choose an external-link policy.

Raw Markdown options: WrapColumn is 30..200 (e.g.80), AutoWrap,
ImageDocumentBaseUri and ProjectImages. Its AutoWrap **shadows** the base property
to refresh paragraph layout; assign through the Markdown-typed reference.
RevealImageSource/RestoreImageProjectionAsync expose/restore syntax without
changing canonical Markdown; projected source images are not a separate preview.

## Classification and completion

For C#/VB assign RoslynCodeEditorService to LanguageService with LanguageId
`"csharp"` / `"vb"`. LanguageService takes precedence over RunProvider.
XML-family tag completion uses XmlTagCompletionService; opening a Rust/Python file
as text does not provide language classification.
FencedCodeRunProvider can use RoslynClassificationRunProvider and a theme-appropriate
ClassificationStyleMap for Markdown fenced C#/VB.

AsyncRequestAutoCompleteSuggestion handlers return Task, honor CancellationToken
and set SuggestionText to the **untyped suffix**. Handle AutoCompleteSuggestionFailed.
CurrentParagraph is text preceding the caret; inspect its trailing word-boundary
prefix rather than equating an entire paragraph with the beginning of a word.
Do not block the UI with network/provider calls.

Read `warp-text-spelling` for dictionary/provider lifetime. Source examples:
TurboGraphics FrmWarpPad, EditorOptionsSnapshot, FrmProtoMarkdownEditor and
WarpPadVocabulary; these are provenance, not required local consumer paths.

## Final checks

1. Open/save/cancel preserve canonical text, clean state, encoding/newlines and metadata.
2. Preview/resource/classification/completion behavior fits actual language/document.
3. Options, sensitive diagnostics, font/native providers and failure paths have owners.
