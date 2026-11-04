---
name: winforms-designer-code
description: >-
  Creates, edits or repairs Designer-compatible settings dialogs, entry forms,
  preferences UserControls, .Designer.cs/.Designer.vb and InitializeComponent.
  Applies to lost Designer changes, failed roundtrips, component disposal,
  static control/event/binding serialization, new property metadata, and legacy
  C#/VB modernization. Uses the layout skill for font/DPI/resizing quality.
---

# Designer serialization and property metadata

## Durable rules

- Designer compatibility takes precedence over DRY/short generated code. Serialization support is not equivalent to what the language compiler accepts.
- Keep static UI in the Designer partial; constructor, runtime dynamic UI, business logic and async handlers in the regular partial.
- Preserve the target tooling's generated template and nullable context; don't claim a universal C# 2.0 or .NET-version language restriction.
- Create the components container **before** components that receive it; keep **one** `Dispose(bool)` override and ownership-correct cleanup.
- Verify a real Designer open/edit/save/reopen roundtrip, not merely compilation.

## Serialized structure

Instantiate components/controls; balance `BeginInit`/`EndInit` for `ISupportInitialize`; suspend nested layouts while configuring; write direct property assignments and named method event subscriptions; add controls/styles/bindings/resources; configure Form/UserControl properties near the end; resume inner-to-outer and perform required layout.

Supported generated calls include `Controls.Add`/`AddRange`, row/column style additions, `SetColumnSpan`, data bindings, resource application and layout/init methods. These are not forbidden simply because they are methods.

Avoid loops, conditional/computed configuration, helpers/factories, lambdas, local functions, `await`, target-typed control instantiation and object/collection-expression shortcuts inside serialized static `InitializeComponent`. Keep explicit generated assignments, even when repetitive. Runtime conditions and dynamically generated controls belong in regular code after initialization; do not pretend they will roundtrip as static Designer UI.

Main partial: constructor calls `InitializeComponent`; event handlers use nullable sender where the project enables NRTs. Designer partial: generated method, component cleanup, fields and generated infrastructure; no unrelated business properties/helpers. Typical generated nullable context is disabled; preserve what supported tooling emits rather than forcing annotations or banning them globally.

C#: named handler reference. VB: `Friend WithEvents` and `Handles` in main file when appropriate; preserve existing `AddHandler … AddressOf` patterns. Read [serialization examples](references/serialization.md) when writing a new partial, resource assignments or initialization pattern.

## Property policies

For a new property, define independent visibility, serialization and default behavior:

- Runtime/derived state: `[Browsable(false)]` plus `[DesignerSerializationVisibility(Hidden)]`; optionally `[EditorBrowsable(Never)]` for IntelliSense.
- Constant default: `[DefaultValue(...)]` matching actual initial value. Attribute does not initialize it.
- Ambient/dynamic default: `ResetXxx` + `ShouldSerializeXxx`; do not combine a competing `[DefaultValue]` policy.
- Nested mutable content/collections: `DesignerSerializationVisibility.Content` where a supported serializer handles their contents.
- Consumer help: `Category`/`Description`; IntelliSense visibility is separate from Properties-window visibility.

Read [metadata and Designer extensions](references/property-metadata.md) when exposing properties, dynamic theme defaults, converters/serializers or custom Designer UI. The SDK is for extension code, not a prerequisite for basic attributes.

## Diagnose regeneration/loss

Check unsupported expressions/helper calls, event lambdas, resource factories, partial-class names, dependency loading, component/init/layout ordering, property serialization defaults and nullable-template drift. Resource factories can compile and deserialize once yet be dropped on reserialization; use supported resources/serializers.

For a large screen, extract logical UserControls with their own Designer files rather than loops inside generated UI. No universal 5,000-line size or token-count threshold defines correctness. Base-Form visual inheritance can lock a TableLayoutPanel; consider composition when inherited editing is unsupported.

Layout property values and examples come from [layout](../winforms-high-dpi-fluent-layout/SKILL.md), not duplicated alternative button/anchor recipes here.

## Final checks

1. Compile and open/edit/save/reopen in supported Visual Studio Designer; mark unperformed roundtrips honestly.
2. One ownership-correct Dispose path, balanced init/layout and no dropped resources/events/properties.
3. Static UI remains declarative; runtime logic remains outside generated serialization.
