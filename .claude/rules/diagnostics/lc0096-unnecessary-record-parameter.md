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
| A local procedure called with both `Rec` and other record instances is still flagged ([#323](https://github.com/ALCops/Analyzers/issues/323)) | Confirmed by the original rule author; the resolution is a parameterless overload that copies `Rec` to a local and delegates (a local variable argument does not re-trigger). Documented on the alcops.dev page. |
| No special handling for implicit `with` | Implicit `with` only adds `Rec.` as a lookup prefix; it never injects `Rec` as an argument, so `MyProcedure(Rec)` always requires an explicit mention. |
| No CodeFix | Removing the argument breaks compilation unless the callee signature changes too. |

## Deliberate non-reports

- Event publishers: passing `Rec` to events is idiomatic AL and event signatures are public contracts.
- Built-in methods and methods defined in other modules.
- Public/internal page methods (see design decisions).
- Obsolete symbols (standard ALCops convention).

## Known issues

- `this.MyProc(this)` is a false negative ([#348](https://github.com/ALCops/Analyzers/issues/348)): `GetSymbolSafe()` returns null for both sides, so the identity comparison never matches. Pinned by `NoDiagnostic/ExternalThisSelfMethodCall.al`; fixing it requires detecting `ThisReference` on both sides.
- `DATABASE::MyTable` as an argument relies on `GetSymbolSafe()` (see `.claude/rules/symbol-resolution.md`); covered by the `DatabaseObjectReference` NoDiagnostic fixture.

## SDK facts

- `OperationExtensions.GetSymbol()` has no `OperationKind` case for `BoundThisReference`, so `this` never resolves to a symbol through it.
