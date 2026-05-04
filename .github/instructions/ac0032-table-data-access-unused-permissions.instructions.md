---
applyTo: 'src/ALCops.ApplicationCop/**/TableDataAccessUnusedPermissions*'
---

# AC0032: Unused Permissions

## Purpose

Detects `Permissions` property entries that have no corresponding table data access in the object. This is the inverse of AC0031 (which detects missing permissions). Reports an Info-level diagnostic per unused permission entry. Unused permissions are dead code because the `Permissions` property only applies to the object that directly accesses the table (no call-stack inheritance).

## Diagnostic properties

| Property | Value |
|---|---|
| ID | AC0032 |
| Category | Design |
| Severity | Info |
| Help URI | https://alcops.dev/docs/analyzers/applicationcop/ac0032/ |

Two `DiagnosticDescriptor` instances share the same ID but have different message formats:
- `TableDataAccessUnusedPermissionsEntireEntry`: no database operations found on the table
- `TableDataAccessUnusedPermissionsPartialChars`: table accessed but not all declared RIMD chars are needed

## Architecture

Uses a `CompilationStartAction` closure pattern for two-phase analysis: parallel collection followed by sequential analysis. All actions share state via closures captured in `OnCompilationStart`.

```
src/ALCops.ApplicationCop/
├── Analyzers/
│   └── TableDataAccessUnusedPermissions.cs           # Analyzer (CompilationStart + CodeBlock + CompilationEnd)
└── CodeFixes/
    └── TableDataAccessUnusedPermissionsCodeFixProvider.cs  # CodeFix (remove entry / reduce chars / remove property)

src/ALCops.Common/
└── Permissions/
    └── RequiredPermissionDetector.cs   # Shared detection logic (also used by AC0031)
```

### Analysis flow

**Registration (`Initialize` + `OnCompilationStart`):**
- `RegisterCompilationStartAction` creates a shared `ConcurrentDictionary` accumulator
- From within `CompilationStartAction`, registers `CodeBlockAction`, `SymbolAction` (x3), and `CompilationEndAction`
- All callbacks capture the accumulator via closure (guaranteed same instance)

**Phase 1 (parallel, compiler-driven):**
1. `RegisterCodeBlockAction`: For each method/trigger, check if containing object has `Permissions` property (early exit for ~70% of callbacks). Syntax-level pre-filter scans for DB method names before the expensive `GetOperation` call (eliminates ~75% of remaining methods). Call `GetOperation(body)` once per method, then walk the operation tree via `InvocationCollectorWalker` (extends `OperationWalker`).
2. `RegisterSymbolAction` (ReportDataItem, QueryDataItem, XmlPortNode): Collect data item permissions into the same accumulator.

**Phase 2 (sequential):**
3. `RegisterCompilationEndAction`: Iterate objects with `Permissions` property, look up accumulated data, compare declared vs. required, report diagnostics.

### Key methods

| Method | Purpose |
|---|---|
| `OnCompilationStart` | Creates shared accumulator, registers all inner actions with closures |
| `CollectFromCodeBlock` | Phase 1 entry; syntax pre-filter, then `GetOperation` + `InvocationCollectorWalker` |
| `InvocationCollectorWalker` | `OperationWalker` subclass; visits `IInvocationExpression` nodes |
| `CollectFromReportDataItem/QueryDataItem/XmlPortNode` | Phase 1; data item collection via `RegisterSymbolAction` |
| `AnalyzeCompilation` | Phase 2; iterates objects, reads accumulated data, reports diagnostics |
| `AnalyzePermissionEntry` | Compares one declared entry against collected required permissions |
| `PermissionMatchesTable` | Matches identifier/qualified/objectId syntax against `ITableTypeSymbol` |

### Threading model

Phase 1 callbacks run in parallel (compiler-managed thread pool). `ConcurrentDictionary` + `ConcurrentBag` ensure thread safety. Shared state is captured via closures from `CompilationStartAction` (no `ConditionalWeakTable` needed). Object keys use `$"{Kind}:{Id}"` strings for safe identity across different symbol instances.

Phase 2 (`RegisterCompilationEndAction`) runs once after all Phase 1 callbacks complete. Reads are sequential, no concurrent writes.

## Design decisions

| Decision | Rationale |
|---|---|
| `CompilationStartAction` + `CompilationEndAction` closure pattern | Guarantees shared state across CodeBlockAction, SymbolAction, and CompilationEndAction via closures. `ConditionalWeakTable<Compilation>` does NOT work because `Compilation` instances differ across action types. |
| `RegisterCodeBlockAction` + `OperationWalker` | Amortizes `GetOperation` cost: one call per method body instead of per invocation node. Compiler parallelizes callbacks. |
| Syntax-level pre-filter before `GetOperation` | Scans method body for DB method names (Find, Get, Insert, etc.) via syntax tree walk. Eliminates ~75% of `GetOperation` calls. `GetOperation` dominates cost at ~0.1ms each. |
| `OperationWalker` for invocation collection | SDK-provided visitor pattern; only visits `IInvocationExpression` nodes efficiently |
| String keys (`Kind:Id`) instead of symbol reference equality | Symbol instances from `GetContainingApplicationObjectTypeSymbol()` and `GetDeclaredApplicationObjectSymbols()` may differ |
| Early exit on `Permissions` property check in CodeBlockAction | ~70% of callbacks are in objects without `Permissions`; avoids `GetOperation` call entirely |
| `RegisterSymbolAction` for data items | Compiler handles member enumeration; simpler than manual iteration |
| Two descriptors sharing one ID | Same conceptual rule, different message clarity |
| `PermissionMatchesTable` duplicated from `PermissionResolver` | Avoids coupling; operates on syntax nodes, not resolved symbols |
| Page SourceTable exemption | Pages implicitly need RIMD on their source table |
| Temporary records NOT exempted | Declaring permissions on temp-only tables is dead code |
| Skip permissionset/permissionsetextension objects | These objects declare permissions as their core purpose, not as code-access declarations; flagging them is always a false positive |
| `DeclaredPermissionSet` reused for RIMD tracking | Existing type from AC0031's permission module |

## CodeFix

The `TableDataAccessUnusedPermissionsCodeFixProvider` removes or reduces unused permission entries. Supports FixAll.

### Scenarios

| Scenario | Behavior |
|---|---|
| Entire entry unused, other entries remain | Remove the entry from the permission list |
| Entire entry unused, only entry | Remove the entire `Permissions` property |
| Partial chars unused | Replace permission chars with only the used subset |

### Data passing (analyzer -> CodeFix)

`ImmutableDictionary<string, string>` properties on the diagnostic:
- `TableName`: table name as written in the permission declaration
- `UnusedChars`: chars to remove (e.g., "imd")
- `UsedChars`: chars to keep (e.g., "r"); empty string when entire entry is unused

### Node finding

`syntaxRoot.FindNode(span)` may return `PermissionPropertyValueSyntax` instead of `PermissionSyntax` when there is only one entry (identical spans). The code fix handles this by searching descendants:
```csharp
var permissionNode = node as PermissionSyntax
    ?? node.FirstAncestorOrSelf<PermissionSyntax>()
    ?? node.DescendantNodes().OfType<PermissionSyntax>().FirstOrDefault();
```

### Trivia handling

When removing the first entry from a multi-entry list, `SeparatedSyntaxList.Remove` preserves the second entry's leading trivia (newline + indent). The code fix strips this trivia to avoid `Permissions =\n              tabledata ...` artifacts.

## Test coverage

**HasDiagnostic (8 cases):** EntireEntryUnused, PartialCharsUnused, MultipleUnusedEntries, NoCodeInCodeunit, UnusedOnReport, UnusedOnQuery, UnusedOnXmlPort, TemporaryRecord.
**NoDiagnostic (8 cases):** AllPermissionsUsed, PageSourceTable, TestCodeunitDisabled, ReadUsed, ReportDataItemRead, QueryDataItemRead, PermissionSet, PermissionSetExtension.
**HasFix (3 cases):** RemoveEntireEntry, ReduceChars, RemoveEntireProperty.

## Known limitations

1. **Extension objects**: Cannot see base object code; may flag permissions needed by the base as unused
2. **CalcFields/CalcSums**: Indirect table access through FlowFields is not traced
3. **InherentPermissions overlap**: Table-level `InherentPermissions` may make an object-level entry redundant, but the analyzer does not flag this (different concern from unused)
4. **Cross-object calls**: If codeunit A calls codeunit B, and B accesses a table, A's permission for that table appears unused (correct, because permissions don't flow through the call stack)
