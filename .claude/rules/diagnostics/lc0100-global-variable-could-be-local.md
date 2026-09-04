---
paths:
  - "src/ALCops.LinterCop/**/GlobalVariableCouldBeLocal*"
---

# LC0100: GlobalVariableCouldBeLocal

## Purpose

Reports global variables in normal codeunits whose references are confined to one procedure or trigger when every read is independent of state retained by an earlier invocation. Moving those variables into the only scope that consumes them makes their lifetime and ownership explicit.

**Reference:** [Discussion #499](https://github.com/ALCops/Analyzers/discussions/499)

## Design decisions

| Decision | Rationale |
|---|---|
| Opt-in `Info` diagnostic | The analysis is intentionally advisory and moving a variable still deserves behavioral review. Existing projects should not gain a new warning by default. |
| One self-contained compilation action | Discovering declarations, references, and control flow in the same callback avoids assumptions about callback ordering and remains stable under incremental compilation. |
| Require every reference to bind to the same global and one procedure or trigger | Textual name matches are insufficient because AL is case-insensitive and locals can shadow globals. Cross-scope use proves object-level ownership. |
| Analyze only value-semantic scalar types, labels, and normal non-temporary records | Arrays, `TextConst`, temporary records, and handle-like types can retain or share state that the current flow model cannot prove safe to localize. Conservative exclusion trades recall for no unsafe recommendation. |
| Analyze normal codeunit objects only | Tables, pages, reports, XMLports, queries, extension objects, and non-normal codeunit subtypes have framework-driven triggers, handlers, or lifecycle state that can affect globals without an identifier reference. Test codeunit handlers are a concrete callback example. Object-specific models can expand the scope later. |
| Three-state initialization lattice: unknown, record fields initialized, fully initialized | `Record.Get(...)` and whole-record assignment replace field values but do not establish every part of record context. Normal fields can be read after either operation; FlowFields, FlowFilters, whole-record uses, and other receiver calls require stronger guarantees. |
| Treat non-`Get` record receiver calls as unsafe | Record methods can read or mutate filters, keys, company, marks, temporary data, or trigger-visible state. Modeling each built-in across SDK versions would create a fragile allowlist. |
| Track `Get`-based field initialization together with whole-record assignment | A whole-record assignment in another path can persist record context into a later invocation whose `Get` only refreshes fields. Monotone method-wide flags suppress that cross-invocation combination. |
| Reject reads before definite initialization on every reachable path | Branches merge to the weakest reachable initialization. Optional loops include a zero-iteration path; repeat loops include multiple iterations. Flow-terminating `Error` and `exit` paths do not continue into the merge. |
| Invalidate initialization after every non-modeled invocation | Any invoked AL or built-in operation can synchronously execute extensible code and reenter the same codeunit instance. `Clear`, `ClearAll`, modeled `Record.Get`, and terminating `Error` are handled explicitly; all other calls require a fresh initialization before a later read. |
| Reject `var` arguments, partial writes, prior-value assignments, and stateful execution models | Each can expose or depend on object state even when the source references appear inside one method. Non-normal subtypes, `SingleInstance`, and manual event-subscriber codeunits are skipped as a whole. Recursive calls are handled by the same invocation invalidation as other calls. |
| Skip objects with an event publisher that has `IncludeSender := true`, or an integration event with `GlobalVarAccess := true` | Integration, business, and internal event subscribers can retain the sender instance; integration-event subscribers can additionally receive global access without an identifier reference in the publisher source. Moving a global could silently break that API surface. |
| Treat `ClearAll()` as unsafe | `ClearAll` targets object/global state rather than an equivalent set of future locals, so localization can change which value a later read observes. |
| No CodeFix | Relocating a declaration can affect attributes, dimensions, comments, and state semantics. The diagnostic provides a review target rather than applying a structural edit automatically. |

## Architecture

- Registers a compilation action and visits each declared application object independently.
- Collects source global variables, resolves every identifier through the semantic model, and groups eligible variables by their single containing method or trigger.
- Binds one operation tree per method and evaluates all candidates together with a flow-sensitive `OperationWalker`.
- Reports at the global declaration and includes the variable name, AL type, and method name in the diagnostic message. Labels use wording that does not imply runtime reinitialization.

## Known issues

- The analysis deliberately produces false negatives for unsupported stateful types and record-method patterns rather than risking a state-changing recommendation.
- Every non-modeled invocation invalidates the current initialization proof. This intentionally suppresses suggestions across calls whose purity or callback behavior cannot be proven.
- Loop handling is bounded and conservative. `break`, `continue`, and `AssertError` suppress a recommendation when their control flow cannot be represented safely.
- `Record.Get(...)` is recognized only in the explicitly modeled statement and direct-condition forms with at least one key argument.
- `Error(...)` is considered flow-terminating only when its first argument has a known non-`ErrorInfo` type; collectible errors remain reachable.
