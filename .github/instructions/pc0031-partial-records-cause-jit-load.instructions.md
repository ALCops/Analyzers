---
applyTo: 'src/ALCops.PlatformCop/**/PartialRecordsCauseJitLoad*'
---

# PC0031: PartialRecordsCauseJitLoad

## Purpose

Detects `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` calls on record variables that subsequently undergo full-field operations (Insert, Delete, Rename, TransferFields, Copy). These operations require all fields on the record to be loaded, so the platform will emit a JIT load if they're not already loaded. This makes the code strictly slower than not using partial records, and can cause "Inconsistent read of field(s)" or "JIT loading of field(s) failed" runtime errors under concurrent access.

**References:**
- [Issue #264](https://github.com/ALCops/Analyzers/issues/264) (Control flow false positive)
- [Issue #265](https://github.com/ALCops/Analyzers/issues/265) (Modify exclusion)
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Partial Records FAQ](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records-faq)
- [Demiliani: SetLoadFields Performances](https://demiliani.com/2021/06/03/dynamics-365-business-central-setloadfields-performances-with-reference-passing-or-value-passing-parameters/)
- [microsoft/AL#6893: Inconsistent Read Error](https://github.com/microsoft/AL/issues/6893)

## Diagnostic properties

**PC0031** · Category: Performance · Severity: Warning · Enabled: true
Message: `Do not use '{0}' before full-field operations ({1}) on '{2}'. Partial records cause a JIT load, resulting in an extra SQL roundtrip and possible runtime errors.`
Version gate: `Spring2021OrGreater` (runtime 6.0, BC17) · Full netstandard2.1 support

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | PlatformCop (PC0031) | Platform runtime behavior (JIT loading, SQL roundtrips, runtime errors) |
| Severity | Warning | Always slower + sometimes causes runtime errors = actionable anti-pattern |
| JIT-load trigger operations | Insert, Delete, Rename, TransferFields, Copy | MS Docs explicit list: "inserts, deletes, renames, field transfers, or copies to temporary records." |
| Excluded operations | Modify, ModifyAll, DeleteAll, Init, Assignment (:=) | Modify only writes changed fields (no JIT load per MS Docs). ModifyAll/DeleteAll are set-based. Init is initialization-only. Assignment copies in-memory buffer without triggering JIT load. |
| Report location | At the SetLoadFields/AddLoadFields/SetBaseLoadFields call | Where the developer needs to make the change |
| Load fields methods | SetLoadFields, AddLoadFields, SetBaseLoadFields | All three put record in partial mode |
| Condition-aware narrowing | When read IS the if-condition, HasPartialRead only applies in the "found" branch | `if [not] Rec.Find/Get() then`: the not-found branch has an empty buffer (no partial data), so full-field ops there don't trigger JIT. Fixes #264. |
| Branch sensitivity (general) | Flag if SetLoadFields on ANY path + full-field op on ANY path after a partial read | Union semantics for complex conditions not handled by condition-aware narrowing |
| Write tracking | Flow-sensitive: only after partial read (read with HasLoadFields=true) | Full-field ops before SetLoadFields or between SetLoadFields and the next read use the full record buffer and don't trigger JIT loads |
| Branch suppression (PC0031) | Suppress JIT-load write detection when inside conditional branches | Avoids false positives where the write only executes on a path where the record wasn't found. Accepts false negatives for zero false positives. Fixes #264. |
| Unconditional loops | repeat-until body does NOT suppress (always executes once) | JIT load is guaranteed on the first iteration |
| SetLoadFields() no-args | Fully resets PC0031 state (HasPartialRead, LoadFieldsLocations, WriteMethodNamesAfterPartialRead) | No-args cancels partial records entirely; next read loads all fields |
| CodeFix | Remove SetLoadFields statement | Simple, safe fix; without partial records, full-field operations work normally |
| Variable scope | Local variables only (Phase 1) | Same as PC0030 |
| Category | Performance | SQL roundtrip overhead and JIT load performance impact |

## Architecture

PC0031 shares the `PartialRecordOperations` analyzer class and `SetLoadFieldsWalker` with PC0030. See `pc0030-use-partial-records-on-read.instructions.md` for the shared flow-sensitive analysis infrastructure (registration strategy, control flow fork/merge semantics, state architecture, reset operations, merge deduplication, known issues).

### Shared analyzer class

The two rules are mutually exclusive on the same variable: when full-field operations exist, PC0030 is suppressed and PC0031 may fire; when no full-field operations exist, PC0031 can't fire.

### PC0031-specific tracking

- `LoadFieldsLocations`: flow-sensitive list of `LoadFieldsInfo` (Location + method name) on `FlowFlags`, recording where SetLoadFields/AddLoadFields/SetBaseLoadFields calls occur
- `HasPartialRead`: flow-sensitive boolean on `FlowFlags`, set to true when a read method (Get/Find/FindFirst/FindLast/FindSet) occurs while `HasLoadFields` is true. This means the record buffer is now partial.
- `WriteMethodNamesAfterPartialRead`: flow-sensitive `HashSet<string>?` on `FlowFlags`, recording full-field operation names called while `HasPartialRead` is true.
- `WriteMethodNames`: method-level `HashSet<string>` on `VariableState`, populated by `FinalizeResults()` from `WriteMethodNamesAfterPartialRead`
- `JitLoadWriteMethods`: static HashSet with the subset of write methods that trigger JIT loads (Insert, Delete, Rename, TransferFields, Copy)

### Why Modify is excluded

Per [MS Docs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records): "For performance reasons, it's not recommended to use partial records on a record that will do inserts, deletes, renames, field transfers, or copies to temporary records." Modify is conspicuously absent. Modify only writes back changed fields in the record buffer without needing all fields loaded. Verified against the decompiled MS CodeCop SDK source.

### Why assignment (:=) is excluded

The `:=` operator copies the in-memory record buffer without accessing the SQL data source. No JIT load occurs. MS Docs "copies to temporary records" refers to the `Record.Copy()` method and/or `TransferFields`, not the `:=` operator. This is confirmed by the MS CodeCop Rule0242 decompiled source, which visits assignment RHS only for field access detection, not as a JIT-load trigger.

### Why full-field operations are only tracked after partial reads

`SetLoadFields` only affects the NEXT read operation. It sets a flag on the record variable that tells the runtime to load fewer fields on the next Get/Find. The current record buffer remains unchanged until a read occurs. This means:

- `Get(); Delete(); SetLoadFields(); Get();` — Delete uses the full buffer from the first Get, no JIT
- `Get(); SetLoadFields(); Delete(); Get();` — Delete still uses the full buffer from the first Get, no JIT
- `SetLoadFields(); Init(); Insert();` — Init creates a fresh in-memory record, Insert writes it, no prior partial data
- `SetLoadFields(); Get(); Delete();` — Get loads partial data, Delete needs all fields → JIT load → REAL problem

### Condition-aware branch suppression

PC0031 uses a `_branchDepth` counter to suppress JIT-load write detection inside conditional branches. The counter increments when entering if/case/while/for/foreach bodies, and decrements when leaving. Writes are only recorded in `WriteMethodNamesAfterPartialRead` when `_branchDepth == 0`.

**Key distinction:** `repeat-until` body does NOT increment branch depth because it always executes at least once (the JIT load is guaranteed on the first iteration).

This approach eliminates false positives from patterns like:
- `SetLoadFields(); if not FindFirst() then Insert();` (find-or-create)
- `ok := Get(); if not ok then Delete();` (boolean variable pattern)
- `SetLoadFields(); Get(); if Condition then Delete();` (conditional write)

**Trade-off:** Some genuine JIT-load problems inside branches become false negatives. This is acceptable because:
1. The most common AL patterns (find-or-create, conditional delete) would otherwise generate noise
2. Straight-line JIT-load problems are still caught
3. repeat-until loops (the most dangerous pattern) still fire

### Reporting logic

After the walker completes, for each variable:
1. Check `state.LoadFieldsLocations.Count > 0`
2. Check `state.WriteMethodNames` contains entries matching `JitLoadWriteMethods`
3. Report PC0031 at each LoadFieldsLocation with the operation names

### Flow sensitivity

- `LoadFieldsLocations` uses union merge (same as UncoveredReads) with dedup by source position
- `HasPartialRead` uses OR merge (if any branch has a partial read, conservative)
- `WriteMethodNamesAfterPartialRead` uses union merge (operation on any branch is tracked)
- `Clear(var)` and `Reset()` clear all flow state including `HasPartialRead` and `WriteMethodNamesAfterPartialRead`
- `SetLoadFields()` with no args resets `HasLoadFields`, `HasPartialRead`, clears `LoadFieldsLocations` and `WriteMethodNamesAfterPartialRead`

## CodeFix: PartialRecordsCauseJitLoadCodeFixProvider

### Purpose

Provides a QuickFix "ALCops: Remove SetLoadFields" that removes the entire `ExpressionStatementSyntax` containing the SetLoadFields/AddLoadFields/SetBaseLoadFields call.

### Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Fix action | Remove statement entirely | Without partial records, full-field operations work normally |
| Statement finding | Walk up from diagnostic span to `ExpressionStatementSyntax` | The diagnostic targets the invocation expression; the statement is the parent |
| FixAll | Supported via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |

## Relationship to PC0030

| Aspect | PC0030 (UsePartialRecordsOnRead) | PC0031 (PartialRecordsCauseJitLoad) |
|---|---|---|
| Focus | Missing SetLoadFields before reads | SetLoadFields present before full-field operations |
| Trigger | Read without SetLoadFields | SetLoadFields + full-field op on same variable |
| Severity | Info | Warning |
| CodeFix | Add SetLoadFields | Remove SetLoadFields |
| Mutually exclusive | Yes (full-field ops suppress PC0030) | Yes (no full-field ops means PC0031 can't fire) |

## Test coverage

**HasDiagnostic (6 cases):** SetLoadFieldsThenDelete, SetLoadFieldsThenRename, SetLoadFieldsThenTransferFields, SetLoadFieldsThenCopy, QualifiedSetLoadFields, RepeatUntilWithWrite.
**NoDiagnostic (22 cases):** SetLoadFieldsReadOnly, NoSetLoadFieldsModify, ModifyAll, DeleteAll, Init, TemporaryTable, ClearBetweenSetLoadFieldsAndModify, WriteThenSetLoadFields, WriteAfterSetLoadFieldsBeforePartialRead, SetLoadFieldsNoArgsResetsPartialRead, SetLoadFieldsThenInsert, SetLoadFieldsThenModify, AddLoadFieldsThenModify, SetBaseLoadFieldsThenModify, SetLoadFieldsThenGetBySystemIdThenModify, AssignmentBeforePartialRead, AssignmentAfterPartialRead, WriteInNotFoundBranch, BranchWithWrite, WriteInFoundBranch, GetAsConditionThenDelete, FindOrCreate.
**HasFix (3 cases):** RemoveSetLoadFields, RemoveAddLoadFields, RemoveSetBaseLoadFields.

## Phase 2 roadmap (not yet implemented)

- **Global variables**: Track across methods in the same object
- **Parameter variables**: Analyze calling context
- **Cross-procedure tracing**: Track through helper methods
- **Event parameter tracing**: Optional mode for event arguments
