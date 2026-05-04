---
applyTo: 'src/ALCops.PlatformCop/**/{UsePartialRecordsOnRead,PartialRecordOperations}*'
---

# PC0030: UsePartialRecordsOnRead

## Purpose

Recommends using `SetLoadFields` (or `AddLoadFields`/`SetBaseLoadFields`) before read operations on local record variables. Without partial records, the runtime loads ALL normal fields including those from table extensions, causing unnecessary SQL joins and 2-9x slower data access.

**References:**
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [Community discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/218)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Record.SetLoadFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setloadfields-method)

## Diagnostic properties

**PC0030** · Category: Performance · Severity: Info · Enabled: true
Message: `Use SetLoadFields before '{0}.{1}()' to improve performance by loading only the fields that are needed.`
Version gate: `Spring2021OrGreater` (runtime 6.0, BC17) · Full netstandard2.1 support

## Design decisions

These decisions were made during the initial design and should be preserved unless explicitly revisited:

| Decision | Choice | Rationale |
|---|---|---|
| Scope | Phase 1: local variables only | Global variables require cross-method analysis; deferred to Phase 2 |
| Function pass | Suppress on ANY function/event pass (including built-in methods like PAGE.Run) | Conservative: avoids false positives when the callee might access any field |
| Temporary tables | Skip | No SQL Server backing, no table extension join benefit. Matches AA0242 behavior |
| Severity | Info | Performance suggestion, not a correctness issue |
| Field access required | No (report on read operation alone) | User preference; may generate noise on patterns like `if Rec.Get(Key) then` |
| RecordRef | Included | Skip temp/table-type checks (not statically determinable for RecordRef) |
| Record table type | Normal only | Skip CDS, Exchange, temporary, and other non-SQL table types |
| RecordRef.SetTable(Record) | Link suppression to target Record | If target has write ops, passed to function, load fields, or is non-local/unresolvable: suppress RecordRef. Only simple overload (1 arg). |
| RecordRef.SetTable(Record, Boolean) | Out of scope | ShareTable overload not handled; revisit if users report false positives |
| RecordRef.GetTable | Out of scope | Reverse direction; only SetTable for now |
| Flow-sensitive analysis | Forward dataflow with fork/merge at branches | Fixes false negatives where SetLoadFields after read, Clear between SetLoadFields and read, or SetLoadFields in one branch suppressed diagnostics elsewhere |
| All three flags flow-aware | Yes (HasLoadFields, HasWriteOp, PassedToFunction) | Clear/Reset should reset all state; all flags have the same positional bug class |
| HasLoadFields merge rule | AND (intersection) | If any code path lacks SetLoadFields, the read might execute without it |
| HasWriteOp/PassedToFunction merge rule | OR (union) | Conservative: if any path writes/passes, suggesting SetLoadFields could interfere |
| Clear(var) resets all flags | Yes | Clear completely reinitializes the variable, invalidating prior state |
| Reset() resets all flags | Yes | Reset clears all filters and state on the record |
| SetLoadFields() no args | Resets HasLoadFields only | Only affects the partial records state, not write/pass flags |
| ClearAll() | Out of scope | Extremely rare, affects all variables, complex to handle |
| while/for/foreach body | Merge pre-loop with post-body | Body might not execute |
| repeat-until body | Use post-body state directly | Body always executes at least once |
| Retroactive read clearing | Clear UncoveredReads when write/pass encountered | Handles `Get(); Modify()` pattern where write comes after read |
| Loop analysis | Single pass, no fixed-point iteration | Acceptable approximation; fixed-point adds complexity for rare edge cases |
| RecordRef flow analysis | Kept method-level (EverHad* flags) | RecordRef SetTable linkage is already complex; bug fixes focus on Record variables |
| Pre-fork read merge rule | Intersection (must be in ALL branches) | Reads before a fork that are retroactively cleared by write/pass in any branch should stay cleared; prevents false positives on `if FindSet then repeat Modify` pattern |
| In-branch read merge rule | Union (in ANY branch) | Reads added inside a branch are genuinely uncovered on their specific path and should be reported |

## Architecture

### Registration strategy

Uses `RegisterCodeBlockAction` (not `RegisterOperationAction`) to analyze entire method/trigger bodies in a single pass. This avoids redundant walks when multiple read operations exist on the same variable.

### Analysis flow (flow-sensitive forward dataflow)

1. Extract local variables of type `Record` (non-temp, Normal table) or `RecordRef` from `IMethodSymbol.LocalVariables`
2. Get `IOperation` tree from the code block via `SemanticModel.GetOperation(body)`
3. Walk with `SetLoadFieldsWalker` (extends `OperationWalker`), maintaining per-variable `FlowFlags`:
   - `HasLoadFields`: bool, true when SetLoadFields/AddLoadFields/SetBaseLoadFields encountered on current path
   - `HasWriteOp`: bool, true when Insert/Modify/ModifyAll/Delete/DeleteAll/Rename/TransferFields/Init/Copy encountered
   - `PassedToFunction`: bool, true when variable passed as argument to any invocation or non-built-in method called on it
   - `UncoveredReads`: List of read locations where none of the above flags were true at the time of the read
4. **Read evaluation is immediate**: When a read (Get/Find/FindFirst/FindLast/FindSet) is encountered, the current flow state determines whether to flag it. If `!HasLoadFields && !HasWriteOp && !PassedToFunction`, the read is added to `UncoveredReads`.
5. **Retroactive clearing**: When a write/pass operation is encountered, `UncoveredReads` is cleared (reads before the write are retroactively suppressed). This handles the pattern `Get(); Modify()` where the write comes after the read.
6. **Control flow**: Override `VisitIfStatement`, `VisitCaseStatement`, and loop visit methods with fork/merge semantics.
7. **Reset detection**: `Clear(var)`, `var.Reset()`, and `var.SetLoadFields()` with no arguments reset flow flags.
8. Post-walk: `FinalizeResults()` deduplicates uncovered reads and copies them to `VariableState.UncoveredReadLocations`.
9. For RecordRef variables with `SetTable(TargetRecord)` calls: suppress if any target is unresolvable, non-local, or has a suppression condition (uses method-level `EverHad*` flags).

### Control flow fork/merge semantics

| Construct | Fork | Merge |
|---|---|---|
| `if-then-else` | Clone pre-branch state for each branch | Merge both branches at join point |
| `if-then` (no else) | Clone for the then-branch | Merge then-branch with pre-branch state (implicit empty else) |
| `case` | Clone pre-case state for each case line and else | Merge all branches at join point |
| `while`/`for`/`foreach` | Visit condition/init, clone for body | Merge post-body with pre-loop state (body might not execute) |
| `repeat-until` | Visit body directly | Use post-body state (body always executes at least once) |

### Merge rules

| Flag | Merge rule | Rationale |
|---|---|---|
| `HasLoadFields` | AND (all branches must have it) | If any path lacks SetLoadFields, the read might execute without it |
| `HasWriteOp` | OR (any branch having it) | If any path writes, suggesting SetLoadFields could interfere |
| `PassedToFunction` | OR (any branch having it) | If any path passes the variable, callee might access any field |
| `UncoveredReads` | Pre-fork: intersection; in-branch: union | Pre-fork reads retroactively cleared in any branch stay cleared; in-branch reads on specific paths should be reported |

### Reset operations

| Operation | Detection | Effect |
|---|---|---|
| `Clear(MyTable)` | Built-in invocation `Clear`, arg[0] is tracked variable | Reset all three boolean flags |
| `MyTable.Reset()` | Built-in instance method `Reset` on tracked variable | Reset all three boolean flags |
| `MyTable.SetLoadFields()` | Built-in instance method `SetLoadFields` with 0 arguments | Reset `HasLoadFields` only |

### Merge deduplication (prevents OOM on large codebases)

Pre-fork reads are cloned into both branches during fork. Without deduplication, concatenating both branches at merge doubles the list size at each nesting level, causing exponential growth (2^K entries after K levels). With deeply nested if-statements and many tracked variables (common in large apps like the Base App), this caused `OutOfMemoryException` in `FlowFlags.Clone()`.

Fix: deduplicate reads by `SourceSpan.Start` position (via `HashSet<int>`) at every merge point. List size is bounded by the number of unique read operations regardless of nesting depth.

### Key implementation detail: argument checking

The walker checks ALL invocations (both built-in and user-defined) for tracked variables in arguments. This is necessary because built-in methods like `PAGE.Run(PageId, Record)` pass the record to a page that will access its fields. The `GetVariableNameFromArgument` method only matches direct variable identifiers (e.g., `Item` in `PAGE.Run(PAGE::"Item Card", Item)`), not field access expressions (e.g., `MyTable."No."` in `SetRange`), so this doesn't cause false positives on record method arguments.

### Dual-direction suppression

Write/pass operations suppress reads in both directions:
- **Forward**: Write/pass BEFORE a read causes the flag check at read time to suppress it
- **Backward**: Write/pass AFTER a read retroactively clears `UncoveredReads` (handles `Get(); Modify()` pattern)

### State architecture

```
FlowFlags (per-variable, forked/merged at branches):
  - HasLoadFields: bool
  - HasWriteOp: bool
  - PassedToFunction: bool
  - UncoveredReads: List<ReadInfo>

VariableState (per-variable, method-level accumulated):
  - UncoveredReadLocations: List<ReadInfo> (populated by FinalizeResults after walk)
  - IsRecordRef: bool
  - SetTableTargets: List<string?>
  - EverHadLoadFields/EverHadWriteOp/EverPassedToFunction: bool (for RecordRef SetTable check)
```

## Known issues and workarounds

### BoundObjectAccess InvalidCastException

The SDK's `OperationExtensions.GetSymbol()` throws `InvalidCastException` when the operation instance is a `BoundObjectAccess` (or `BoundApplicationObjectAccess`). These internal SDK types report `Kind = FieldAccess` but don't implement `IFieldAccess`, so the SDK's internal cast fails.

The analyzer uses `GetSymbolSafe()` from `ALCops.Common.Extensions.OperationSafeExtensions` on all `GetSymbol()` call sites. This method handles the bug without exception handling: it checks `is IApplicationObjectAccess` (public SDK interface) first, then guards any remaining `FieldAccess`-kind operations that don't implement `IFieldAccess` by returning null. See the "SDK GetSymbol() Bug" section in `analyzer-development.instructions.md`.

This is an SDK bug. If a future SDK version fixes it, the type checks become harmless no-ops.

## Method classification

Method classifications are centralized in `ALCops.Common.RecordMethodClassification`. This analyzer uses:
- `RecordMethodClassification.SingleRecordReadMethods` + `FindSet` for read methods (trigger diagnostic)
- `RecordMethodClassification.WriteMethods` for write/mutation methods (suppress entire variable)
- `RecordMethodClassification.LoadFieldsMethods` for load fields methods (suppress diagnostic)
- `RecordMethodClassification.JitLoadWriteMethods` for JIT load write methods (used by PC0031)

### Read methods (trigger diagnostic)
`Get`, `GetBySystemId`, `Find`, `FindFirst`, `FindLast`, `FindSet`

### Load fields methods (suppress diagnostic)
`SetLoadFields`, `AddLoadFields`, `SetBaseLoadFields`

### Write/mutation methods (suppress entire variable)
`Insert`, `Modify`, `ModifyAll`, `Delete`, `DeleteAll`, `Rename`, `TransferFields`, `Init`, `Copy`

### Reset methods (clear flow flags)
`Clear(var)` (built-in), `Reset()` (instance method), `SetLoadFields()` with 0 arguments (resets HasLoadFields only)

## Relationship to AA0242

AA0242 (CodeCop's `Rule0242PartialRecordsDetectJitLoads`) is the **complement** to PC0030:

- **AA0242**: Fires when `SetLoadFields` IS present but accessed fields are missing from it (detects JIT loads)
- **PC0030**: Fires when `SetLoadFields` is entirely absent (recommends adding it)

## Phase 2 roadmap (not yet implemented)

- **Global variables**: Analyze all methods in the object; check access modifiers (protected variables can't use SetLoadFields from outside); detect writes across any method
- **Parameter variables**: Analyze calling context to determine if the parameter is used only for reading
- **Cross-procedure tracing**: Track record variables passed by `var` reference through call chains
- **RecordRef enhanced**: Resolve the opened table at compile time when possible
- **SetBaseLoadFields/AddLoadFields recommendations**: Separate rule for recommending one over the other

## Test coverage

58 test cases total:

**HasDiagnostic (17 cases):** LocalRecordGet, LocalRecordGetBySystemId, LocalRecordFindFirst, LocalRecordFindSet, LocalRecordFindLast, LocalRecordFind, LocalRecordMultipleReads, LocalRecordRefFindFirst, SetLoadFieldsAfterGet, ClearBetweenSetLoadFieldsAndGet, ResetBetweenSetLoadFieldsAndGet, SetLoadFieldsNoArgsBetween, CaseBranchWithoutSetLoadFields, IfBranchWithoutSetLoadFields, ClearResetsWriteOp, ClearResetsPassedToFunction, LoopNoSetLoadFields.

**NoDiagnostic (29 cases):** HasSetLoadFields, HasSetLoadFieldsGetBySystemId, HasAddLoadFields, HasSetBaseLoadFields, HasModify, HasInsert, HasDelete, HasDeleteAll, HasModifyAll, HasRename, HasTransferFields, HasInit, HasCopy, PassedToFunction, PassedToEvent, PassedToPageRun, TemporaryTable, GlobalVariable, ParameterVariable, IsEmptyOnly, CDSTable, RecordRefSetTableWithModify, RecordRefSetTablePassedToFunction, DatabaseObjectReference, IfBothBranchesSetLoadFields, LoopSetLoadFieldsBefore, FindSetWithModifyInLoop, FindSetWithPassedToFunctionInLoop, GetWithConditionalModify.

**HasFix (11 cases):** SingleField, MultipleFields, QuotedFieldName, NoFieldAccess, SetRangeFieldExcluded, SetFilterFieldExcluded, SetRangeValueArgIncluded, AllFieldsInFilters, TestFieldIncluded, SetCurrentKeyExcluded, MixedFilterAndConsume.

## CodeFix: UsePartialRecordsOnReadCodeFixProvider

### Purpose

Provides a QuickFix "ALCops: Add SetLoadFields" that inserts a `SetLoadFields` call immediately before the read operation, pre-populated with only the fields whose values are actually consumed in the method body. Fields used solely as selectors in filter/metadata methods (e.g., `SetRange`, `SetFilter`, `SetCurrentKey`) are excluded.

### Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Scope | Record variables only (not RecordRef) | RecordRef.SetLoadFields takes integer field numbers, not statically determinable |
| No field access case | Primary key fields as fallback | Establishes the partial records pattern; PK fields are always loaded |
| Field detection | In the CodeFix via SemanticModel | Zero analyzer overhead; `Document.GetSemanticModelAsync()` is available |
| Field ordering | Alphabetical (case-insensitive) | Deterministic output regardless of source order |
| Field filtering | Normal fields only | Skip FlowField, FlowFilter, Blob (SetLoadFields returns false for these) |
| Insertion position | New statement before the read operation's containing statement | Matches MS docs conventions |
| Filter field exclusion | Built-in methods only | User-defined methods already trigger PassedToFunction suppression in the analyzer |
| Exclusion approach | Override VisitInvocationExpression, selectively visit arguments | Clean, avoids fragile skip-set patterns; extensible via HashSets |
| Value args in filter methods | Still collected if they're field accesses | `SetRange(F1, MyTable.F2)` should include F2 since F2's value is consumed |
| All-filters fallback | Falls back to PK fields | Consistent with existing no-field-access behavior |

### Field selector exclusion in FieldAccessCollector

The `FieldAccessCollector` overrides `VisitInvocationExpression` to detect built-in Record methods that use field references as selectors rather than value consumers. Two categories:

**FirstArgFieldSelectorMethods** (exclude arg[0] only, visit remaining args normally):

| Method | Why excluded |
|---|---|
| `SetRange` | Arg 0 = filter field, args 1-2 = filter values (WHERE clause) |
| `SetFilter` | Arg 0 = filter field, rest = filter expression |
| `GetRangeMin` | Arg 0 = field selector, returns filter range |
| `GetRangeMax` | Arg 0 = field selector, returns filter range |
| `GetFilter` | Arg 0 = field selector, returns filter string |
| `SetAscending` | Arg 0 = field selector, arg 1 = boolean |
| `FieldCaption` | Arg 0 = field selector, returns metadata |
| `FieldName` | Arg 0 = field selector, returns metadata |
| `FieldNo` | Arg 0 = field selector, returns metadata |
| `HasFilter` | Arg 0 = field selector, returns boolean |
| `FieldActive` | Arg 0 = field selector, returns boolean |

**AllArgsFieldSelectorMethods** (exclude all arguments):

| Method | Why excluded |
|---|---|
| `SetCurrentKey` | All args are key fields for sorting |
| `AddLoadFields` | All args are field selectors for loading |
| `LoadFields` | All args are field selectors for JIT load |
| `AreFieldsLoaded` | All args are field selectors for checking |

**Value-consuming methods (NOT excluded, fields collected normally):**

| Method | Why included |
|---|---|
| `TestField` | Reads current field value to compare/validate |
| `FieldError` | May include field value in error message |

### Architecture

1. **CodeFix receives** the diagnostic span
2. **Gets SemanticModel** via `document.GetSemanticModelAsync()` (proven pattern from PossibleOverflowAssigning CodeFix)
3. **Resolves the variable** and record type from the invocation instance
4. **Walks the operation tree** with `FieldAccessCollector` (OperationWalker) to find consumed Normal fields on the variable
5. **FieldAccessCollector** overrides `VisitInvocationExpression` to skip field-selector arguments in known built-in methods
6. **Falls back to PK fields** when no consumed field accesses are found
7. **Sorts fields alphabetically** for deterministic output
8. **Builds** `Record.SetLoadFields(Record.Field1, Record.Field2)` syntax using `SyntaxFactory`
9. **Inserts** before the read statement using `root.InsertNodesBefore()`