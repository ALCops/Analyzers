---
paths:
  - "src/ALCops.PlatformCop/**/UseValidateForFieldAssignment*"
  - "src/ALCops.PlatformCop.Test/Rules/UseValidateForFieldAssignment/**"
---

# PC0037: UseValidateForFieldAssignment

## Purpose

Detects direct field assignments on non-temporary record variables and recommends using `Validate()` instead. Direct assignment bypasses OnValidate triggers and event subscribers, silently breaking business logic in an extensible platform.

Registers `RegisterOperationAction` on `AssignmentStatement` and `CompoundAssignmentStatement`; main type `UseValidateForFieldAssignment`.

**References:**
- [Discussion #259](https://github.com/ALCops/Analyzers/discussions/259)
- [Issue #422](https://github.com/ALCops/Analyzers/issues/422) (`TableType = Temporary` and the `temporary` keyword)

## Design decisions

| Decision | Rationale |
|---|---|
| All fields flagged, including primary-key and system fields | Community consensus: no exceptions, use a pragma for justified cases |
| `temporary` record variables of persisted (`TableType = Normal`) tables are excluded | A real table used as a scratch buffer may carry OnValidate side effects the caller never signed up for ([Discussion #259](https://github.com/ALCops/Analyzers/discussions/259)) |
| `TableType = Temporary` tables stay in scope with or without the `temporary` keyword | Whoever wrote OnValidate on an inherently temporary table meant it to run (BaseApp w1-28: 22 of 211 such tables have triggers, ~30 of those 75 triggers have external effects), and MS applies the keyword inconsistently (0-37% by table vintage), so keying on it would be a loophole ([#422](https://github.com/ALCops/Analyzers/issues/422)); consistent with PC0027 exempting these tables |
| Include-list `{Normal, Temporary}` on `BaseTable.TableType`; externally-backed types (CRM, CDS, ExternalSQL, Exchange, MicrosoftGraph) and an unresolved base table are skipped | Their OnValidate runs only for AL-side changes, so suggesting `Validate()` falsely implies it validates external input ([#369](https://github.com/ALCops/Analyzers/issues/369)); an include-list also auto-excludes future external table types, and silence on unresolved tables avoids false positives |
| Assignment of a field to the current record (`Rec`/`this`/page bare reference) inside that field's own `OnValidate`/`OnBeforeValidate`/`OnAfterValidate` is suppressed; cross-field cascades, `xRec` and other same-table variables still fire | Default values and value transformation there are by design and `Validate()` would recurse ([#357](https://github.com/ALCops/Analyzers/issues/357)); other fields and other records still have subscribers that must run |
| Assignments after `Init()` are not excluded | The Init+assign+Insert pattern should still use `Validate` where possible |
| No `ChangeCompany` handling | Use a pragma for legacy ChangeCompany+write patterns |
| `CompoundAssignmentStatement` registered behind a `!= default` guard | The OperationKind does not exist in the netstandard2.1 SDK; the rule degrades to `:=` only there |

## Deliberate non-reports

- `temporary` variables of `TableType = Normal` tables.
- Records of externally-backed table types (CRM, CDS, ExternalSQL, Exchange, MicrosoftGraph) and records whose base table cannot be resolved.
- The validated field assigned to the current record inside its own validate trigger (table field, page control, and `modify(...)` extension triggers).
- Bare `"Field" := value` inside a table object (no `Rec.`): it binds to `ITableTypeSymbol`, not `IRecordTypeSymbol`, so it never reaches the rule (see Known issues).

## Known issues

- Bare table self field assignment does not fire (accepted limitation pinned by `NoDiagnostic/InsideOnValidateTrigger.al`); page bare references resolve to `Rec` via implicit-with and do fire.
- Do not replace the table-type gate with `IRecordTypeSymbol.IsTemporary()` from Common: that OR-merges the keyword and `TableType` cases this rule must keep apart.
- Detection of `this` and of `Rec` versus `xRec` follows `.claude/rules/record-receiver-forms.md`.

## SDK facts

- `IRecordTypeSymbol.Temporary` reflects the `temporary` keyword only (`RecordTypeSymbol` ctor / `Binder`); `Rec` inside a table object is hard-coded non-temporary even for `TableType = Temporary`.
- `TableType = Temporary` tables are extensible (`SemanticFacts.IsTableTypeExtensible`), so extensions may add `OnBefore`/`OnAfterValidate` to them.
- `OnBeforeValidate`/`OnAfterValidate` are only valid on modified fields/controls (`modify(...)`), not on newly added extension fields (AL0162).
- The change-modify symbol of a `modify(...)` block exposes the modified base field/control through an internal `Target` property (read via `PropertyAccessor.GetPropertyIfExists`).

## Test notes

- `this` fixtures are gated on runtime 14.0; table/page extension fixtures on 13.0 (earlier runtimes reject extensions whose target is declared in the same module).
- Extension fixtures use `modify("Unit Price")` on a base field because AL0162 forbids `OnBefore`/`OnAfterValidate` on added fields.

## CodeFix: UseValidateForFieldAssignmentCodeFixProvider

| Decision | Rationale |
|---|---|
| Rewrite `Rec.Field := Value` to `Rec.Validate(Field, Value)` for simple `:=` only | Compound assignments (`+=`, `-=`) need binary-expression expansion and stay unfixed |
| Reuse the original `SemicolonToken` (a missing token when absent) instead of fabricating one | A then-branch assignment directly before `else` must stay compilable ([#395](https://github.com/ALCops/Analyzers/issues/395)) |
