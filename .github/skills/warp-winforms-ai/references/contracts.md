# AI prompt, streaming, history and compatibility contracts

Read for structured requests, direct streaming extraction, history persistence or
an older consuming package. Current source baseline/provenance is in the parent
SKILL.md; historical contracts below are retained for migration, not aliases.

## Contents

- Component options and model discovery
- Templates and schema
- Streaming parser
- Observable history
- Historical component/history inventory

## Component options and model discovery

AIChatClientComponent exposes Provider/ModelName/ChatClientName/Endpoint/
DeploymentName, DeveloperPrompt, JsonSchemaString, RequestCodeBlockExtractions,
response text-format options, Temperature/TopP/TopK/Seed, MaxOutputTokens,
FrequencyPenalty/PresencePenalty/StopSequences and ConversationId.
GetChatOptions creates options; unsupported sampling/provider parameters are not
made portable by having a property. Preserve per-provider capabilities.

Current GetModelsAsync(ModelFeatures? filter, CancellationToken cancellation)
returns IAIModels, **not IReadOnlyList<IAIModelInfo>**. Only OpenAI catalog is
implemented; other providers throw NotSupportedException. Current source does not
apply the supplied filter and reports Features.Unknown, zero token/context limits,
IsVetted=false. Do not select a “best vetted model” from metadata that is unavailable.

IAIModelInfo/AIModelInfo contracts retain provider, context/output limits, feature,
deprecation/vetted indicators and extended knowledge-cutoff/deprecation-date/
latency/pricing/comparison metadata where a provider actually supplies it.
Do not synthesize known limits/pricing or assume catalog existence equals suitability.

## Templates and schema

Namespace WarpToolkit.Microsoft.Extensions.AI.Templates:

```csharp
[AITemplate(Prompt = "Generate a structured product description.",
    ProvideDate = true, ProvideTimeZone = true)]
public sealed class ProductDescriptionRequest
{
    [AITemplateSegment(Purpose = SegmentPurpose.Request,
        Prompt = "Use the exact name without abbreviation.")]
    public required string Name { get; init; }
}
```

AITemplate also supports ProvideTime, FormatCulture and LanguageCulture.
SegmentPurpose is Return (default), Request or RequestAndReturn, not a caption
string or the outdated Output enum shown in some source XML examples.
Attributes describe prompt/schema intent, not provider consent or guaranteed JSON.
PromptBuilder (WarpToolkit.Desktop.AI.Parser) exposes GetTypePrompt,
GetRequestParameters(requestTemplate, indentLevel=0, indentation=2),
GetTypePromptInfo and **GetJSonSchema** (actual casing). GetPreamblePrompt is
private; old public-call examples are invalid. Prompt assembly includes the
preamble, response schema/reference instructions and per-property segments.

AIGenerated(guid, timeStamp, confidence) records generation provenance/diagnostics
on types/members; preserve it through Roslyn transformations and confidence reviews.
AITextResource and StringLiteralConverterExtension are retained lower-layer
resource/string-conversion capabilities; verify their installed namespace before use.

## Streaming parser

ProcessTokens consumes IAsyncEnumerable<ChatResponseUpdate> and returns an async
enumerable of processed strings. It invokes Task-returning metadata/paragraph/code
callbacks during enumeration:

```csharp
await foreach (string text in ReturnTokenParser.ProcessTokens(
    stream,
    onReceivedMetaDataAsyncFunc: e => Task.CompletedTask,
    onReceivedNextParagraphAsyncFunc: e => Task.CompletedTask,
    onCodeBlockInfoProvidedAsyncFunc: e => Task.CompletedTask))
{
    await appendOutputAsync(text);
}
```

This is an application method fragment; stream and appendOutputAsync are supplied
by its host. Calling ProcessTokens without enumerating does not run the parser.
Propagate cancellation through the upstream stream/callback work; do not invent an
explicit parser token overload. UI callbacks must marshal safely.

Metadata includes key/value/position/dedicated-line information.
Paragraph callbacks carry Paragraph/TextPosition/IsLastParagraph.
CodeBlockInfo includes Filename/Type/Description/Content; setting e.Handled=true
suppresses default handling when the host owns extraction/rendering.
CodeBlockFilename/Description/Type tokens are protocol constants. Parser supports
split token boundaries/inline code/metadata/fences; ordinary ChatProcessor already
integrates it, so do not run a second parser over the same UI stream.

## Observable history

Current WarpToolkit.Desktop.AI.ChatHistory.ObservableChatHistory derives from
ObservableCollection<ChatMessage>. Constructors accept empty, nonblank developer
prompt, prompt+user text or existing messages. It exposes Transcript metadata and
AddUserMessage/AddMessage.
SyncFrom(ChatResponse) uses prefix-merge semantics: preserves the leading
role/text-equal run, trims remaining local messages and appends response remainder.
It is not blindly “append one assistant response”; inspect whether the response
contains a full conversation or only a delta before choosing synchronization.

Wire calls use an appropriate detached List<ChatMessage> snapshot. UI-bound history
mutates only on its owning context; streaming work must not race that collection.
Persist transcript/conversation/model/usage identifiers intentionally.

## Historical component/history inventory

The original skill documented a previous four-layer stack with AIChatServiceBase
and OpenAIChatService/AzureOpenAIChatService/AnthropicChatService/OllamaChatService
(also provider families Google/OpenRouter/etc.). Those derived components were
Designer-droppable IChatClient implementations in AIChatServices; current source
replaces their application-facing pattern with AIChatClientComponent.

Historical options worth mapping, not silently dropping:
ModelName/DefaultChatModelName/DefaultDataModelName; sampling/token/penalty/stop
settings; ApiKeyGetter/KeyRetrievalService/ChatOptions; ConversationId/
DefaultConversationId. Current keys are resolved by registered factory/key-manager/
retrieval services, not a fabricated KeyRetrievalService property on the new component.

Historical AIChatHistory had AddMessage, ToChatMessages, Sync and an explicit string
serialization conversion. AIChatClient<TPersonality> combined an IChatClient,
typed personality and IAIChatHistory. These types are not established by the audited
current source; retain personality/history association and serialization semantics
in migration requirements, but use the installed API or an explicit adapter rather
than calling removed members.

Historical ChatView/ChatRendererControl/ChatProcessor/ChatViewVm descriptions also
included a writable DefaultDeveloperPrompt, ChatView.Chat and renderer
UpdateCurrentResponseAsync. Verify exact current renderer/persistence entry points;
do not mechanically replace them with guessed names or claim all old UI internals
remain. Any adapter must preserve streamed block granularity, metadata and persisted
conversation/provenance, not silently turn them into token-only display.
