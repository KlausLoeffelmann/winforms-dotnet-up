---
name: winforms-databinding
description: >-
  Implements customer-entry save validation, search-as-you-type, bound
  preferences, master/detail customers and orders, selected combo values and
  grid/list updates. Fixes BindingSource currency, missing notifications,
  null/empty records, conversion, bulk-update performance and disposal.
  Applies to BindingList or ObservableCollection adaptation and supported
  Visual Studio object-data-source discovery.
---

# Binding, currency and validation

## Durable rules

- Use `BindingSource` for currency/mediation and `BindingList<T>` for WinForms collection notifications; plain `List<T>` changes are not automatically announced.
- Mutable bound items notify actual changes through `INotifyPropertyChanged`; bound collection and property notifications are UI-affine.
- Choose update direction/timing deliberately and validate/commit before save; do not read a stale model.
- Collection adapters must declare mutation direction, supported events, UI affinity and disposal ownership.
- Null/no-record is a valid state, not permission to silently create and save a replacement object.

## Core decisions

| Requirement | Mechanism/caveat |
|---|---|
| Property updates | Change-only `PropertyChanged`, preferably CallerMemberName or Toolkit-generated |
| Add/remove updates | `IBindingList`/`BindingList<T>`; item notifications require notifying items |
| Designer property discovery | `BindingSource.DataSource=typeof(Model)` in static UI; set actual instance/list at runtime |
| Live search | `DataSourceUpdateMode.OnPropertyChanged` |
| Entry commit | `OnValidation`; validation is not identical to every focus change |
| Read-only/calculated display | `Never`; binding creation can still initially write, and explicit `WriteValue` is possible |
| Formatting/conversion | `FormattingEnabled`, `Format` source→control, `Parse` control→source; handle culture and failures |
| Current record/detail | BindingSource currency/DataMember, not invented grid `SelectedItem` |
| Sorting/filtering | Underlying source must support them; basic BindingList is not a general sorted/filterable collection |

Read [binding recipes](references/binding-recipes.md) for notifying/validating models, actual binding statements, datasource XML, master/detail, combo values, formatting, grid virtual mode and save flow.

Object binding is a suitable modern default. Preserve functioning typed DataSet runtime behavior during migration; modern .NET Designer tooling differs from .NET Framework's Data Sources Window/typed-dataset designers. `.datasource` helps supported **Visual Studio** tooling discover a built assembly/type; it does not create a WinForms Designer in VS Code.

## Shared collections and lifetime

`ObservableCollection<T>` uses `INotifyCollectionChanged`, not WinForms `IBindingList`. Read [one-way adapter](references/observable-adapter.md) when bridging shared ViewModels. Its collection structure is read-only from the binding side; source Add/Remove/Replace/Move/Reset are mirrored in order. Mutable items can still be edited. Commands mutate the source explicitly.

Detach long-lived source/event subscriptions, clear view sources as needed, and dispose owning components/adapters. `BindingList<T>` itself has no virtual Dispose method. Use the component container for BindingSource/ErrorProvider and one generated Form cleanup path under [Designer](../winforms-designer-code/SKILL.md).

## Validation and performance

Prefer non-trapping field feedback plus form-level save validation. `ErrorProvider` can consume `IDataErrorInfo`; supply its Form/container ownership. `e.Cancel` is a supported mechanism, but `AutoValidate` determines focus behavior. Choose an explicit policy such as `EnableAllowFocusChange`, keep Cancel reachable with `CausesValidation=false`, and prevent affirmative close/save until errors are resolved.

Before save: validate controls, write relevant bindings, observe conversion errors, `EndEdit` where appropriate, validate model/domain rules, then persist. Don't blanket-force writeback on every read or combine manual state copying with binding for the same property.

For bulk changes, suppress list notifications intentionally and restore/reset them once in `finally`; `SuspendBinding` alone does not universally suppress grid list notifications. Paging/cache/virtual mode should follow measured cost and required editing semantics, not a hard 10,000-row threshold.

Troubleshoot direction separately: UI not refreshing needs item/collection notification and actual source checks; source not updating needs validation/update-mode/writeback/conversion checks. Test replacing/null sources and repeated bindings for leaked subscriptions.

## Final checks

1. Verify initial/changed/null records, selected values and master/detail currency.
2. Test conversion errors, focused-field save, Cancel navigation and model validation.
3. Exercise collection mutations/order, bulk refresh, disposal and thread ownership.
