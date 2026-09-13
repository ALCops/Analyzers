---
paths:
  - "src/ALCops.TestAutomationCop/**/InvokeActionOnPartTestPage*"
  - "src/ALCops.TestAutomationCop.Test/Rules/InvokeActionOnPartTestPage/**"
---

# TA0002: InvokeActionOnPartTestPage

## Purpose

Reports `Invoke()`, `Enabled()` and `Visible()` calls on an action of a `TestPage` variable whose target page has `PageType = ListPart` or `CardPart`. A part page opened directly through its own `TestPage` renders no actions at runtime, so the test fails with "The action with ID = xxx is not found on the page." The working pattern is to invoke the action through the part control of the hosting page (`HostPage.SubPagePart.MyAction.Invoke()`).

Registers `RegisterOperationAction` on `InvocationExpression`; main type `InvokeActionOnPartTestPage`.

**References:** https://github.com/ALCops/Analyzers/discussions/455

## Design decisions

| Decision | Rationale |
|---|---|
| Register on `InvocationExpression` rather than a hypothetical `TestActionAccess` operation kind | `IOperation` has no `Parent`, so the action access alone cannot reach the containing `Invoke`/`Enabled`/`Visible` call; the invocation has both the target method identity and the `ITestActionAccess` instance. Covers both `Invoke()` and `Invoke` (without parentheses) because `IInvocationExpression` represents both spellings. |
| All three `TestAction` methods (`Invoke`, `Enabled`, `Visible`) | They are the only members of the built-in `TestAction` class; all three fail identically at runtime on a directly opened part page. |
| Type-based reach (every body, no `Subtype = Test` gate) | `TestPage` variables can appear in helper codeunits that are not `Subtype = Test`; gating on subtype would miss those. |
| Only `ListPart` and `CardPart` page types | `HeadlinePart` is not confirmed to fail the same way; silence is safer. A page without an explicit `PageType` property defaults to `Card`, which is not a part type and is therefore silent. |
| Anchor on the `TestAction` built-in class symbol, not on the method name | A name-only check could match a future built-in on another class; the class identity is stable. |
| Obsolete part page is still reported | The runtime failure is real even when the page is `ObsoleteState = Pending`; only obsolete test code (`ctx.IsObsolete()`) is skipped. |
| Severity `Warning`, category `Usage` | The call always fails at runtime, but Warning (not Error) matches the convention for rules that do not prevent compilation. |
| No CodeFix | The fix requires knowing the hosting page and its part control name; no mechanical rewrite exists. |
| No `PageTypeKind` sentinel hardening | A missing `PageTypeKind` member would only cause silence (the equality check fails); the sentinel pattern is unnecessary here. |

## Deliberate non-reports

- Actions reached through a part control (`MainPage.SubPagePart.MyAction.Invoke()`): the `ITestActionAccess.Instance` is typed `TestPart` (`NavTypeKind.TestPart`), not `TestPage`, so the `NavTypeKind` check excludes it.
- Field access, `OpenView`/`OpenEdit`/`OpenNew` and all other `TestPage` built-ins on a part: these are not `TestAction` members and pass through the `ContainingSymbol` class-name check.
- Built-in actions `OK`/`Cancel`/`Yes`/`No`/`View`/`Edit`: `SubPage.OK().Invoke()` has an invocation (the `OK()` call) as the `Instance` of the outer `Invoke`, not an `ITestActionAccess`.
- `HeadlinePart` and every other `PageType` not confirmed to fail.
- `TestRequestPage`: its receiver type is `RequestPageTypeSymbol`, which is `IPageBaseTypeSymbol` but not `IPageTypeSymbol`; the `OriginalDefinition is not IPageTypeSymbol` cast bails out.
- Obsolete test code: `ctx.IsObsolete()` returns true when the enclosing method or object is obsolete, skipping the diagnostic.

## Test notes

- `ListPartActionFromPageExtension` is skipped below runtime 13.0: AL 12 rejects a pageextension whose target page is declared in the same module (AL0334).

## SDK facts

- `TestPage "X"` type is the internal `TestPageTypeSymbol`: `NavTypeKind.TestPage`, `OriginalDefinition` is the `PageTypeSymbol` (public `IPageTypeSymbol`, `PageType` on `IPageBaseTypeSymbol`). No public `ITestPageTypeSymbol`.
- `X.SomeAction` binds to `BoundTestActionAccess : ITestActionAccess` (`Instance`, `ActionSymbol : IActionSymbol`).
- `MainPage.Part` binds to `BoundTestPartAccess : ITestPartAccess`; its `Type` is `TestPartSymbol` (`NavTypeKind.TestPart`). For `MainPage.Part.Action.Invoke()` the `ITestActionAccess.Instance` is typed `TestPart`; for `SubPage.Action.Invoke()` it is typed `TestPage`.
- `.Invoke()`/`.Enabled()`/`.Visible()` are the only three members of the built-in class `TestAction` (`Symbols/TestActionClassTypeSymbol.cs:7-12`); the call is an `IInvocationExpression` with `TargetMethod.MethodKind == BuiltInMethod`, `ContainingSymbol` the `IClassTypeSymbol` named `TestAction`.
- `OK`/`Cancel`/`Yes`/`No`/`View`/`Edit` are `TestPage` built-ins returning `TestActionType`: `SubPage.OK().Invoke()` has an invocation as `Instance`, never an `ITestActionAccess`.
- Pageextension actions are folded into the test page members; classify by receiver type, not by `IActionSymbol.ContainingSymbol`.
- `TestRequestPage` is `RequestPageTypeSymbol`, which is `IPageBaseTypeSymbol` but not `IPageTypeSymbol`, so the cast bails out silently.
- Bare `SubPage.SomeAction;` does not compile, so every `TestAction` access is one of the three calls.
