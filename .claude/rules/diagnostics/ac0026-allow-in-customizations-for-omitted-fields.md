---
paths:
  - "src/ALCops.ApplicationCop/**/AllowInCustomizationsForOmittedFields*"
  - "src/ALCops.ApplicationCop.Test/Rules/AllowInCustomizationsForOmittedFields/**"
---

# AC0026: AllowInCustomizationsForOmittedFields

## Purpose

Detects table/table extension fields that are not placed on any page and do not have `AllowInCustomizations` explicitly set. Fields omitted from pages should declare `AllowInCustomizations = Always` (or `Never`) so page customizers know whether the field is intentionally hidden.

Registers `RegisterCompilationStartAction` (table-to-page index) with an inner `RegisterSymbolAction` on `Table` and `TableExtension`; main type `AllowInCustomizationsForOmittedFields`.

## Design decisions

| Decision | Rationale |
|---|---|
| Version-gated on the `AddPageControlInPageCustomization` feature, no netstandard2.1 stub | `AllowInCustomizations` only exists on runtimes that have the feature; every TFM compiles the same code. |
| Two-level lazy index: a cheap table-to-pages/page-extensions map at CompilationStart, and per-table field resolution deferred to the symbol callback through a `ConcurrentDictionary<ITableTypeSymbol, Lazy<HashSet<IFieldSymbol>>>` | Calling `GetDeclaredApplicationObjectSymbols()` per table cost 8.6s on the Base App, and eagerly materializing `FlattenedControls` for all 2591 pages still cost 2.9s. Tables that exit early never pay for control materialization, and the remaining work runs in parallel across symbol callbacks. |
| Fields placed by a page extension (`AddedControlsFlattened`) count as placed | The extension's `Target` is the extended page whose `RelatedTable` identifies the source table. |
| API pages are not counted as placements | API pages do not use `AllowInCustomizations`. |
| Table-extension fields are flagged even without a page when the base table declares `LookupPageId` or `DrillDownPageId` | Those properties imply page usage. |
| Cross-checks page placement, unlike AppSourceCop AS0138 (`RuleUseAllowInCustomizationsProperty`) | AS0138 flags every field without `AllowInCustomizations`; AC0026 only flags fields that appear on no page. |

## Deliberate non-reports

- Obsolete tables and extensions, and objects that already set `AllowInCustomizations` at table/tableextension level.
- Fields outside the user ID range, local or protected fields, FlowFilters, disabled or obsolete fields, and unsupported types (`Blob`, `Media`, `MediaSet`, `RecordId`, `TableFilter`).

## SDK facts

- Page controls reference a field's `OriginalDefinition`, not the field instance seen through a table extension; compare on `field.OriginalDefinition as IFieldSymbol`.
- `ITableTypeSymbol` keys use default reference equality, which is stable within one compilation's symbol set.
- `FlattenedControls` and `AddedControlsFlattened` are SDK `Lazy<ImmutableArray>` properties and are safe to read concurrently.

## Test notes

- Table-extension fixtures are gated on 13.0 (same-module target); the fixtures with object-level `AllowInCustomizations` are gated on 16.0.
