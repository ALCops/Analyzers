---
paths:
  - "src/ALCops.LinterCop/**/RecordInstanceIsolationLevel*"
  - "src/ALCops.LinterCop.Test/Rules/RecordInstanceIsolationLevel/**"
---

# LC0031: RecordInstanceIsolationLevel

## Purpose

Flags `LockTable()` on `Record` and `RecordRef` instances and suggests `ReadIsolation(IsolationLevel::UpdLock)`. `LockTable()` sets transaction-wide table state: every subsequent read of that table, on any variable, uses UPDLOCK until commit, and tri-state optimistic reads are disabled for it. `ReadIsolation` is local to one record instance.

Registers `RegisterOperationAction` on `InvocationExpression`; matches built-in methods named `LockTable`.

**References:**
- [Record instance isolation level](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-read-isolation), [Record.LockTable](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/methods-auto/record/record-locktable-method), [Tri-state locking](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-tri-state-locking), [Performance for developers](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/performance/performance-developer) (Microsoft Learn)
- microsoft/BCQuality [prefer-readisolation-over-locktable-for-reads.md](https://github.com/microsoft/BCQuality/blob/main/microsoft/knowledge/performance/prefer-readisolation-over-locktable-for-reads.md) and [do-not-locktable-in-read-only-procedure.md](https://github.com/microsoft/BCQuality/blob/main/microsoft/knowledge/performance/do-not-locktable-in-read-only-procedure.md)
- [#530](https://github.com/ALCops/Analyzers/issues/530) (LockTable in table triggers), [#545](https://github.com/ALCops/Analyzers/issues/545) (receiver-form audit)

## Design decisions

| Decision | Rationale |
|---|---|
| Severity Info, category Design | The call is legal and not deprecated; the replacement narrows lock scope, so it is a suggestion, not a defect |
| Version gate `Spring2023OrGreater` (runtime 11.0) | `ReadIsolation` is a runtime 11.0 built-in (BC22) |
| Self `LockTable()` in table and tableextension triggers is reported on purpose (#530) | The compiler binds bare, `Rec.`, `this.` and named-variable receivers to one `TableClassTypeSymbol` built-in with no self-receiver special case, and the transaction-wide effect is identical inside a trigger. Microsoft's own new code does not write it: W1 app corpus trigger `LockTable()` calls went 80 to 78 from BC 23.5 to 28.4 (two removals, zero additions); Business Foundation, E-Document Core, Subscription Billing and Excise Taxes contain no `LockTable` at all; no Microsoft guidance or community source treats triggers as a special case. The Base Application is legacy here, not the oracle |
| Built-in matched by name only, so `RecordRef.LockTable()` is reported too | `RecordRef` has both `LockTable` and `ReadIsolation` with the same scope difference |
| No dead-call detection | Whether a following read depends on the lock is a judgement the developer makes; the alcops.dev page explains convert versus delete |

### Receiver-form verdicts (#545)

| Form | Analyzer | CodeFix |
|---|---|---|
| Named variable | ok, pinned | ok, pinned |
| `Rec.` (table, tableextension, page, `TableNo` OnRun) | ok, pinned | ok, pinned (table); same path elsewhere, not pinned |
| Bare self in table and tableextension | ok, pinned | fixed (#530), pinned |
| Bare self on page and in `TableNo` OnRun | ok, pinned | `IdentifierNameSyntax` path; page pinned, OnRun not pinned |
| `this.` (table, tableextension) | ok, pinned | ok, pinned |
| Namespaced fully qualified variable | ok, pinned | same path as named variable, not pinned |
| `RecordRef` variable | ok, pinned | ok, pinned |
| `LockTable(true)` (arguments) | ok, pinned | arguments dropped, see Known issues |

## Deliberate non-reports

- `ReadIsolation` itself, in method and property form: the binder rewrites `ReadIsolation := X` into the same `BoundCall` as `ReadIsolation(X)`, and the rule matches the method name only.
- Obsolete code (`IsObsolete()`).

## Known issues

- The fix drops `LockTable(Wait, VersionCheck)` arguments because `ReadIsolation` has no equivalent. Accepted at Info severity; the developer reviews the edit.
- The fix always converts, even when the call is dead. A Remove action was considered and rejected: deleting is a second semantics-changing edit the developer has to judge anyway, and the docs page carries the choice.

## SDK facts

- `TableClassTypeSymbol` declares `LockTable(Wait?, VersionCheck?)` with no version gate and no deprecation, while the same class deprecates `FindSet(ForUpdate, UpdateKey)` with a message; the absence is deliberate. `ReadIsolation` is a property-style built-in (`isProperty: true`) gated on runtime 11.0. `RecordRefClassTypeSymbol` mirrors both (verified against SDK 18.0.41 in `../nav-sdk-source`).
- `Rec` inside a table is synthesized as a plain record of the table's own type (`TableObjectMembers`); all receiver forms bind to one singleton built-in symbol. `Binder.BindAssignmentStatement` rewrites the property form of `ReadIsolation` into the one-argument call.

## Test notes

- `this` fixtures are gated on runtime 14.0 with `SkipTestIfVersionIsTooLow` in both `HasDiagnostic` and `HasFix`.

## CodeFix: RecordInstanceIsolationLevelCodeFixProvider

| Decision | Rationale |
|---|---|
| Single Replace action | Converting never widens locking; deletion is documented, not automated (Known issues) |
| Keep the author's receiver form: `memberAccess.Expression` reused verbatim, bare stays bare | A `Rec.` prefix on the bare form was rejected; `this.` needs no `ThisExpressionSyntax` reference this way (`netstandard21-compatibility.md`) |
| Any other expression shape returns the unchanged document | Never throw from a fix |
| `WellKnownFixAllProviders.BatchFixer` | Each diagnostic replaces its own invocation node; no shared ancestor |
