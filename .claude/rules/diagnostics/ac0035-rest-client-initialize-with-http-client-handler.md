---
paths:
  - "src/ALCops.ApplicationCop/**/RestClientInitializeWithHttpClientHandler*"
  - "src/ALCops.ApplicationCop.Test/Rules/RestClientInitializeWithHttpClientHandler/**"
---

# AC0035: RestClientInitializeWithHttpClientHandler

## Purpose

Reports a `Codeunit "Rest Client"` variable that is used or initialized without a custom `"Http Client Handler"`: `Initialize`/`Create` without a handler argument, with the System Application's default codeunit 2360 `"Http Client Handler"`, or no initialization at all before use. Such a client sends through the System Application, so the external-call permission, outgoing web-service telemetry and test mocking belong to the System Application instead of the calling app. AC0033 checks that the app has a handler at all; this rule checks that each variable receives one.

Registers `RegisterSymbolAction` on Codeunit, Table, TableExtension, Page, PageExtension, Report, ReportExtension, Query and XmlPort; main type `TrackedWalker` (an `OperationWalker` per root, sharing one `RootState`).

## Design decisions

| Decision | Rationale |
|---|---|
| Roots are locals (any method or trigger body) and globals of the object; parameters and return values are never roots | The caller owns a parameter's initialization and the callee of a return value decides what to do with it; reporting them would flag every helper that receives a ready client. |
| Good = an argument bound to an `Interface "Http Client Handler"` parameter whose static type is not codeunit 2360 `"Http Client Handler"` (id and name) | Interface-typed values, factory results, parameters and codeunits from any app cannot be proven to be the default handler; tracing their origin was rejected as unbounded. |
| Silence (root becomes unknown) when it is passed to anything not followable (another object, a method without source, an interface method, an event, a built-in such as `Clear`), assigned from anything but `Root.Create(...)`, assigned to another variable, returned via `exit`, or an invocation on it is invalid | Past those points the rule no longer sees every call on the instance; reporting anyway was rejected because it would flag clients initialized elsewhere. |
| A root passed to a procedure declared inside the same object syntax is followed into the callee, `var` and by-value alike, unlimited depth, cycle-safe | Codeunit variables are reference types, so a by-value callee initializes the caller's instance too. Containment is checked on syntax (same tree, span contains the callee), not by comparing `GetContainingApplicationObjectTypeSymbol()`, which is null for request-page procedures. |
| Location: the first default `Initialize`/`Create` found directly on the root; otherwise the variable's name | The call is where the fix goes. A default call inside a followed callee reports at the root's declaration, because the callee's call also serves other callers. |
| A default call is reported even when a good call exists elsewhere on the same root | `Initialize()` after `Initialize(MyHandler)` replaces the handler; ordering analysis was rejected as too costly for the gain. |
| No ordering analysis and no request-method list: any other member invocation marks the root used | A list of `Get`/`Post`/`Send`... would miss new members; "used before initialization" order is not tracked (a good initialization anywhere counts). |
| Roots discovered on symbols (`LocalVariables`, `GetMembers()`), with a body-text pre-filter only for globals | A declaration can spell the type `Codeunit 2350`, so text cannot find roots; a reference to a global always spells its name, so skipping bodies that do not contain it is sound and saves binding. |
| One symbol action per object instead of a code-block action | A global's initialization and use live in different bodies; a per-object callback analyzes and reports in one place (`sdk-analysis-scope.md`) and binds only objects that declare a root. |
| Warning, enabled by default, `Category.Design`, no settings, no CodeFix, no version gate | Same impact class as AC0033; the right handler codeunit cannot be generated; every SDK member used exists at ns2.0 / AL 12.0 (nav-sdk-docs `reference/`). |

## Deliberate non-reports

- Parameters and return values of type `Codeunit "Rest Client"` (not roots).
- A root passed to `Clear` or any other built-in, to another object, an interface method or an event publisher (unknown).
- `Codeunit::"Rest Client"` object references without a variable.
- `array[n] of Codeunit "Rest Client"`, `List of [...]` and interface-typed variables: the variable type is not the codeunit symbol.
- A declared but never used root without an `Initialize`/`Create` call.
- Test and test-runner codeunits, obsolete objects and locals of obsolete procedures.

## Known issues

- A same-named codeunit 2360 from another module would count as the default handler (id and name match only); accepted.

## SDK facts

- `RestClient.Initialize;` without parentheses in statement position binds to an `IInvocationExpression` whose `Syntax` is the `MemberAccessExpressionSyntax` (verified with the `LocalInitializeWithoutParentheses` fixture, net10.0 SDK).
- `ISymbol.GetLocation()` on a local or global variable is the name identifier, not the whole declaration (verified by dumping fixture diagnostics).
- `IArgument.Parameter` is null when no parameter could be matched (docs: nav-sdk-docs `docs/40-operations/invocation-and-arguments.md`); arguments bound to an interface parameter are wrapped in `IConversionExpression`, so values are compared after `UnwrapConversions()`.
- The SDK has no `SymbolEqualityComparer`; `ISymbol.Equals` is the only equality contract (docs: nav-sdk-docs `docs/20-symbols/symbol-hierarchy.md`).

## Test notes

- `ThisReceiverProcedureCall` is gated on 14.0 (the `this` keyword).
- Fixtures declare their own System Application stubs with the real ids (2350 `"Rest Client"`, 2360 `"Http Client Handler"`, 2358 `"Http Authentication Anonymous"`); no stub implements both interfaces, so `Initialize`/`Create` overloads resolve unambiguously. A user procedure named `Run` collides with the built-in codeunit `Run` (AL0440).
