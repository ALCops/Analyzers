---
paths:
  - "src/ALCops.PlatformCop/**/UseSetAutoCalcFieldsForLoops*"
  - "src/ALCops.PlatformCop.Test/Rules/UseSetAutoCalcFieldsForLoops/**"
---

# PC0035: UseSetAutoCalcFieldsForLoops

## Purpose

Detects `CalcFields` calls inside loop bodies and recommends using `SetAutoCalcFields` before the loop instead. Each `CalcFields` inside a loop generates a separate SQL query per FlowField per iteration, while `SetAutoCalcFields` bundles FlowField calculation into the main SELECT query.

Registers `RegisterCodeBlockAction` on method/trigger bodies; main type `CalcFieldsInLoopWalker`.

**References:**
- [Discussion #74](https://github.com/StefanMaron/BusinessCentral.LinterCop/discussions/74)
- [MS Docs: Record.SetAutoCalcFields](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-setautocalcfields-method)
- [CalcFields vs SetAutoCalcFields](https://www.kauffmann.nl/2014/04/04/calcfields-vs-setautocalcfields/)

## Design decisions

| Decision | Rationale |
|---|---|
| Loops recognized: `FindSet`/`Find` + `repeat-until`, `while-do`, and report `OnAfterGetRecord` (the DataItem is the implicit loop variable) | These are the patterns that iterate over records |
| Only `CalcFields` on the variable driving the loop is flagged | Avoids false positives on other records touched inside the loop |
| `CalcFields` inside `if`/`case` within the loop is skipped; a nested loop resets the conditional depth so its own body counts as unconditional | A conditional `CalcFields` may not run every iteration; accepted false negative for zero false positives, while a nested loop is unconditional relative to itself |
| Severity Warning | Stronger than Info because the perf impact in loops is significant |
| An existing `SetAutoCalcFields` call does not suppress | Always flag `CalcFields` in a loop even if `SetAutoCalcFields` exists |
| No version gate | `SetAutoCalcFields` available since runtime 1.0 |

## Deliberate non-reports

- Temporary records (`temporary` keyword or `TableType = Temporary`): `SetAutoCalcFields` rewrites the SQL SELECT, which in-memory records never issue ([#364](https://github.com/ALCops/Analyzers/issues/364)).
- `CalcFields` inside conditional branches within the loop, even when every branch calls it.
- `CalcFields` on a record passed to another method (cross-method tracking is out of scope).
- `RecordRef` and `foreach`: `RecordRef` has no `CalcFields`, and AL `foreach` works on List/Array only.
- `Rec`, `this` and bare self forms inside a table: the object's own record is always the loop variable there; no separate fixtures.

## Known issues

- Multiple `CalcFields` in the same loop are reported individually, not merged; the CodeFix handles one at a time and Fix All covers the rest.
- No CodeFix is offered when the insertion target is not an element of a statement list (an unblocked then-branch such as `if X then if Y.FindSet() then repeat ...`, [#398](https://github.com/ALCops/Analyzers/issues/398)); wrapping the branch in `begin..end` was rejected as over-engineering because the pattern does not occur in the BaseApp. The diagnostic still appears.

## SDK facts

- `InsertNodesBefore` throws `InvalidOperationException` in the SDK `SyntaxReplacer` when the target node is not in a statement list (`BlockSyntax` or `RepeatStatementSyntax` body).
- The SDK `CodeAction` post-formats only elastic-annotated spans; source nodes (unlike factory-created tokens) carry no elastic trivia.

## Test notes

- Fixtures using the `this` self-reference keyword are gated with `SkipTestIfVersionIsTooLow("14.0")` (runtime 14.0, BC 2024 wave 2).

## CodeFix: UseSetAutoCalcFieldsForLoopsCodeFixProvider

| Decision | Rationale |
|---|---|
| Remove the `CalcFields` statement and insert `SetAutoCalcFields(fields)` before the `FindSet` statement (or the loop itself), passing the arguments through unqualified | Mirrors the recommended pattern; field names are already unqualified in `CalcFields` |
| Reuse the receiver expression from the `CalcFields` member access (trivia stripped) instead of rebuilding it from text | `SyntaxFactory.IdentifierName(expression.ToString())` turned `this.Job` into the quoted identifier `"this.Job"` ([#428](https://github.com/ALCops/Analyzers/issues/428)) |
| Attach `SyntaxFactory.ElasticMarker` leading trivia to the reused node | Without it the inserted statement loses its indentation (see SDK facts) |
| Return null (no fix) when the insertion target is not in a statement list | `InsertNodesBefore` would throw; see Known issues |
