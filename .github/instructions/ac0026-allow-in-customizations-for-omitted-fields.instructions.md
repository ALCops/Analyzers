---
applyTo: 'src/ALCops.ApplicationCop/**/AllowInCustomizationsForOmittedFields*'
---

# AC0026: AllowInCustomizationsForOmittedFields

## Purpose

Detects table/table extension fields that are not placed on any page and do not have `AllowInCustomizations` explicitly set. Fields omitted from pages should declare `AllowInCustomizations = Always` (or `Never`) so page customizers know whether the field is intentionally hidden.

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `AC0026` |
| Category | Design |
| Severity | Info |
| Enabled by default | true |
| MessageFormat | `Field '{0}' is not added to any page and does not have the AllowInCustomizations property set.` |
| Version gate | `AddPageControlInPageCustomization` feature |
| netstandard2.1 | Full support (no net8.0-only APIs used) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Architecture | CompilationStart with lightweight index + lazy field resolution | Cross-object analysis split into two levels: (1) cheap table-to-page index at CompilationStart, (2) per-table field resolution deferred to AnalyzeSymbol via `ConcurrentDictionary<ITableTypeSymbol, Lazy<HashSet<IFieldSymbol>>>`. Tables that exit early never trigger expensive FlattenedControls materialization. |
| Data structure | `HashSet<ITableTypeSymbol>` + `Dictionary<..., List<IPageTypeSymbol>>` + `Dictionary<..., List<IPageExtensionTypeSymbol>>` + `ConcurrentDictionary<..., Lazy<HashSet<IFieldSymbol>>>` | Level 1: which tables have pages and which pages/extensions reference them (read-only after construction). Level 2: lazy per-table field cache, computed at most once per table on-demand. |
| Page extensions | Resolve via `ext.Target.GetTypeSymbol()` to `IPageTypeSymbol` | Extension's `.Target` is the extended page object; its `RelatedTable` gives the source table. |
| API pages | Excluded | API pages don't use AllowInCustomizations. |
| Table extensions with no page | Check `BaseTableHasLookupOrDrillDown` | Table extensions should still flag if the base table has LookupPageId or DrillDownPageId (implicit page usage). |
| Field filtering | User ID range, non-local/protected, non-FlowFilter, enabled, non-obsolete, supported types | Standard field filtering; unsupported types (Blob, Media, MediaSet, RecordId, TableFilter) are excluded. |
| OriginalDefinition for field comparison | `field.OriginalDefinition` cast to `IFieldSymbol` | Page controls reference field original definitions, not the field instances from table extensions. |
| Symbol equality | Default (reference) equality for `ITableTypeSymbol` keys | SDK uses reference equality within the same compilation's symbol set. |
| Skip obsolete | Yes | Standard ALCops convention. |

## Architecture

### Registration strategy

Uses `RegisterCompilationStartAction` to build page lookups once per compilation, then `RegisterSymbolAction` for `Table` and `TableExtension` symbols.

### Performance-critical design

**Problem:** The original implementation called `compilation.GetDeclaredApplicationObjectSymbols()` per table (~1500 times on the Base App), taking 8.6s. Phase 1 moved to a single call in CompilationStart but eagerly materialized `FlattenedControls` for all 2591 pages and iterated ~75k controls, costing 2.9s.

**Solution (two-level lazy architecture):**

**Level 1 — `BuildTableToPageIndex()` (CompilationStart, sequential):**
- Single `GetDeclaredApplicationObjectSymbols()` call
- For each page: record `table → List<IPageTypeSymbol>` and add table to `tablesWithPages`
- For each page extension: record `table → List<IPageExtensionTypeSymbol>`
- Does NOT access `FlattenedControls` or `AddedControlsFlattened` (deferred)
- Cost: ~500ms (just symbol iteration + `RelatedTable` resolution)

**Level 2 — `ResolveFieldsOnPages()` (AnalyzeSymbol, concurrent, lazy):**
- `ConcurrentDictionary<ITableTypeSymbol, Lazy<HashSet<IFieldSymbol>>>` caches per-table results
- Factory: iterates `tableToPages[table]` → `FlattenedControls` + `tableToPageExtensions[table]` → `AddedControlsFlattened`
- Computed at most once per table (thread-safe via `Lazy<T>` default mode)
- Parallelized across AnalyzeSymbol threads (4+ cores → ~125ms wall-clock)
- Tables that exit early (obsolete, no candidates, AllowInCustomizations set, etc.) never trigger computation

### Analysis flow

1. **CompilationStart**: `BuildTableToPageIndex()` builds lightweight index (no control iteration)
2. **PerSymbol (Table/TableExtension)**:
   - Version gate check, obsolete check, AllowInCustomizations on object check
   - Resolve to `ITableTypeSymbol` via `TryGetTableOrTargetTable()`
   - Get candidate fields via `GetCandidateFields()`
   - Check if table has pages (HashSet lookup)
   - For table extensions without pages, check `BaseTableHasLookupOrDrillDown()`
   - **Lazy resolve:** `fieldRefCache.GetOrAdd(table, Lazy<...>).Value` triggers field resolution only if needed
   - For each candidate field, check if it's referenced on any page (HashSet lookup)
   - Report diagnostic for unreferenced fields

### Concurrency safety

- `BuildTableToPageIndex` runs once before any `SymbolAction` callbacks. The `tableToPages` and `tableToPageExtensions` dictionaries are construction-complete and never modified after, safe for concurrent reads.
- `ConcurrentDictionary.GetOrAdd(key, valueFactory)` with `Lazy<T>` ensures single computation per table key.
- `Lazy<T>` with default `ExecutionAndPublication` mode ensures thread safety.
- `FlattenedControls` and `AddedControlsFlattened` are SDK `Lazy<ImmutableArray>` properties, safe for concurrent access.

## Test coverage

### HasDiagnostic (6 cases)

| Test case | Scenario |
|---|---|
| FieldOmittedPage | Field not on any page |
| ObsoleteStateNo | Non-obsolete field (ObsoleteState = No) flags |
| TableExtension | Field in table extension not on page |
| TableExtensionBaseDrillDownPageId | Table extension where base has DrillDownPageId |
| TableExtensionBaseLookupPageId | Table extension where base has LookupPageId |
| TableExtensionBaseTableHasAllowInCustomizations | Table extension where base has AllowInCustomizations |

### NoDiagnostic (11 cases)

| Test case | Suppression reason |
|---|---|
| AllowInCustomizationsIsSet | AllowInCustomizations already set on field |
| AllowInCustomizationsOnField | AllowInCustomizations set at field level |
| AllowInCustomizationsOnTable | AllowInCustomizations set at table level |
| AllowInCustomizationsOnTableExtension | AllowInCustomizations set at table extension level |
| DisabledField | Field has Enabled = false |
| FieldOnPage | Field is placed on a page |
| FieldTypeNotSupported | Field type is Blob/Media/etc. |
| FlowFilterField | FlowFilter field class |
| ObsoleteStatePending | Obsolete field (pending) |
| ObsoleteStateRemoved | Obsolete field (removed) |
| TableExtension | Table extension field that is on a page |

## Relationship to Microsoft's AS0138

Microsoft's `RuleUseAllowInCustomizationsProperty` (AS0138 in AppSourceCop) is simpler: it only checks if AllowInCustomizations is set on fields, without the page-reference cross-check. AC0026 is more sophisticated, only flagging fields that are NOT on any page.
