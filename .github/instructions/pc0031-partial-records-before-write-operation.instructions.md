---
applyTo: 'src/ALCops.PlatformCop/**/PartialRecordsBeforeWriteOperation*'
---
# PC0031: PartialRecordsBeforeWriteOperation

## Purpose

Detects `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` calls on record variables that subsequently undergo write operations (Insert, Modify, Delete, Rename, TransferFields, Copy). The write requires all fields, forcing the platform to JIT-load the remaining fields via an extra SQL roundtrip. This makes the code strictly slower than not using partial records, and can cause "Inconsistent read of field(s)" or "JIT loading of field(s) failed" runtime errors under concurrent access.

**References:**
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Partial Records FAQ](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records-faq)
- [Demiliani: SetLoadFields Performances](https://demiliani.com/2021/06/03/dynamics-365-business-central-setloadfields-performances-with-reference-passing-or-value-passing-parameters/)
- [microsoft/AL#6893: Inconsistent Read Error](https://github.com/microsoft/AL/issues/6893)

## Diagnostic properties

| Property | Value |
|---|---|
| ID | `PC0031` |
| Category | Performance |
| Severity | Warning |
| Enabled by default | true |
| MessageFormat | `Do not use '{0}' before write operations ({1}) on '{2}'. Partial records cause a JIT load on write, resulting in an extra SQL roundtrip and possible runtime errors.` |
| Version gate | `Spring2021OrGreater` (SetLoadFields introduced in runtime 6.0, BC17) |
| netstandard2.1 | Full support (no net8.0-only APIs used) |

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cop | PlatformCop (PC0031) | Platform runtime behavior (JIT loading, SQL roundtrips, runtime errors) |
| Severity | Warning | Always slower + sometimes causes runtime errors = actionable anti-pattern |
| Write operations trigger set | Insert, Modify, Delete, Rename, TransferFields, Copy | MS Docs explicit list of operations requiring all fields |
| Excluded operations | ModifyAll, DeleteAll, Init | Set-based (no per-record JIT) or initialization-only |
| Report location | At the SetLoadFields/AddLoadFields/SetBaseLoadFields call | Where the developer needs to make the change |
| Load fields methods | SetLoadFields, AddLoadFields, SetBaseLoadFields | All three put record in partial mode |
| Branch sensitivity | Flag if SetLoadFields on ANY path + write on ANY path after a partial read | Union semantics; JIT load problem exists even on conditional paths |
| Write tracking | Flow-sensitive: only after partial read (read with HasLoadFields=true) | Writes before SetLoadFields or between SetLoadFields and the next read use the full record buffer and don't trigger JIT loads |
| SetLoadFields() no-args | Fully resets PC0031 state (HasPartialRead, LoadFieldsLocations, WriteMethodNamesAfterPartialRead) | No-args cancels partial records entirely; next read loads all fields, so prior SetLoadFields locations are irrelevant |
| CodeFix | Remove SetLoadFields statement | Simple, safe fix; without partial records, writes work normally |
| Variable scope | Local variables only (Phase 1) | Same as PC0030 |
| Category | Performance | SQL roundtrip overhead and JIT load performance impact |

## Architecture

### Shared analyzer class

PC0031 is implemented in the same `PartialRecordOperations` DiagnosticAnalyzer class as PC0030. Both rules share the `SetLoadFieldsWalker` inner class which performs a single walk per method body. The two rules are mutually exclusive on the same variable: when writes exist, PC0030 is suppressed and PC0031 may fire; when no writes exist, PC0031 can't fire.

### PC0031-specific tracking

- `LoadFieldsLocations`: flow-sensitive list of `LoadFieldsInfo` (Location + method name) on `FlowFlags`, recording where SetLoadFields/AddLoadFields/SetBaseLoadFields calls occur
- `HasPartialRead`: flow-sensitive boolean on `FlowFlags`, set to true when a read method (Get/Find/FindFirst/FindLast/FindSet) occurs while `HasLoadFields` is true. This means the record buffer is now partial.
- `WriteMethodNamesAfterPartialRead`: flow-sensitive `HashSet<string>?` on `FlowFlags`, recording write methods called while `HasPartialRead` is true. Only these writes are actual JIT-load triggers.
- `WriteMethodNames`: method-level `HashSet<string>` on `VariableState`, populated by `FinalizeResults()` from `WriteMethodNamesAfterPartialRead`
- `JitLoadWriteMethods`: static HashSet with the subset of write methods that trigger JIT loads

### Why writes are only tracked after partial reads

`SetLoadFields` only affects the NEXT read operation. It sets a flag on the record variable that tells the runtime to load fewer fields on the next Get/Find. The current record buffer remains unchanged until a read occurs. This means:

- `Get(); Delete(); SetLoadFields(); Get();` — Delete uses the full buffer from the first Get, no JIT
- `Get(); SetLoadFields(); Delete(); Get();` — Delete still uses the full buffer from the first Get, no JIT
- `SetLoadFields(); Init(); Insert();` — Init creates a fresh in-memory record, Insert writes it, no prior partial data
- `SetLoadFields(); Get(); Modify();` — Get loads partial data, Modify needs all fields → JIT load → REAL problem

### Reporting logic

After the walker completes, for each variable:
1. Check `state.LoadFieldsLocations.Count > 0`
2. Check `state.WriteMethodNames` intersects `JitLoadWriteMethods`
3. Report PC0031 at each LoadFieldsLocation with the write method names

### Flow sensitivity

- `LoadFieldsLocations` uses union merge (same as UncoveredReads) with dedup by source position
- `HasPartialRead` uses OR merge (if any branch has a partial read, conservative)
- `WriteMethodNamesAfterPartialRead` uses union merge (write on any branch is tracked)
- `Clear(var)` and `Reset()` clear all flow state including `HasPartialRead` and `WriteMethodNamesAfterPartialRead`
- `SetLoadFields()` with no args resets `HasLoadFields`, `HasPartialRead`, clears `LoadFieldsLocations` and `WriteMethodNamesAfterPartialRead`. This fully cancels partial records mode: the next read loads all fields, so prior SetLoadFields locations and writes-after-partial-read are moot.

## CodeFix: PartialRecordsBeforeWriteOperationCodeFixProvider

### Purpose

Provides a QuickFix "ALCops: Remove SetLoadFields" that removes the entire `ExpressionStatementSyntax` containing the SetLoadFields/AddLoadFields/SetBaseLoadFields call.

### Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Fix action | Remove statement entirely | Without partial records, writes work normally with full-field loads |
| Statement finding | Walk up from diagnostic span to `ExpressionStatementSyntax` | The diagnostic targets the invocation expression; the statement is the parent |
| FixAll | Supported via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |

## Relationship to PC0030

| Aspect | PC0030 (UsePartialRecordsOnRead) | PC0031 (PartialRecordsBeforeWriteOperation) |
|---|---|---|
| Focus | Missing SetLoadFields before reads | SetLoadFields present before writes |
| Trigger | Read without SetLoadFields | SetLoadFields + write on same variable |
| Severity | Info | Warning |
| CodeFix | Add SetLoadFields | Remove SetLoadFields |
| Mutually exclusive | Yes (writes suppress PC0030) | Yes (no writes means PC0031 can't fire) |

## Test coverage

### HasDiagnostic (9 cases)

| Test case | Scenario |
|---|---|
| SetLoadFieldsThenModify | `SetLoadFields + Get + Modify` |
| SetLoadFieldsThenDelete | `SetLoadFields + Get + Delete` |
| SetLoadFieldsThenRename | `SetLoadFields + Get + Rename` |
| SetLoadFieldsThenTransferFields | `SetLoadFields + Get + TransferFields` |
| SetLoadFieldsThenCopy | `SetLoadFields + Get + Copy` |
| AddLoadFieldsThenModify | `AddLoadFields variant` |
| SetBaseLoadFieldsThenModify | `SetBaseLoadFields variant` |
| BranchWithWrite | `SetLoadFields + Get + conditional Modify` |
| QualifiedSetLoadFields | `SetLoadFields + FindFirst + Modify` |

### NoDiagnostic (11 cases)

| Test case | Suppression reason |
|---|---|
| SetLoadFieldsReadOnly | No write operation (read-only usage) |
| NoSetLoadFieldsModify | No SetLoadFields (no partial records) |
| ModifyAll | ModifyAll excluded from trigger set |
| DeleteAll | DeleteAll excluded from trigger set |
| Init | Init excluded from trigger set |
| TemporaryTable | Temporary table (no SQL backing) |
| ClearBetweenSetLoadFieldsAndModify | Clear resets partial records state |
| WriteThenSetLoadFields | Write (Delete) before SetLoadFields; uses full buffer, no JIT |
| WriteAfterSetLoadFieldsBeforePartialRead | Write (Delete) after SetLoadFields but before partial read; uses full buffer from prior Get |
| SetLoadFieldsNoArgsResetsPartialRead | SetLoadFields("PK") + Get + SetLoadFields() no-args + Get + Delete; no-args cancels partial records entirely |
| SetLoadFieldsThenInsert | `SetLoadFields + Init + Insert` with no read; Init creates fresh record, no partial data |

### HasFix (3 cases)

| Test case | Scenario |
|---|---|
| RemoveSetLoadFields | Remove `SetLoadFields(...)` statement |
| RemoveAddLoadFields | Remove `AddLoadFields(...)` statement |
| RemoveSetBaseLoadFields | Remove `SetBaseLoadFields()` statement |

## Phase 2 roadmap (not yet implemented)

- **Global variables**: Track across methods in the same object
- **Parameter variables**: Analyze calling context
- **Cross-procedure tracing**: Track through helper methods
- **Event parameter tracing**: Optional mode for event arguments
