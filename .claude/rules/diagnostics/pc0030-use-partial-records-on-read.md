---
paths:
  - "src/ALCops.PlatformCop/**/{UsePartialRecordsOnRead,PartialRecordOperations}*"
  - "src/ALCops.PlatformCop.Test/Rules/UsePartialRecordsOnRead/**"
---

# PC0030: UsePartialRecordsOnRead

## Purpose

Recommends using `SetLoadFields` (or `AddLoadFields`/`SetBaseLoadFields`) before read operations on local record variables. Without partial records, the runtime loads ALL normal fields including those from table extensions, causing unnecessary SQL joins and 2-9x slower data access.

Registers `RegisterCodeBlockAction` on method/trigger bodies; main type `PartialRecordOperations` (shared with PC0031) with the `SetLoadFieldsWalker` operation walker.

**References:**
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [Community discussion](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/218)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Record.SetLoadFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setloadfields-method)

## Design decisions

| Decision | Rationale |
|---|---|
| Local variables only | Global variables need cross-method analysis; parameters need the calling context |
| Complement to CodeCop AA0242: fires only when `SetLoadFields` is entirely absent | AA0242 already covers the case where `SetLoadFields` is present but misses accessed fields (JIT loads) |
| Severity Info | Performance suggestion, not a correctness issue |
| Report on the read operation alone; no field access required | User preference; accepted noise on patterns like `if Rec.Get(Key) then` |
| Any pass of the variable to any invocation, including built-in methods like `PAGE.Run`, suppresses it; only bare variable arguments count, not field accesses | Conservative: the callee might access any field, while `SetRange(MyTable."No.")` only reads one field |
| `RecordRef` variables are included, without temporary/table-type checks | The opened table is not statically determinable for a `RecordRef` |
| `RecordRef.SetTable(Record)` (single-argument overload) links the `RecordRef` to the target record's suppression state; the target must be local and resolvable | If the target record has write ops, load fields, or is passed to a function, suggesting `SetLoadFields` on the `RecordRef` could interfere |
| Flow-sensitive forward dataflow with fork/merge at branches, all flags (`HasLoadFields`, `HasFullRecordAccess`, `PassedToFunction`) flow-aware | Positional flags caused false negatives when `SetLoadFields` came after the read, `Clear` sat between them, or one branch had it; all flags share the same positional bug class |
| `HasLoadFields` merges with AND (intersection) | If any path lacks `SetLoadFields`, the read may execute without it |
| `HasFullRecordAccess`/`PassedToFunction` merge with OR (union) | If any path writes, assigns or passes the variable, suggesting `SetLoadFields` could interfere |
| Pre-fork uncovered reads merge by intersection, in-branch reads by union | A read before a fork that is retroactively cleared in any branch stays cleared (`if FindSet then repeat Modify`), while a read added inside a branch is genuinely uncovered on that path |
| Reads are evaluated immediately but cleared retroactively when a later write, pass or whole-record assignment is met | Handles `Get(); Modify()` where the full-record operation follows the read |
| Whole-record assignment with the variable as source (`TempMyTable := MyTable`) counts as full-record access | The copy reads all fields; a preceding partial read would produce a partial copy. Only bare variable references on the right-hand side, not field access |
| `exit(Rec)` clears uncovered reads on the current path only, sets no forward flag, and only for bare variable references | The path terminates at `exit`, so a forward flag would leak suppression past the enclosing branch via OR-merge and hide reads after an early-exit guard ([#429](https://github.com/ALCops/Analyzers/issues/429)); `exit(Rec."No.")` reads one field |
| `Clear(var)` and `var.Reset()` reset all flags; `var.SetLoadFields()` without arguments resets `HasLoadFields` only | `Clear`/`Reset` reinitialize the variable; a no-argument `SetLoadFields` only cancels the partial-records state |
| `while`/`for`/`foreach` merge the pre-loop state with the post-body state; `repeat-until` uses the post-body state directly | The former bodies might not execute, the latter always executes at least once |
| Single pass over loops, no fixed-point iteration | Acceptable approximation; fixed-point adds complexity for rare edge cases |
| `RecordRef` suppression stays method-level (`EverHad*` flags) rather than flow-sensitive | `SetTable` linkage is already complex; flow fixes concentrate on `Record` variables |
| Parameterless `Get()` on a setup table is suppressed; `FindFirst`/`FindSet` still fire | Near-zero SQL benefit for single-record cached tables (BaseApp uses `SetLoadFields` on them in 1.8% of cases). Heuristic via `TableHelper.IsSetupTable()`: single Code PK field named "Primary Key", or a parameterless return-less `GetRecordOnce` method declared on the table itself so referenced-app symbols work ([#283](https://github.com/ALCops/Analyzers/issues/283), [#287](https://github.com/ALCops/Analyzers/issues/287)) |
| Version gate `Spring2021OrGreater` (runtime 6.0, BC17); full netstandard2.1 support | `SetLoadFields` was introduced with runtime 6.0 |

## Deliberate non-reports

- Global variables and parameters: only locals are tracked.
- Temporary records (`temporary` keyword or `TableType = Temporary`) and non-SQL table types (CDS, Exchange, etc.): no SQL backing, no table-extension join to save; matches AA0242.
- Records passed to any invocation, including built-in methods such as `PAGE.Run`: the callee may access any field.
- Variables with any write (`Insert`, `Modify`, `ModifyAll`, `Delete`, `DeleteAll`, `Rename`, `TransferFields`, `Init`, `Copy`), whole-record assignment source, or `exit(Rec)` on the path.
- Parameterless `Get()` on setup tables.
- `Rec`, `this` and bare self forms inside tables and tableextensions: they are object-scope globals, never entered into the local-variable map; no realistic use for `SetLoadFields` on the object's own record ([#348](https://github.com/ALCops/Analyzers/issues/348)).
- `RecordRef.SetTable(Record, Boolean)` (ShareTable overload), `RecordRef.GetTable`, and `ClearAll()`: out of scope; revisit if users report false positives.

## Known issues

- Merge points deduplicate uncovered reads by `SourceSpan.Start` (`HashSet<int>`): without it, concatenating both branches doubled the list at each nesting level (2^K after K levels) and threw `OutOfMemoryException` in `FlowFlags.Clone()` on the Base App.
- Name-keyed tracking means `this.FindSet()` next to a local variable named like the table shares a key; not fixed, no real-world occurrence found.

## CodeFix: UsePartialRecordsOnReadCodeFixProvider

| Decision | Rationale |
|---|---|
| Insert `Record.SetLoadFields(...)` as a new statement immediately before the read's containing statement | Matches the MS Docs convention |
| `Record` variables only, not `RecordRef` | `RecordRef.SetLoadFields` takes integer field numbers that are not statically determinable |
| Field list = Normal fields whose values are consumed in the method body, resolved in the CodeFix via `Document.GetSemanticModelAsync()`; sorted alphabetically (case-insensitive) | Zero analyzer overhead; deterministic output regardless of source order. FlowField, FlowFilter and Blob are skipped because `SetLoadFields` returns false for them |
| Two built-in selector sets are excluded from consumption: first-argument selectors (`SetRange`, `SetFilter`, `GetFilter`, `FieldCaption`, ...) and all-argument selectors (`SetCurrentKey`, `AddLoadFields`, ...); `TestField`/`FieldError` remain consumers | A field named as a filter or key selector is not read from the buffer, but a field passed as a filter *value* (`SetRange(F1, MyTable.F2)`) is. Only built-in methods are excluded because user-defined calls already suppress the diagnostic |
| Fallback to primary-key fields when no consumed field is found (including all-selector cases) | Establishes the partial-records pattern; PK fields are always loaded |
