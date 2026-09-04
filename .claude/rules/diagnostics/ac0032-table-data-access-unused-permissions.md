---
paths:
  - "src/ALCops.ApplicationCop/**/TableDataAccessUnusedPermissions*"
  - "src/ALCops.ApplicationCop.Test/Rules/TableDataAccessUnusedPermissions/**"
---

# AC0032: Unused Permissions

## Purpose

Detects `Permissions` property entries that have no corresponding table data access in the object. This is the inverse of AC0031 (which detects missing permissions). Reports an Info-level diagnostic per unused permission entry. Unused permissions are dead code because the `Permissions` property only applies to the object that directly accesses the table (no call-stack inheritance).

Registers `RegisterSyntaxNodeAction` on the nine application object syntax kinds (codeunit, table, tableextension, page, pageextension, report, reportextension, query, xmlport); main type `TableDataAccessUnusedPermissions`, with detection shared in `ALCops.Common/Permissions` (`RequiredPermissionDetector`, `DataTransferOperations`, `DataTransferTableResolver`).

## Design decisions

| Decision | Rationale |
|---|---|
| One self-contained `SyntaxNodeAction` per object, no CompilationStart/End or cross-callback state | Each object is analyzed atomically with the SemanticModel at hand; the earlier two-phase accumulator produced false positives during incremental compilation (`sdk-analysis-scope.md`). |
| Name-keyed variable maps (locals, parameters, named returns, globals, data items) resolve receivers before any binding; `GetOperation` only for non-identifier receivers, `GetSymbolInfo` as last fallback | The fast path handles ~99% of receivers and was 4.5x faster than binding every invocation (`analyzer-performance.md`). |
| Report data items and xmlport table elements go into the object-scope record map | They act as implicit record variables in trigger code. |
| Named return values are mapped only when `ReturnValueSymbol.IsNamed` | An unnamed return has no identifier a receiver could use. |
| Nested report data items via the public `IReportTypeSymbol.FlattenedDataItems`; nested query data items and xmlport nodes via reflection on the internal flattened properties | `GetMembers()` and the public xmlport API only return one level, which produced false positives on nested structures. |
| System tables are included (`includeSystemTables: true`) | A declared permission on a system table must be matched against real accesses like any other. |
| Two descriptors share the one ID: `...EntireEntry` (no operation on the table) and `...PartialChars` (some declared RIMD chars unused) | Same rule, two messages that tell the developer exactly what to remove. |
| `PermissionMatchesTable` is duplicated from `PermissionResolver` | It matches syntax nodes, not resolved symbols; sharing would couple the two. |
| `Next` counts as a Read ([#466](https://github.com/ALCops/Analyzers/issues/466)) | An object that only iterates a set positioned elsewhere still reads the database, so its `r` is used; without this the pattern was a false positive. Adding `Next` to `MethodOperationMap` also covers `RecordRef.Next()` through the bailout. |
| Temporary-only accesses do not count as uses, so a permission held only for temporary records is reported | Temporary tables never touch the database; all temporary forms are excluded through the `IsTemporary()` extensions at every map and fallback. |
| Whole-object bailout on any `MethodOperationMap` call on a `RecordRef` receiver ([#420](https://github.com/ALCops/Analyzers/issues/420)) | A `RecordRef` can target any table at runtime, so no declared permission can be proven unused; without it FixAll removed permissions needed at runtime. Suppressing only the matching char was considered and rejected for simplicity. |
| `CopyFields` and `CopyRows` on a `DataTransfer` count as table operations (`r`+`m`, `r`+`i`) ([#465](https://github.com/ALCops/Analyzers/issues/465)) | The tables are `SetTables` arguments rather than receivers, so receiver-keyed resolution never saw them and every `DataTransfer`-only permission was reported. The other seven `DataTransfer` methods only build the definition. |
| `DataTransfer` executors live in `DataTransferOperations`, consulted only after the receiver is confirmed to be a `DataTransfer`, not in `MethodOperationMap` | `MethodOperationMap` is keyed on record-receiver built-ins and mirrored by `RecordMethodClassification`; adding the names there would make any same-named record call look like a DB operation. |
| `SetTables` and executor must sit in the same method or trigger body; one `DataTransferTableResolver` per body, held callback-local | Following the variable across procedures needs call-graph analysis and shared state, which the design rules out; the cross-procedure case falls to the bailout. |
| Pairing is flow-sensitive: a `SetTables` replaces the pending pair for that variable, each executor is charged only for the pair that reaches it, and an executor does not clear the pair | `SetTables; CopyRows; SetTables; CopyFields` is the standard upgrade pattern; the earlier union over every `SetTables` in the body made never-needed permissions look used and hid true positives such as an `i` on a `CopyFields`-only destination. |
| Branch merges are the union of pairs; `break` feeds the post-loop state; loop bodies are visited twice | Union is the conservative direction (it can only make a permission look used); the second pass lets a `SetTables` written after the executor reach it via the back edge, and is the fixed point because the transfer function is constant or pass-through. |
| Executor and `SetTables` are matched on the variable name, with `this.<variable>` normalized to the bare name via `EnumProvider.OperationKind.ThisReference` | Mixed spellings of the same variable must pair; `ThisExpressionSyntax` is unavailable at the netstandard2.1 floor (`record-receiver-forms.md`). |
| Only `Database::"X"` literals resolve (`IApplicationObjectAccess`, possibly wrapped in `IConversionExpression`) | Constant propagation of integer locals is unbounded and the bailout already covers the case safely. |

## Deliberate non-reports

- Objects without a `Permissions` property, obsolete objects, `permissionset`/`permissionsetextension` objects, and test codeunits with `TestPermissions = Disabled`.
- Accesses inside obsolete methods are not collected as uses.
- The whole object is silent when a DB operation runs on a `RecordRef` receiver (see [#420](https://github.com/ALCops/Analyzers/issues/420) above).
- The whole object is silent when a `DataTransfer` executor cannot be resolved: no `SetTables` reaches it on any path, a reaching `SetTables` has a non-literal table argument, or the receiver is neither a plain identifier nor `this.<variable>`. The executor may then touch any table, exactly like the `RecordRef` case.
- `FieldRef.Value`/`Field`/`Caption` are in-memory operations on the current row: they neither consume a permission nor trigger the bailout. `FieldRef.CalcField` does read the database but is not traced (see Known issues).

## Known issues

- Extension objects cannot see the base object's code and may flag permissions the base needs as unused.
- `CalcFields`/`CalcSums` (FlowField reads) are not traced.
- A table-level `InherentPermissions` can make an object-level entry redundant; the rule does not flag that (a different concern from unused).
- Cross-object calls: if codeunit A calls B and B accesses the table, A's permission is correctly reported as unused (permissions do not flow through the call stack). The reverse is fine: A iterating with `Next()` a set that B positioned counts as A's own read.
- Both whole-object bailouts hide genuinely unused entries in that object; accepted trade-off.
- The flow walk does not terminate a path at `exit`, so state from before an early exit flows forward. For AC0032 that only ever makes a permission look used; the same walk backs AC0031, where it can produce a spurious report.

## SDK facts

- `IXmlPortNodeSymbol.FlattenedNodes` returns only immediate children; the internal `SourceXmlPortTypeSymbol.FlattenedNodes` is fully recursive (read via reflection in `GetFlattenedXmlPortNodes`).
- `IReportTypeSymbol.FlattenedDataItems` is public and recursive; `IQueryTypeSymbol` has no public equivalent, so the internal `QueryTypeSymbol.FlattenedDataItems` is read via `PropertyAccessor.GetPropertyIfExists`.
- `IApplicationObjectTypeSymbol.GetMembers()` returns only top-level data items and schema nodes.
- `IRecordTypeSymbol.Temporary` reflects only the `temporary` keyword; `TableType = Temporary` must be read from `ITableTypeSymbol.TableType`.
- The `SetTables` argument operation is an `IApplicationObjectAccess`, optionally wrapped in an `IConversionExpression`.
- Receiver forms, self-reference symbol shapes and name-map scoping: see `record-receiver-forms.md`; `GetOperation` cost in `SyntaxNodeAction` and the method-call-without-parentheses syntax shape: see `analyzer-performance.md`.

## Test notes

- The `this` fixtures are gated on runtime 14.0; the `PermissionSetExtension` NoDiagnostic fixture on 13.0.

## CodeFix: TableDataAccessUnusedPermissionsCodeFixProvider

| Decision | Rationale |
|---|---|
| Removes the whole entry when it is entirely unused, replaces the chars with the used subset when only some are unused | Mirrors the two descriptors; a partial entry keeps the table listed. |
| Removes the entire `Permissions` property when the last entry goes | An empty `Permissions =` is not valid. |
| Locates the `PermissionSyntax` via ancestor-or-self, then descendant search from the diagnostic span | `FindNode` may return the `PermissionPropertyValueSyntax` when a single entry shares its span. |
| Strips the leading trivia of the new first entry after removing the original first one | `SeparatedSyntaxList.Remove` keeps the second entry's newline and indent, leaving a `Permissions =\n    tabledata ...` artifact otherwise. |
| Supports FixAll | Unused entries typically appear across many objects at once. |
