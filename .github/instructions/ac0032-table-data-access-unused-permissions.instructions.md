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

Uses a per-object `RegisterSyntaxNodeAction` pattern for self-contained analysis. Each application object is analyzed atomically within a single callback, eliminating shared mutable state.

```
src/ALCops.ApplicationCop/
├── Analyzers/
│   └── TableDataAccessUnusedPermissions.cs           # Analyzer (SyntaxNodeAction on object kinds)
└── CodeFixes/
    └── TableDataAccessUnusedPermissionsCodeFixProvider.cs  # CodeFix (remove entry / reduce chars / remove property)

src/ALCops.Common/
└── Permissions/
    └── RequiredPermissionDetector.cs   # Shared detection logic (also used by AC0031)
```

### Analysis flow

**Registration (`Initialize`):**
- `RegisterSyntaxNodeAction` on 9 application object syntax kinds (CodeunitObject, TableObject, TableExtensionObject, PageObject, PageExtensionObject, ReportObject, ReportExtensionObject, QueryObject, XmlPortObject)

**Per-object analysis (`AnalyzeApplicationObject`):**
1. `GetDeclaredSymbol(ctx.Node)` to obtain `IApplicationObjectTypeSymbol` (early exit if not)
2. Skip PermissionSet/PermissionSetExtension, obsolete objects, test codeunits with permissions disabled
3. Get `Permissions` property (early exit if null, covers ~70% of objects)
4. **Collect DB invocations** (`CollectFromInvocations`):
   - Build object-scope record map (`objectScopeRecordMap`) from `containingObject.GetMembers()`: global vars, report data items (via `GetTypeSymbol()`), xmlport table elements (via `FlattenedNodes` + `GetTypeSymbol()`)
   - Walk `ctx.Node.DescendantNodes()` for `MethodOrTriggerDeclarationSyntax`
   - Skip obsolete methods via `GetDeclaredSymbol` + `IsObsolete()`
   - For each method body: syntax pre-filter (`HasPossibleDbInvocation`)
   - Build per-method record variable map from `IMethodSymbol.LocalVariables` + `.Parameters`
   - Walk method body for both `InvocationExpressionSyntax` and standalone `MemberAccessExpressionSyntax` (method calls without parentheses)
   - Unified `TryGetPermissionFromDbAccess` extracts method name + receiver from either form, resolves via variable map (fast path), falls back to `GetSymbolInfo` for complex receivers
5. **Collect data items** (`CollectFromDataItems`):
   - Iterate `containingObject.GetMembers()` for ReportDataItem, QueryDataItem, XmlPortNode symbols
   - For XmlPortNode: also iterate `FlattenedNodes` to reach nested table elements
   - Use `RequiredPermissionDetector.TryGetFrom*` methods
6. Compare declared entries against collected permissions, report diagnostics

### Key methods

| Method | Purpose |
|---|---|
| `AnalyzeApplicationObject` | Entry point; checks Permissions, orchestrates collection and reporting |
| `CollectFromInvocations` | Builds object-scope record map (vars + data items), walks method bodies, resolves DB calls via unified handler |
| `TryGetPermissionFromDbAccess` | Unified resolution for both syntax forms (with/without parentheses): pattern-matches to extract method name + receiver, uses variable-map fast path, falls back to `TryGetPermissionViaSymbolInfo` |
| `TryGetPermissionViaSymbolInfo` | Fallback for complex receivers: uses GetSymbolInfo on the node and receiver expression to resolve method and receiver type |
| `CollectFromDataItems` | Iterates report/query FlattenedDataItems and xmlport FlattenedXmlPortNodes (all via reflection) for implicit read permissions |
| `AddXmlPortNodeToVarMap` | Adds an xmlport table element to the object-scope record map if it references a non-temporary table |
| `HasPossibleDbInvocation` | Syntax pre-filter: checks if body has any invocation name matching a DB operation (handles both syntax forms) |
| `AnalyzePermissionEntry` | Compares one declared entry against collected required permissions |
| `PermissionMatchesTable` | Matches identifier/qualified/objectId syntax against `ITableTypeSymbol` |

### Threading model

Each `SyntaxNodeAction` callback is self-contained with no shared mutable state. The compiler may parallelize callbacks across different objects, but each object's analysis uses only local variables (`List<RequiredPermission>`). No `ConcurrentDictionary` or cross-callback communication needed.

## Design decisions

| Decision | Rationale |
|---|---|
| `RegisterSyntaxNodeAction` on object kinds | Self-contained per-object analysis; no CompilationStart/End coupling; gives SemanticModel directly |
| Variable map + syntax resolution | Resolves ~66% of DB calls via dictionary lookup from `IMethodSymbol.LocalVariables`/`.Parameters`; avoids expensive `GetOperation` calls entirely |
| Global variable map from `GetMembers()` | Captures object-level Record variables that account for ~34% of invocations not resolvable from locals/params |
| Data items in object-scope record map | Report data items and xmlport table elements act as implicit record variables in trigger code; added to the same map for fast-path resolution via `GetTypeSymbol()` |
| XmlPort nested nodes via `GetFlattenedXmlPortNodes` | `GetMembers()` returns only top-level schema nodes; `IXmlPortNodeSymbol.FlattenedNodes` only returns immediate children (not recursive). Uses reflection on the internal `SourceXmlPortTypeSymbol.FlattenedNodes` property which truly flattens all depths |
| `GetSymbolInfo` fallback for complex receivers | Handles function return values, array indexing, property access; only ~1% of invocations need this path |
| No `GetOperation` / `OperationWalker` | `GetOperation` in `SyntaxNodeAction` costs ~0.3ms/call (no pre-computed cache); variable map approach is 4.5x faster |
| No cross-callback shared state | Eliminates the fragile two-phase accumulator pattern that caused false positives during incremental compilation |
| Iterate `DescendantNodes()` for methods | Finds all MethodOrTriggerDeclarationSyntax in the object, skip obsolete ones |
| Skip obsolete methods via symbol | `GetDeclaredSymbol` + `IsObsolete()` on the method symbol |
| Syntax pre-filter per method body | `HasPossibleDbInvocation` checks method names against MethodOperationMap before expensive analysis |
| Data items via `GetMembers()` | Direct member iteration replaces separate `RegisterSymbolAction` callbacks |
| Report nested data items via `FlattenedDataItems` | `IReportTypeSymbol.FlattenedDataItems` (public API) recursively includes all nested data items; fixes false positives on nested report structures |
| Query nested data items via reflection | `IQueryTypeSymbol` doesn't expose `FlattenedDataItems`; access the internal `QueryTypeSymbol.FlattenedDataItems` via `PropertyAccessor.GetPropertyIfExists` (consistent with project reflection patterns) |
| Named return values included in localRecordVarMap | AL named return values act as implicit local variables; only added when `ReturnValueSymbol.IsNamed == true` to avoid issues with unnamed returns |
| No CompilationEnd needed | Eliminates the fragile two-phase pattern that caused false positives |
| Page SourceTable exemption unchanged | Same logic, just moved into per-object callback |
| System tables included in collection | AC0032 passes `includeSystemTables: true` to `RequiredPermissionDetector` so that declared permissions on system tables are matched against actual accesses |
| Two descriptors sharing one ID | Same conceptual rule, different message clarity |
| `PermissionMatchesTable` duplicated from `PermissionResolver` | Avoids coupling; operates on syntax nodes, not resolved symbols |
| Temporary records NOT exempted | Declaring permissions on temp-only tables is dead code |
| Skip permissionset/permissionsetextension objects | These objects declare permissions as their core purpose |

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

**HasDiagnostic (10 cases):** EntireEntryUnused, PartialCharsUnused, MultipleUnusedEntries, NoCodeInCodeunit, UnusedOnReport, UnusedOnQuery, UnusedOnXmlPort, TemporaryRecord, ParameterPartialUnused, ReportDataItemPartialUnused.
**NoDiagnostic (25 cases):** AllPermissionsUsed, PageSourceTable, TestCodeunitDisabled, ReadUsed, ReportDataItemRead, QueryDataItemRead, PermissionSet, PermissionSetExtension, SystemTable, ParameterOperations, UppercasePermissions, ParameterAllOperations, LocalVarSpacedTable, GlobalVarSpacedTable, ReportDataItemModify, ReportDataItemAliasModify, XmlPortTableElementModify, XmlPortNestedTableElementModify, ReturnParameterRead, ReportNestedDataItemRead, QueryNestedDataItemRead, MethodWithoutParenthesesCount, MethodWithoutParenthesesFindFirst, MethodWithoutParenthesesIsEmpty, MethodWithoutParenthesesChained.
**HasFix (4 cases):** RemoveEntireEntry, ReduceChars, RemoveEntireProperty, ReplaceChars.

## Known limitations

1. **Extension objects**: Cannot see base object code; may flag permissions needed by the base as unused
2. **CalcFields/CalcSums**: Indirect table access through FlowFields is not traced
3. **InherentPermissions overlap**: Table-level `InherentPermissions` may make an object-level entry redundant, but the analyzer does not flag this (different concern from unused)
4. **Cross-object calls**: If codeunit A calls codeunit B, and B accesses a table, A's permission for that table appears unused (correct, because permissions don't flow through the call stack)

## Design decisions (continued)

| Decision | Rationale |
|---|---|
| Handle `MemberAccessExpressionSyntax` without parent `InvocationExpressionSyntax` | AL allows method calls without parentheses (e.g., `MyTable.Count`); the parser produces `MemberAccessExpressionSyntax` instead of `InvocationExpressionSyntax`. Unified in `TryGetPermissionFromDbAccess` which pattern-matches both forms at entry, then uses a single resolution path. The `HasPossibleDbInvocation` pre-filter also checks both forms. |
