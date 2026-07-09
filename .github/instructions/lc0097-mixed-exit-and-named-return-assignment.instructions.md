---
applyTo: 'src/ALCops.LinterCop/**/MixedExitAndNamedReturnAssignment*'
---

# LC0097: MixedExitAndNamedReturnAssignment

## Purpose

Detects methods with a named return variable that mix both styles:
- assignment to the named return variable, and
- usage of `exit(...)` or `exit`.

## Diagnostic properties

| Property | Value |
|----------|-------|
| ID | LC0097 |
| Category | Usage |
| Severity | Warning |
| Enabled | No (disabled by default) |
| CodeFix | No |
| Message | `{0} '{1}' mixes exit() with assignments to the named return variable. Use one return style consistently.` |

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Scope: method and trigger declarations with return values | Ensures all return-capable declarations are evaluated for mixed return style |
| Check only named return methods | The rule is about mixing named return assignments with `exit(...)` |
| Triggers are analyzed but currently contribute only NoDiagnostic scenarios | Page triggers like `OnQueryClosePage` return unnamed values and cannot satisfy the mixed-style condition |
| Exclude TryFunction | TryFunction has platform-defined return semantics |
| Single `exit` is sufficient when named-return assignment exists | One mixed path is enough to make the declaration inconsistent |
| Assignment position is irrelevant (any nesting depth) | The readability problem exists regardless of control-flow depth |
| Report location is each exit statement | Points directly at the conflicting return style |

## Architecture

- Registers `CodeBlockAction`.
- Filters to `MethodOrTriggerDeclarationSyntax` with body and named return symbol (`ReturnValueSymbol.IsNamed`).
- Excludes TryFunction methods via `MethodDeclarationSyntaxExtensions.IsTryFunction` in `ALCops.Common`.
- Walks operation tree with `OperationWalker`:
  - collects all `exit` statement locations,
  - detects assignments targeting the named return variable via `OperationSafeExtensions.IsNamedReturnTarget` in `ALCops.Common`.
- Emits LC0097 for each collected `exit` location if both conditions are true.

## Test coverage

**HasDiagnostic (6 cases):** NamedAssignmentAndExitValue, NamedAssignmentAndExitWithoutValue, NamedAssignmentInIfAndExitInElse, NamedCaseAssignmentAndExit, NamedIfElseIfElseAssignmentAndExit, NamedNestedIfElseIfAssignmentAndExit.
**NoDiagnostic (10 cases):** NamedOnlyAssignment, NamedOnlyExit, UnnamedExitAndLocalAssignment, TryFunctionExcluded, NamedCaseOnlyAssignments, NamedIfElseIfElseOnlyExit, NamedNestedIfElseIfOnlyAssignments, TriggerBooleanOnlyExit, TriggerBooleanCaseOnlyExit, TriggerBooleanNestedIfElseIfOnlyExit.

## Known issues

- Assignment target detection prefers `ReturnValueReferenceExpression` but keeps symbol-name fallback for SDK representation differences.
- Methods that only use `exit` (without return-variable assignments) are intentionally not flagged.
