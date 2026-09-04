---
paths:
  - "src/ALCops.FormattingCop/**/UseParenthesisForMethodAssignment*"
  - "src/ALCops.FormattingCop.Test/Rules/UseParenthesisForMethodAssignment/**"
---

# FC0005: UseParenthesisForMethodAssignment

## Purpose

Detects a method that takes a single parameter being invoked using assignment syntax (`target.Method := value;`) and recommends the explicit parenthesised call (`target.Method(value);`). The assignment form hides that a method is being called and would become more ambiguous if AL ever gains real object properties. Applies to built-in methods (`Rec.ReadIsolation := ...`, `currXMLport.TextEncoding := ...`) and user-defined procedures (`MyCodeunit.SetValue := 5`) alike; a CodeFix rewrites the assignment into a parenthesised invocation.

Registers `RegisterOperationAction` on `OperationKind.InvocationExpression`; main type `UseParenthesisForMethodAssignment`.

**References:** [discussion #235](https://github.com/ALCops/Analyzers/discussions/235)

## Design decisions

| Decision | Rationale |
|---|---|
| New rule (FC0005), not an extension of FC0003 | FC0003 covers the no-paren no-arg *call* form (`Rec.LockTable;`); the syntax shape and CodeFix differ. |
| Cover built-in methods and user procedures | Both compile via the same single-parameter assignment binding, and the discussion requests both. |
| `MethodKind.Property` exclusion is defensive only | Genuine properties (`SynthesizedPropertySymbol`) are getters with 0 parameters and cannot reach this analyzer today; the guard prevents a false positive and an invalid `prop(value)` fix (`ERR_PropertyUsedAsMethod`) should the SDK ever expose a settable property. |
| Operation action rather than a syntax action on assignment statements | Only the binder reveals that an assignment is a method call; the operation is already built and the `Syntax.IsKind(AssignmentStatement)` filter is a cheap early reject. |
| Report on the whole assignment statement | That is the `BoundCall`'s syntax and the natural fixable span. |

## Deliberate non-reports

- Compound assignments (`+=`, `-=`, ...): the binder does not rewrite them into a single-parameter method call.
- Obsolete symbols (`ctx.IsObsolete()`), following the standard cop convention.
- Methods with `MethodKind.Property` (see design decisions).

## SDK facts

- `Binder.BindAssignmentStatement`: when the target binds to a `BoundCall` whose method has `ParameterCount == 1`, `target := source` is rebound as `target(source)` via `BindInvocationExpression(..., asProperty: true, ...)`. It surfaces in the operation tree as an `IInvocationExpression` whose `Syntax` is an `AssignmentStatementSyntax` (the whole statement including the semicolon).
- `TextEncoding` (xmlport `currXMLport`) and `ReadIsolation` (record) are built-in single-parameter methods, not properties, so their assignment form is a method call. FC0003's NoDiagnostic fixtures use the assignment form of `TextEncoding` only because FC0003 targets a different shape.

## CodeFix: UseParenthesisForMethodAssignmentCodeFixProvider

| Decision | Rationale |
|---|---|
| The assignment becomes an `ExpressionStatementSyntax` wrapping `InvocationExpression(Target, Source)` with target and source taken `WithoutTrivia()`; the original statement's leading trivia and `SemicolonToken` are reused | Indentation and the trailing newline carried by the semicolon survive without re-synthesising trivia. |
