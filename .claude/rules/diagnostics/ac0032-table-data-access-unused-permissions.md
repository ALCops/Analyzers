---
paths:
  - "src/ALCops.ApplicationCop/**/TableDataAccessUnusedPermissions*"
---

# AC0032: Unused Permissions

## Purpose

Detects `Permissions` property entries that have no corresponding table data access in the object. This is the inverse of AC0031 (which detects missing permissions). Reports an Info-level diagnostic per unused permission entry. Unused permissions are dead code because the `Permissions` property only applies to the object that directly accesses the table (no call-stack inheritance).

## Design decisions

| Decision | Rationale |
|---|---|
| `RegisterSyntaxNodeAction` on object kinds | Self-contained per-object analysis; no CompilationStart/End coupling; gives SemanticModel directly |
| Variable map + syntax resolution | Resolves ~66% of DB calls via dictionary lookup from `IMethodSymbol.LocalVariables`/`.Parameters`; avoids expensive `GetOperation` calls entirely |
| Global variable map from `GetMembers()` | Captures object-level Record variables that account for ~34% of invocations not resolvable from locals/params |
| Data items in object-scope record map | Report data items and xmlport table elements act as implicit record variables in trigger code; added to the same map for fast-path resolution via `GetTypeSymbol()` |
| XmlPort nested nodes via `GetFlattenedXmlPortNodes` | `GetMembers()` returns only top-level schema nodes; `IXmlPortNodeSymbol.FlattenedNodes` only returns immediate children (not recursive). Uses reflection on the internal `SourceXmlPortTypeSymbol.FlattenedNodes` property which truly flattens all depths |
| `GetSymbolInfo` fallback for complex receivers | Handles function return values, array indexing, property access; only ~1% of invocations need this path |
| Hot path avoids `GetOperation` / `OperationWalker` | `GetOperation` in `SyntaxNodeAction` costs ~0.3ms/call (no pre-computed cache); the variable-map fast path is 4.5x faster and handles the common identifier receivers. `GetOperation` is used only for the rare non-identifier receivers (`this.`, expression receivers), reading the receiver type off the base `IOperation.Type` |
| Four receiver forms handled explicitly | Database access can target a record via four syntactic forms: `MyTable.Modify()`, `Rec.Modify()`, bare `Modify()` (implicit self), and `this.Modify()` (AL `this` keyword). Variable receivers use the fast path; bare self resolves the containing `ITableTypeSymbol`; `this` (and other expression receivers) resolve via `IOperation.Type` -- the same operation-tree mechanism AC0031 uses, which works on all TFMs (it never references the `ThisExpressionSyntax` type, absent at the netstandard2.1 floor). Missing the `this` form caused the AC0032 false positive in issue #343 |
| Self-table symbol shapes | The table object's declared symbol is an `ITableTypeSymbol` (not an `IRecordTypeSymbol`); `this`/`Rec` resolve to a separate `IRecordTypeSymbol` wrapper. `TryGetPermissionForType` accepts both: record types via `OriginalDefinition`, table types directly |
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
| Two descriptors sharing one ID | Same conceptual rule, different message clarity. `TableDataAccessUnusedPermissionsEntireEntry`: no database operations found on the table; `TableDataAccessUnusedPermissionsPartialChars`: table accessed but not all declared RIMD chars are needed |
| `PermissionMatchesTable` duplicated from `PermissionResolver` | Avoids coupling; operates on syntax nodes, not resolved symbols |
| `Next` counts as a Read (issue #466) | `Next()` advances the server-side cursor and fetches the next row, so it consumes `r` in the object that calls it. Adding it to `MethodOperationMap` fixes the false positive where the caller only iterates (`repeat … until Rec.Next() = 0`) a set that a helper object positioned via `FindSet`: permissions don't flow through the call stack, so the iterating object genuinely needs its own `r`. `RecordRef.Next()` needs no extra code - once `Next` is in the map it is picked up by the `HasPossibleDbInvocation` pre-filter and the existing RecordRef whole-object bailout. |
| Temporary tables NOT counted as permission users (all implementations) | Temporary tables never touch the database, so a permission declared for a table accessed ONLY via temporary records is dead code and IS flagged as unused. All three implementations plus report/xmlport `UseTemporary` are excluded from the "used" collection via the `IRecordTypeSymbol.IsTemporary()` / `ITableTypeSymbol.IsTemporary()` extensions: applied to global var map, data-item map, locals, params, named return, `TryGetPermissionForType` (record + table-type branches), `AddXmlPortNodeToVarMap`, and the `GetSymbolInfo` fallback. The `TableType = Temporary` case needs the explicit `TableType` check because `IRecordTypeSymbol.Temporary` reflects only the `temporary` keyword. |
| Skip permissionset/permissionsetextension objects | These objects declare permissions as their core purpose |
| RecordRef whole-object bailout (issue #420) | A DB operation (any `MethodOperationMap` entry, reads included) on a `RecordRef` receiver can target any table at runtime; static analysis cannot tell which declared permission it consumes. AC0032 is therefore disabled for the entire object (silent, no diagnostic). Operation-granular suppression (only suppress the matching permission char) was considered and rejected in favor of simplicity. Detection: RecordRef-typed globals/locals/params/named returns tracked by name (fast path, `NavTypeKind == RecordRef`), complex receivers via `IOperation.Type.NavTypeKind`, plus the `GetSymbolInfo` fallback. Without this, the FixAll CodeFix removed permissions that were required at runtime |
| Fast-path lookup honors AL scoping | Variables are classified by symbol type, never by name, so `RecordRef: Record Customer` tracks Customer normally. Because locals shadow globals, the receiver lookup consults the full local scope (RecordRef name set, then record map) before the object scope; otherwise a global `RecordRef: RecordRef` shadowed by a local record variable of the same name would falsely trigger the bailout |
| `DataTransfer` executors count as table operations (issue #465) | `CopyFields` reads the source and modifies the destination, `CopyRows` reads the source and inserts into the destination — real database work that the object needs `r`/`m`/`i` for. Because the tables are arguments to `SetTables` and not the receiver, the whole construct was invisible to the receiver-keyed resolution and every permission held only for a `DataTransfer` was reported as unused. The other seven `DataTransfer` methods only build the transfer definition and are not operations. |
| Separate `DataTransferOperations`, not `MethodOperationMap` | `MethodOperationMap` is keyed on built-in methods invoked *on a record receiver* and is mirrored by `RecordMethodClassification`; adding `CopyFields`/`CopyRows` there would make any same-named call on a record look like a DB operation and would leak into the record-method classification. The `DataTransfer` set is consulted only after the receiver is confirmed to be a `DataTransfer`. |
| Resolution scope is the same method or trigger body | The `SetTables` call must sit in the same body as the executor. Following the variable across procedures would need call-graph analysis and cross-callback state, which the no-shared-state design rules out. A `DataTransfer` configured in one procedure and executed in another therefore falls to the bailout rather than being resolved. One `DataTransferTableResolver` is built per body and reused for every executor in it, held in a callback-local so nothing crosses callbacks. |
| Pairing is flow-sensitive with strict reset | Reconfiguring one `DataTransfer` variable for several sequential copies (`SetTables; CopyRows; SetTables; CopyFields`) is the standard upgrade-codeunit pattern, so a `SetTables` *replaces* the pending pair for that variable on the path rather than adding to it, and each executor is attributed only to the pair that reaches it. The earlier union over every `SetTables` in the body over-granted (it made permissions the code never needs look used) and hid true positives such as an `i` on a destination that only `CopyFields` touches. An executor consumes the pending pairs without clearing them, so a second executor after the same `SetTables` transfers the same tables again. Branches fork and merge like PC0030's `SetLoadFieldsWalker`, and a merge is the **union** of the branches' pairs — the conservative direction for AC0032, which can only make a permission look used. |
| Receiver may be bare or `this.`-qualified | The executor and the `SetTables` calls are matched by the variable name the receiver resolves to, and both `MyDataTransfer.CopyFields()` and `this.MyDataTransfer.CopyFields()` yield the bare name — so a `SetTables` written one way still matches an executor written the other. The self-reference is recognized through `EnumProvider.OperationKind.ThisReference` on the operation tree, never `ThisExpressionSyntax`/`SyntaxKind.ThisExpression` (absent at the netstandard2.1 floor). Any other receiver shape is unresolvable. |
| Only `Database::"X"` literals resolve | The argument operation is an `IApplicationObjectAccess` (optionally wrapped in `IConversionExpression`), which names the table directly. Constant propagation of integer locals was rejected: it is unbounded and the bailout already covers the case safely. |
| Unresolvable `DataTransfer` triggers the whole-object bailout | Same reasoning as the RecordRef bailout (#420): no `SetTables` reaching the executor on any path, a reaching `SetTables` with a non-literal table argument, or a receiver that is neither a plain identifier nor `this.<variable>` all mean the executor may touch any table, so no declared permission in the object can be proven unused. Silent, no diagnostic. |
| Handle `MemberAccessExpressionSyntax` without parent `InvocationExpressionSyntax` | AL allows method calls without parentheses (e.g., `MyTable.Count`); the parser produces `MemberAccessExpressionSyntax` instead of `InvocationExpressionSyntax`. Unified in `TryGetPermissionFromDbAccess` which pattern-matches both forms at entry, then uses a single resolution path. The `HasPossibleDbInvocation` pre-filter also checks both forms. |

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
    ├── RequiredPermissionDetector.cs   # Shared detection logic (also used by AC0031)
    ├── DataTransferOperations.cs       # DataTransfer executors, kept out of MethodOperationMap
    └── DataTransferTableResolver.cs    # Flow-sensitive SetTables <-> executor pairing per body
```

### Analysis flow

**Registration (`Initialize`):**
- `RegisterSyntaxNodeAction` on 9 application object syntax kinds (CodeunitObject, TableObject, TableExtensionObject, PageObject, PageExtensionObject, ReportObject, ReportExtensionObject, QueryObject, XmlPortObject)

**Per-object analysis (`AnalyzeApplicationObject`):**
1. `GetDeclaredSymbol(ctx.Node)` to obtain `IApplicationObjectTypeSymbol` (early exit if not)
2. Skip PermissionSet/PermissionSetExtension, obsolete objects, test codeunits with permissions disabled
3. Get `Permissions` property (early exit if null, covers ~70% of objects)
4. **Collect DB invocations** (`CollectFromInvocations`):
   - Build object-scope record map (`objectScopeRecordMap`) from `containingObject.GetMembers()`: global vars, report data items (via `GetTypeSymbol()`), xmlport table elements (via `FlattenedNodes` + `GetTypeSymbol()`); also collect object-scope RecordRef variable names (`objectScopeRecordRefNames`)
   - Walk `ctx.Node.DescendantNodes()` for `MethodOrTriggerDeclarationSyntax`
   - Skip obsolete methods via `GetDeclaredSymbol` + `IsObsolete()`
   - For each method body: syntax pre-filter (`HasPossibleDbInvocation`)
   - Build per-method record variable map from `IMethodSymbol.LocalVariables` + `.Parameters`; RecordRef-typed locals/parameters/named returns go into a per-method RecordRef name set (`localRecordRefNames`)
   - Walk method body for both `InvocationExpressionSyntax` and standalone `MemberAccessExpressionSyntax` (method calls without parentheses)
   - Unified `TryGetPermissionFromDbAccess` extracts method name + receiver from either form, resolves via variable map (fast path), then via the receiver's `IOperation.Type` for non-identifier receivers (`this.`, expression receivers), and falls back to `GetSymbolInfo` for anything unresolved
   - **RecordRef whole-object bailout**: when a DB-operation method is invoked on a RecordRef receiver (detected via the RecordRef name sets, `IOperation.Type.NavTypeKind`, or the `GetSymbolInfo` fallback), `CollectFromInvocations` returns `true` and `AnalyzeApplicationObject` aborts without reporting any diagnostic for the object
5. **Collect data items** (`CollectFromDataItems`):
   - Iterate `containingObject.GetMembers()` for ReportDataItem, QueryDataItem, XmlPortNode symbols
   - For XmlPortNode: also iterate `FlattenedNodes` to reach nested table elements
   - Use `RequiredPermissionDetector.TryGetFrom*` methods
6. Compare declared entries against collected permissions, report diagnostics

### Key methods

| Method | Purpose |
|---|---|
| `AnalyzeApplicationObject` | Entry point; checks Permissions, orchestrates collection and reporting |
| `CollectFromInvocations` | Builds object-scope record map (vars + data items) and RecordRef name set, walks method bodies, resolves DB calls via unified handler. Returns `true` (bail out) when a RecordRef DB operation is found |
| `TryGetPermissionFromDbAccess` | Unified resolution for both syntax forms (with/without parentheses). Handles all four receiver forms: `MyTable.Modify()` / `Rec.Modify()` (variable-map fast path), `Modify()` (bare implicit self), `this.Modify()` (AL `this` self-reference, resolved via `IOperation.Type`). Falls back to `TryGetPermissionViaSymbolInfo` for unresolved receivers |
| `TryGetPermissionForType` | Builds a required permission from a resolved receiver/self type (record resolved to its backing table, or a table type directly); returns null for non-record/table types (e.g. `this` inside a codeunit) |
| `TryGetPermissionViaSymbolInfo` | Fallback for complex receivers: uses GetSymbolInfo on the node and receiver expression to resolve method and receiver type |
| `CollectFromDataItems` | Iterates report/query FlattenedDataItems and xmlport FlattenedXmlPortNodes (all via reflection) for implicit read permissions |
| `AddXmlPortNodeToVarMap` | Adds an xmlport table element to the object-scope record map if it references a non-temporary table |
| `HasPossibleDbInvocation` | Syntax pre-filter: checks if body has any invocation name matching a DB operation or a `DataTransfer` executor (handles both syntax forms) |
| `IsDataTransferReceiver` | Decides whether a `CopyFields`/`CopyRows` receiver is a `DataTransfer`, honoring AL scoping (a local record shadows an object-scope `DataTransfer` of the same name). Identifiers resolve through the name sets; every other shape (`this.MyDataTransfer`, expression receivers) falls back to the receiver's bound type, so anything that is not a `DataTransfer` keeps flowing through the normal record path |
| `AnalyzePermissionEntry` | Compares one declared entry against collected required permissions |
| `PermissionMatchesTable` | Matches identifier/qualified/objectId syntax against `ITableTypeSymbol` |

### Threading model

Each `SyntaxNodeAction` callback is self-contained with no shared mutable state. The compiler may parallelize callbacks across different objects, but each object's analysis uses only local variables (`List<RequiredPermission>`). No `ConcurrentDictionary` or cross-callback communication needed.

## Known issues

1. **Extension objects**: Cannot see base object code; may flag permissions needed by the base as unused
2. **CalcFields/CalcSums**: Indirect table access through FlowFields is not traced
3. **InherentPermissions overlap**: Table-level `InherentPermissions` may make an object-level entry redundant, but the analyzer does not flag this (different concern from unused)
4. **Cross-object calls**: If codeunit A calls codeunit B, and B accesses a table, A's permission for that table appears unused (correct, because permissions don't flow through the call stack). The reverse is not a limitation: when A iterates a set that B positioned (`Rec.Next()` in A), A's `r` is counted as used, because `Next` itself reads the database in A
5. **RecordRef bailout hides true positives**: When the whole-object bailout triggers, genuinely unused permissions in that object are no longer reported (accepted trade-off; see design decisions)
6. **DataTransfer bailout hides true positives**: a `CopyFields`/`CopyRows` that no `SetTables` reaches in the same body, or whose table arguments are not `Database::X` literals, silences AC0032 for the whole object — genuinely unused entries there go unreported (same trade-off as the RecordRef bailout). Two accepted imprecisions in the flow walk, both erring towards over-attribution: `exit` does not terminate a path, so state from before an early exit still flows forward; and the walk is a single pass, so a `SetTables` written *after* an executor inside a loop body does not flow back to it (the executor is then unresolvable unless another `SetTables` precedes it)
7. **FieldRef access is not a DB operation**: `FieldRef.Value`/`Field`/`Caption` operate on the in-memory current row and neither consume a permission nor trigger the RecordRef bailout (verified: only mapped DB methods on the RecordRef itself count). `FieldRef.CalcField` does read the database but is not traced, consistent with limitation 2

## CodeFix: TableDataAccessUnusedPermissionsCodeFixProvider

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
