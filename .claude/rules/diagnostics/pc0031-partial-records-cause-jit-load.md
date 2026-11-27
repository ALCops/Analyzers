---
paths:
  - "src/ALCops.PlatformCop/**/PartialRecordsCauseJitLoad*"
  - "src/ALCops.PlatformCop.Test/Rules/PartialRecordsCauseJitLoad/**"
---

# PC0031: PartialRecordsCauseJitLoad

## Purpose

Detects `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` calls on record variables that subsequently undergo full-field operations (Insert, Delete, Rename, TransferFields, Copy). These operations require all fields on the record to be loaded, so the platform will emit a JIT load if they're not already loaded. This makes the code strictly slower than not using partial records, and can cause "Inconsistent read of field(s)" or "JIT loading of field(s) failed" runtime errors under concurrent access.

Registers `RegisterCodeBlockAction` on method/trigger bodies; main type `PartialRecordsCauseJitLoad` with the `SetLoadFieldsWalker` operation walker. PC0030 (`UsePartialRecordsForLoops`) has its own walker; the two rules share no code beyond `RecordMethodClassification`.

**References:**
- [Issue #264](https://github.com/ALCops/Analyzers/issues/264) (Control flow false positive)
- [Issue #265](https://github.com/ALCops/Analyzers/issues/265) (Modify exclusion)
- [Issue #517](https://github.com/ALCops/Analyzers/issues/517) (roadmap verdict: globals, parameters, cross-procedure, events)
- [Discussion #155](https://github.com/ALCops/Analyzers/discussions/155)
- [MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records)
- [MS Docs: Partial Records FAQ](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records-faq)
- [Demiliani: SetLoadFields Performances](https://demiliani.com/2021/06/03/dynamics-365-business-central-setloadfields-performances-with-reference-passing-or-value-passing-parameters/)
- [microsoft/AL#6893: Inconsistent Read Error](https://github.com/microsoft/AL/issues/6893)

## Design decisions

| Decision | Rationale |
|---|---|
| PlatformCop | Platform runtime behaviour: JIT loading, SQL roundtrips, runtime errors |
| Severity Warning, category Performance | Always slower and sometimes a runtime error: an actionable anti-pattern |
| JIT-load triggers are Insert, Delete, Rename, TransferFields, Copy; Modify, ModifyAll, DeleteAll, Init and `:=` are excluded | MS Docs list exactly "inserts, deletes, renames, field transfers, or copies to temporary records". Modify writes back only changed fields, ModifyAll/DeleteAll are set-based, Init is initialization-only, and `:=` copies the in-memory buffer without touching SQL (confirmed against the decompiled CodeCop Rule0242, which visits assignment RHS only for field access) |
| Full-field operations count only after a partial read (a read while `HasLoadFields` is true) | `SetLoadFields` affects only the next read; writes before it or between it and the next read use the full buffer already loaded, so `Get(); SetLoadFields(); Delete();` is fine while `SetLoadFields(); Get(); Delete();` JIT-loads |
| Condition-aware narrowing: when the read is the `if` condition, the partial read applies only to the "found" branch | The not-found branch of `if [not] Rec.Find/Get() then` has an empty buffer, so full-field operations there do not JIT-load ([#264](https://github.com/ALCops/Analyzers/issues/264)) |
| Full-field operations inside conditional branches (`if`/`case`/`while`/`for`/`foreach` bodies) are not recorded; `repeat-until` bodies are | Find-or-create (`if not FindFirst() then Insert()`), boolean-result and conditional-delete patterns would otherwise be noise; `repeat-until` always executes once, so the JIT load is guaranteed. Accepts false negatives inside branches for zero false positives ([#264](https://github.com/ALCops/Analyzers/issues/264)) |
| Otherwise union semantics across branches: `SetLoadFields` on any path plus a full-field operation on any path after a partial read | Conservative for complex conditions not handled by the narrowing |
| Report at each `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` call, naming the operations; each location is charged with the JIT-load writes that followed it on the path | Where the developer makes the change. Findings are recorded at the write, per location, so a load-fields call that no write follows is never reported, and a write is attributed only to the load-fields calls that preceded it |
| `SetLoadFields()` without arguments resets the flow state; findings already recorded at a write survive | No-argument `SetLoadFields` cancels partial records entirely; the next read loads all fields. Findings are recorded at the write, not at the end of the walk, so the reset cannot erase them |
| Mutually exclusive with PC0030 on the same variable | Full-field operations suppress PC0030 and are the trigger for PC0031, so a variable never gets both suggestions |
| Flow-sensitive forward dataflow with fork/merge at `if`/`case`/`while`/`for`/`foreach`; `while`/`for`/`foreach` merge the pre-loop state with the post-body state, `repeat-until` uses the post-body state directly | Positional flags gave wrong answers when `SetLoadFields`, `Clear` or the read sat in one branch only; a `repeat` body always runs once |
| `HasLoadFields` merges with AND, `HasPartialRead` and the load-fields locations with OR (union); writes take no part in merges because they are recorded at branch depth 0 only | If any path lacks `SetLoadFields`, the read may load all fields; a partial read on any path can reach a later write, so a hand-off in one branch does not clear the other path's partial read |
| `Clear(var)` and `var.Reset()` reset the flow state; findings already recorded at a write survive | Both reinitialize the partial-records state of the variable. Findings are recorded at the write, not at the end of the walk, so the reset cannot erase them |
| Hand-offs that can change the instance's partial state reset it: `var` arguments to user procedures and event publishers, any built-in that takes the variable as an argument, including ones that only read it (`CopyFilters`, `GetTable`), and non-built-in methods on the record; by-value `Record` arguments do not reset, by-value `RecordRef` arguments do | The callee may call `SetLoadFields()`, `Reset` or a full read on the caller's instance, and a false positive is worse than a missed finding. The set of reference-passing built-ins is not enumerable with certainty, and a missed finding is cheaper than a false positive. A by-value `Record` argument is a copy with its own load selection and cannot change the caller's instance ([MS Docs: Using Partial Records](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-partial-records), "Iterating over records"). Findings are recorded at the write, so a `var` publisher raised after it (`Delete(); OnAfterDelete(MyTable)`) keeps the report. A `RecordRef` assigned or passed by value refers to the same table instance ([MS Docs: RecordRef data type](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/recordref/recordref-data-type)), so the callee can reset it |
| Every read kind (`Find*`, `Get`, `GetBySystemId`) marks a partial read; the set is the analyzer's own `ReadMethods`, not PC0030's loop-entry set | Any read after `SetLoadFields` leaves a partial buffer, whether or not a loop follows |
| `RecordRef` variables are included, without temporary/table-type checks | The opened table is not statically determinable; `RecordRef.SetLoadFields` followed by a JIT-load operation is the same anti-pattern |
| Single pass over loops, no fixed-point iteration | Acceptable approximation; fixed-point adds complexity for rare edge cases |
| Local variables only; bound receivers must be `SymbolKind.LocalVariable` before the name-keyed map is consulted, text lookups (argument names) stay name-based | `this.<Global>` and `this.<DataItem>` reach an object-scope symbol that may share a local's name, so without the kind check a global's operations were charged to the local. A bare identifier always binds innermost, so text lookups cannot alias. The synthesized `OnRun` `Rec` of a `TableNo` codeunit is a `LocalVariable` too |
| Version gate `Spring2021OrGreater` (runtime 6.0, BC17); full netstandard2.1 support (`LoadFieldsInfo` has the `#if NETSTANDARD2_1` struct twin) | `SetLoadFields` was introduced with runtime 6.0; same gate as PC0030 |

## Deliberate non-reports

- Modify, ModifyAll, DeleteAll, Init and `:=` after a partial read: none of them needs all fields loaded.
- Full-field operations that precede the first partial read, or sit between `SetLoadFields` and the next read.
- Full-field operations inside conditional branches, including the not-found branch of `if not Rec.Get() then Insert()`.
- Globals and parameters whose load state, read and write all sit in one body: no occurrence in the Base Application (0 same-body global hits, about one parameter hit in 1,663 load-fields call sites), not worth the extra bail-out on same-object calls a global would need.
- Globals across procedures: whether a write JIT-loads depends on which procedure ran first on the instance and on the instance's lifetime (single-instance codeunits, pages, test codeunits), which is invisible statically.
- Parameters whose partial state comes from the caller: the caller is unknown; 1,554 Base Application helpers take a `var` record and write it without reading it first, so reporting them would be noise.
- Flows through other procedures: a dependency procedure symbol has no body (`ReferenceMethodSymbol.DeclaringSyntaxReference` is null) and same-app callees would cost a body bind per call site on every editor pass; the Base Application has one such flow.
- Event subscribers: the publisher's load state is invisible to the subscriber, and the publishers raised with a partial record are deliberate extension points; the subscriber-side remedy is `AddLoadFields`.

## Known issues

- Genuine JIT-load problems inside conditional branches are false negatives by design; straight-line and `repeat-until` cases are still caught.
- A hand-off that only reads the record (a `var` helper that deletes without resetting) suppresses the finding: accepted false negative.
- A built-in that only reads the variable (`Other.CopyFilters(MyTable)`) between the partial read and the write suppresses the finding: accepted false negative.

## Test notes

- The `this` fixtures are gated on runtime 14.0 (`SkipTestIfVersionIsTooLow`).

## CodeFix: PartialRecordsCauseJitLoadCodeFixProvider

| Decision | Rationale |
|---|---|
| Remove the whole `ExpressionStatementSyntax` containing the load-fields call | Without partial records, full-field operations work normally; simple and safe |
| FixAll via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |
