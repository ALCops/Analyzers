---
applyTo: 'src/ALCops.LinterCop/**/CognitiveComplexity*'
---

# LC0089/LC0090 Cognitive Complexity

## Purpose

LC0089 reports cognitive-complexity metrics, LC0089i reports individual increments, and LC0090 reports when the configured threshold is reached.

## Diagnostic properties

| Property | Value |
|----------|-------|
| IDs | LC0089, LC0089i, LC0090 |
| Category | Design |
| Severity | Hidden (LC0089/LC0089i), Warning (LC0090) |
| Enabled | LC0089/LC0089i: No; LC0090: Yes |
| CodeFix | No |

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Resolve `Error(...)` and `FieldError(...)` guard clauses through the semantic model | User-defined procedures with the same names must retain their cognitive-complexity increment. The complete code expression is resolved so AL calls without parentheses are also covered. |
| Accept an `Error`/`FieldError` target when its name is in `FlowTerminatingBuiltIns.MethodNames` and it has no `DeclaringSyntaxReference`, instead of requiring `MethodKind.BuiltInMethod` | When an argument fails to bind (undefined variable, wrong arity, mid-edit), `Binder.CreateBadCall` synthesizes an `ErrorMethodSymbol` with `MethodKind.Method` for the two-overload built-ins `Dialog.Error`, `Table.FieldError` and `FieldRef.FieldError`; requiring `BuiltInMethod` made `if X then Error(UndefinedVar)` score +1 and LC0089/LC0090 flicker while typing. Only user-defined procedures have a `DeclaringSyntaxReference`, and AL has no user overloads, so a user procedure with bad arguments still binds to its own symbol and is demoted. |
| Use `context.SemanticModel` from the code-block context | `Compilation.GetSemanticModel` creates an uncached `SyntaxTreeSemanticModel` on every call, so obtaining a model per code block re-bound each procedure from scratch; the code-block context already carries the model for its tree. |
| Keep `exit`, `continue`, and `CurrReport`/`CurrXMLport` commands syntactic | Their existing syntax-specific behavior is unchanged; semantic resolution is limited to the shared built-in terminator names. |

## Architecture

- Registers a code-block action and walks method and trigger syntax iteratively.
- Uses the semantic model supplied by the code-block context; no models are created by the analyzer.
- `IsGuardExpression` binds the `then` expression once; an `IInvocationExpression` whose target is named in `FlowTerminatingBuiltIns.MethodNames` and is not source-declared is a guard, everything else falls through to the lexical `Break`/`Continue`/`Quit`/`Skip` checks.

## Known issues

- An `Error`/`FieldError` call whose receiver is itself unresolved (for example `Foo.Error(x)` with `Foo` never declared) is not recognised as a guard clause and scores +1 until the receiver is declared.

## Roadmap

- Unify the lexical and semantic guard models. `Break`, `Continue`, `Quit` and `Skip` (and the `CurrReport`/`CurrXMLport` receivers) are still matched purely lexically.

## Test coverage

**HasDiagnostic (9 cases):** ConditionalExpressionNested, IfStatement, IfStatementNested, RecursionDirect, RecursionIndirect, RecursionDirectWithoutParentheses, RecursionIndirectWithoutParentheses, UserDefinedErrorNotGuardClause, UserDefinedFieldErrorNotGuardClause.
**NoDiagnostic (9 cases):** CurrReportGuardClause, CurrXMLportGuardClause, IfStatement, DiscountConsecutiveAndOperator, IfStatementElseIf, IfStatementGuardClause, IfStatementGuardClauseFieldRefFieldErrorWithoutParentheses, IfStatementGuardClauseContinue, IfStatementGuardClauseFieldError.
**HasDiagnosticInDocumentWithErrors (1 case):** UserDefinedErrorNotGuardClauseUnboundArgument.
**NoDiagnosticInDocumentWithErrors (2 cases):** IfStatementGuardClauseErrorUnboundArgument, IfStatementGuardClauseFieldErrorUnboundArgument.

## CodeFix

No CodeFix is provided.
