---
name: warp-winforms-ai
description: >-
  Adds WARP provider-agnostic chat, Designer AI components, structured prompt/schema
  requests and streamed paragraphs/code blocks. Diagnoses DI timing, old provider
  component APIs, chat history synchronization or unavailable model catalogs.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.AI\\AIChatClientComponent; src\\WarpToolkit.WinForms.Chat\\ChatView; src\\WarpToolkit.Desktop.AI\\Parser; ChatHistory; src\\WarpToolkit.Microsoft.Extensions.AI\\Templates"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# AI components and chat

## Critical constraints

- **Verify the consuming component model.** Current source uses AIChatClientComponent;
  old AIChatServiceBase/OpenAIChatService examples are not current callable guidance.
- Hosting/constructor timing belongs to `warp-winforms-application-builder`; provider
  keys to `warp-app-services`. No keys or provider-network calls in Designer code.
- ReturnTokenParser.ProcessTokens returns **IAsyncEnumerable<string>**: consume it
  with await foreach, not `await` as if a Task.
- Streaming/history updates respect UI ownership. Use `winforms-async-apis` for
  asynchronous failure boundaries, not unobserved token-by-token UI calls.
- Preserve template/provenance/schema/history semantics; do not reuse provider
  options blindly or strip AIGenerated diagnostics when rewriting source.
- Source-only API presence does not establish package parity or production provider
  capability. Current model discovery is OpenAI-only and returns limited metadata.

## Stack and current workflow

| Package | Responsibility |
|---|---|
| WarpToolkit.Desktop.AI | Provider enums/factories, parser/prompt/history primitives |
| WarpToolkit.Microsoft.Extensions.AI | MEAI integration, model/template/provenance contracts |
| WarpToolkit.WinForms.AI | Designer AIChatClientComponent |
| WarpToolkit.WinForms.Chat | ChatView/renderer/processor/prompt UI |

Keep shared ViewModels in the non-WinForms layers.
AIChatClientComponent has parameterless and IContainer constructors. Declare it in
Designer code; configure service/model options without resolving the client there.
ResolvedChatClient is lazy; SetServiceProvider **does attempt optional logger
resolution**, so it is not a pure store-only override.

Two client paths:

- ChatClientName resolves a keyed IChatClient and ignores Provider/ModelName/
  Endpoint/DeploymentName.
- Otherwise resolve IChatClientFactory, require ModelName and create the provider
  with Endpoint/DeploymentName/token options. Register the corresponding factory
  or keyed client in the consuming stack.

Do not assume an IServiceProvider-only Form façade supports keyed lookup; forward
IKeyedServiceProvider or assign the actual scoped provider at a safe runtime boundary.

GetChatOptions returns options matching component properties; InvalidateResolvedChatClient
invalidates the cached client. RequestResponseAsync, RequestStreamingResponseAsync
and RequestStructuredResponseAsync&lt;T&gt; expose response workflows; inspect their
history/parameter overloads in the installed version.

## ChatView

Current source slots AIUserChatService/AIMetaDataService accept AIChatClientComponent.
DeveloperPrompt is the writable property; **DefaultDeveloperPrompt is static read-only**.
Other members: AutoInvokeMetaDataAgent, ExceptionLevel, ChatRenderer,
PromptControl and ObservableChatHistory ChatHistory.

```csharp
// Regular code after valid provider assignment, not a Designer constructor.
chatView.AIUserChatService = chatComponent;
chatView.AIMetaDataService = metadataComponent;
chatView.AutoInvokeMetaDataAgent = true;
chatView.DeveloperPrompt = "Assist with the user's document.";
chatView.ExceptionLevel = ChatExceptionLevel.User;
```

ChatView has its own IServiceProviderAssignable contract. Its setter currently
**eagerly requires ILoggerFactory and wires events/UI**; do not blindly call it
from a generated InitializeComponent before provider assignment or repeatedly
reassign it and duplicate handlers. DI-created ChatView can be initialized in its
parameterized constructor with the actual scoped provider. Designer-only
construction and serializer order must be tested for the consuming build.
Do not assume WebView2 remains the implementation: current prompt surface uses
DirectX text editing.

The renderer/ChatProcessor handle streaming paragraphs/code/persistence. Existing
UI code should use this pipeline instead of bypassing it. Persistence includes
chat base folder, unique export filenames and load/save history; inspect current
ChatProcessor constructors rather than copying removed `_chatView.Chat`.
Prompt spelling is initialized through InitializePromptSpellCheckingAsync;
observe PromptSpellingStatus/Changed and read `warp-text-spelling`.

## Conditional advanced reference

Read [Prompt, stream, history and compatibility contracts](references/contracts.md)
for templates, JSON schemas, parser callbacks, provenance, model metadata,
current history synchronization and the explicitly historical provider inventory.
It retains uncommon contracts without presenting old samples as current APIs.
Read `warp-directx-text-editors` for source/Markdown editor surfaces,
`warp-console-diagnostics` for export/console helpers and
`warp-fluent-tab-control` only for tab hosting.

## Final checks

1. Current provider/history/component APIs and supported provider capabilities exist.
2. Initialization, keyed lookup, stream enumeration and UI failure reporting are tested.
3. Keys/provenance/history/schema and native prompt-provider lifetimes remain intact.
