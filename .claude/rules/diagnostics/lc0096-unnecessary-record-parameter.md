---
paths:
  - "src/ALCops.LinterCop/**/UnnecessaryRecordParameterInMethodCall*"
  - "src/ALCops.LinterCop.Test/Rules/UnnecessaryRecordParameterInMethodCall/**"
---

# LC0096: UnnecessaryRecordParameterInMethodCall

## Purpose

Detects redundant record parameters passed to methods where the same record variable is already the invocation instance. Covers two patterns:

1. **External call**: `MyRecord.MyProcedure(MyRecord)` from any context
2. **Internal call**: `MyProcedure(Rec)` inside tables, pages, and their extensions

Registers `OperationAction` on `OperationKind.InvocationExpression`; main type `UnnecessaryRecordParameterInMethodCall`.

**References:**
- [BusinessCentral.LinterCop LC0094](https://github.com/StefanMaron/BusinessCentral.LinterCop/wiki/LC0094) (original rule)
- [BC.LinterCop PR #1132](https://github.com/StefanMaron/BusinessCentral.LinterCop/pull/1132)

## Design decisions

| Decision | Rationale |
|---|---|
| LinterCop LC0096 (not the original LC0094); Category Usage, Severity Warning | LC0094 was already taken by AllowInCustomizationsRedundancy; a readability smell, not a runtime error. |
| Scope: tables, pages and their extensions; on pages only `local` target methods are flagged | These are the objects with an implicit `Rec`. Public/internal page methods accepting the source record are intentional API design for decoupling and testability, whereas a table *is* the record. |
| Current module only, via `ContainingModule` object equality rather than a `ModuleName` string comparison | Avoids flagging calls into dependency methods the developer cannot refactor. |
| `Rec` matched by `IsSynthesized` + `SemanticFacts.IsSameName`, not by text | Matches only the compiler-generated `Rec` (not user globals named `Rec`) and discriminates it from the equally synthesized `xRec`. |
| A local procedure called with both `Rec` and other record instances is still flagged ([#323](https://github.com/ALCops/Analyzers/issues/323)) | Confirmed by the original rule author. The documented resolution inverts the delegation (implementation in the parameterless procedure, the old overload calls `Param.DoSomething()` on its parameter, keeping `var` aliasing) and needs the procedure to be at least `internal`, because a `local` procedure cannot be invoked on another record instance (AL0161); the overload that copies `Rec` to a local stays documented only for a procedure that must remain `local` with a by-value parameter, since a `var` callee would mutate the copy instead of `Rec`. |
| No special handling for implicit `with` | Implicit `with` only adds `Rec.` as a lookup prefix; it never injects `Rec` as an argument, so `MyProcedure(Rec)` always requires an explicit mention. |
| No CodeFix that removes the argument | It needs the callee signature change, every other caller (invisible for public procedures with dependents, where the removal is an AS0024 breaking change), the `var` versus by-value distinction (by-value is a copy) and a callee body rewrite ([#519](https://github.com/ALCops/Analyzers/issues/519)). |
| No CodeFix that adds the parameterless overload and marks the old one `[Obsolete]` | The host can apply such a multi-document action, but it only reaches an intermediate state: the end state (one procedure without the parameter) remains a manual body refactor, so the inverted delegation is documented as the manual route instead. |

## Deliberate non-reports

- Event publishers: passing `Rec` to events is idiomatic AL and event signatures are public contracts.
- Built-in methods.
- Methods defined in other modules, with no opt-in cross-module mode ([#519](https://github.com/ALCops/Analyzers/issues/519)): the developer cannot change the dependency's signature, and the only call-site change left is de-aliasing through a second variable, which is a `var`-parameter aliasing concern rather than this rule's. The review of the original LinterCop rule (see References) hit exactly these Base App false positives (about 1,500 `X.Proc(X)` call sites in Microsoft's code).
- `xRec` as the argument (`Rec.AssistEdit(xRec)`, `DoSomething(xRec)`): passing the previous record version is a deliberate BC idiom (262 call sites in Microsoft's code), and whether the callee needs `xRec` semantics is intent the analyzer cannot derive. `xRec.Proc(xRec)` is still reported because the instance and argument are the same symbol.
- `RecordRef` arguments: a RecordRef cannot be the receiver of a user procedure, and relating `Proc(RecRef)` to `Rec` after `RecRef.GetTable(Rec)` would need dataflow tracing even though a RecordRef is its own buffer, not `Rec`.
- Public/internal page methods (see design decisions).
- Obsolete symbols (standard ALCops convention).

## Known issues

- `this.MyProc(this)` is a false negative ([#348](https://github.com/ALCops/Analyzers/issues/348)): `GetSymbolSafe()` returns null for both sides, so the identity comparison never matches. Pinned by `NoDiagnostic/ExternalThisSelfMethodCall.al`; fixing it requires detecting `ThisReference` on both sides.
- `DATABASE::MyTable` as an argument relies on `GetSymbolSafe()` (see `.claude/rules/symbol-resolution.md`); covered by the `DatabaseObjectReference` NoDiagnostic fixture.

## SDK facts

- `OperationExtensions.GetSymbol()` has no `OperationKind` case for `BoundThisReference`, so `this` never resolves to a symbol through it.

## Test notes

- The cross-module non-report needs the callee in a second workspace project, so `NoDiagnosticWithDependencyProject` uses `AnalyzerTestFixtureConfig.DependencyProjects` and asserts `NoException` before the marker check; a single-document fixture cannot place the callee in another module.
