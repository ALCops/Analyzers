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
| Resolve `Error(...)` and `FieldError(...)` guard clauses through the semantic model | The shared `FlowTerminatingBuiltIns` classifier requires `MethodKind.BuiltInMethod`, so user-defined procedures with the same names retain their cognitive-complexity increment. The complete code expression is resolved so AL calls without parentheses are also covered. |
| Obtain one semantic model per code block | Avoids repeated compilation lookups while resolving only candidate guard-clause invocations. |
| Keep `exit`, `continue`, and `CurrReport`/`CurrXMLport` commands syntactic | Their existing syntax-specific behavior is unchanged; semantic resolution is limited to the shared built-in terminator classification. |

## Architecture

- Registers a code-block action and walks method and trigger syntax iteratively.
- Resolves a semantic model once for each analyzed code block.
- Uses `FlowTerminatingBuiltIns.IsFlowTerminatingCall` only for invocation expressions, preserving its `MethodKind.BuiltInMethod` guard.

## Test coverage

**HasDiagnostic (9 cases):** ConditionalExpressionNested, IfStatement, IfStatementNested, RecursionDirect, RecursionIndirect, RecursionDirectWithoutParentheses, RecursionIndirectWithoutParentheses, UserDefinedErrorNotGuardClause, UserDefinedFieldErrorNotGuardClause.
**NoDiagnostic (9 cases):** CurrReportGuardClause, CurrXMLportGuardClause, IfStatement, DiscountConsecutiveAndOperator, IfStatementElseIf, IfStatementGuardClause, IfStatementGuardClauseFieldRefFieldErrorWithoutParentheses, IfStatementGuardClauseContinue, IfStatementGuardClauseFieldError.

## CodeFix

No CodeFix is provided.