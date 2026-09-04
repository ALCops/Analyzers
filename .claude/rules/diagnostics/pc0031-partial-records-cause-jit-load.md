---
paths:
  - "src/ALCops.PlatformCop/**/PartialRecordsCauseJitLoad*"
  - "src/ALCops.PlatformCop.Test/Rules/PartialRecordsCauseJitLoad/**"
---

# PC0031: PartialRecordsCauseJitLoad

## Purpose

Detects `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` calls on record variables that subsequently undergo full-field operations (Insert, Delete, Rename, TransferFields, Copy). These operations require all fields on the record to be loaded, so the platform will emit a JIT load if they're not already loaded. This makes the code strictly slower than not using partial records, and can cause "Inconsistent read of field(s)" or "JIT loading of field(s) failed" runtime errors under concurrent access.

Registers `RegisterCodeBlockAction` on method/trigger bodies; main type `PartialRecordOperations` (shared with PC0030, see `pc0030-use-partial-records-on-read.md` for the shared flow analysis) with the `SetLoadFieldsWalker` operation walker.

**References:**
- [Issue #264](https://github.com/ALCops/Analyzers/issues/264) (Control flow false positive)
- [Issue #265](https://github.com/ALCops/Analyzers/issues/265) (Modify exclusion)
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
| Report at each `SetLoadFields`/`AddLoadFields`/`SetBaseLoadFields` call, naming the operations | Where the developer makes the change |
| `SetLoadFields()` without arguments fully resets PC0031 state | No-argument `SetLoadFields` cancels partial records entirely; the next read loads all fields |
| Mutually exclusive with PC0030 on the same variable | Full-field operations suppress PC0030 and are the trigger for PC0031, so a variable never gets both suggestions |
| Local variables only | Same scope as PC0030 |
| Version gate `Spring2021OrGreater` (runtime 6.0, BC17); full netstandard2.1 support | `SetLoadFields` was introduced with runtime 6.0; same gate as PC0030 |

## Deliberate non-reports

- Modify, ModifyAll, DeleteAll, Init and `:=` after a partial read: none of them needs all fields loaded.
- Full-field operations that precede the first partial read, or sit between `SetLoadFields` and the next read.
- Full-field operations inside conditional branches, including the not-found branch of `if not Rec.Get() then Insert()`.
- Global variables and parameters.

## Known issues

- Genuine JIT-load problems inside conditional branches are false negatives by design; straight-line and `repeat-until` cases are still caught.

## CodeFix: PartialRecordsCauseJitLoadCodeFixProvider

| Decision | Rationale |
|---|---|
| Remove the whole `ExpressionStatementSyntax` containing the load-fields call | Without partial records, full-field operations work normally; simple and safe |
| FixAll via `WellKnownFixAllProviders.BatchFixer` | Standard pattern |
