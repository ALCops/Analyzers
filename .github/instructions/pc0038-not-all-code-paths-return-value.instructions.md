---
applyTo: 'src/ALCops.PlatformCop/**/NotAllCodePathsReturnValue*'
---

# PC0038: NotAllCodePathsReturnValue

## Purpose

Detects procedure declarations with an explicit return type where at least one reachable path does not return a value.
The rule excludes TryFunction methods.

## Diagnostic properties

| Property | Value |
|----------|-------|
| ID | PC0038 |
| Category | Usage |
| Severity | Warning |
| Enabled | Yes |
| CodeFix | No |
| Message | `'{0}': not all code paths return a value` |

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Scope: procedure declarations with return values | The rule intentionally excludes triggers even when they declare a return type |
| Require explicit return syntax (`method.ReturnValue`) | Targets only methods that declare a return contract |
| Exclude TryFunction | TryFunction has implicit platform semantics and is intentionally out of scope |
| Flow analysis based on IOperation tree | Works consistently for nested blocks and AL control-flow constructs |
| Named return variable counts as return value when definitely assigned on fallthrough paths | Matches AL named-return pattern without forcing exit() usage |
| `exit` without explicit expression is treated as missing value unless named return was already assigned on that path | Prevents silent default-value returns on early exits |
| Built-in `Error(...)` and `FieldError(...)` invocations terminate the path (return empty state set) | Both methods unconditionally end AL execution. Matching requires `MethodKind.BuiltInMethod`, so user-defined procedures with these names remain non-terminating. The common `FlowTerminatingBuiltIns` classifier keeps the set shared with FC0007 and LC0089/LC0090. |
| Named return is treated as assigned when passed to a `var` (by-reference) parameter or used as the receiver of an invocation (e.g. `Rec.Get(No)`) | Covers common AL idioms: out-parameter initialization and `Record.Get`/`FindFirst`/etc. into the return record. Intentionally conservative to avoid noise |
| Case-else clauses are traversed through both `IBlockStatement` and `IStatementList` | The AL SDK wraps a case's else clause in `IStatementList` (`BoundStatementList`), so an additional case was added alongside the block handler |
| `IsNamedReturnTarget` fallback requires symbol kind `ReturnValue` | Comparing by name only would misclassify member accesses that share the return variable's name (e.g. `Buf.Result := 5;` when the record has a field named `Result`). Fix lives in `ALCops.Common/Extensions/OperationExtensions.cs` |
| Report location is method name | User requirement |

## Architecture

- Registers `SyntaxNodeAction` on `SyntaxKind.MethodDeclaration`.
- Resolves `IMethodSymbol` and validates:
  - explicit return syntax is present,
  - procedure is not a TryFunction method (via `MethodDeclarationSyntaxExtensions.IsTryFunction` in `ALCops.Common`),
  - body exists.
- Formats the diagnostic subject through `MethodSymbolInterfaceExtensions.GetDiagnosticDisplayText(...)` in `ALCops.Common` using the object name, procedure name, and parameter type list.
- Obtains operation tree via `SemanticModel.GetOperation(method.Body)`.
- Runs path-state analysis with state set `{assignedNamedReturn: true|false}`.
  - Assignment to named return target marks state as `true` (via `OperationSafeExtensions.IsNamedReturnTarget` in `ALCops.Common`).
  - `exit(<expr>)` terminates path with value.
  - `exit` without expression terminates path and marks missing-value path if required.
  - Branches (`if`, `case`) union successor states.
  - Loops conservatively include non-executed path for optional loops.
- Reports diagnostic when at least one reachable path can end without value.

## Test coverage

**HasDiagnostic (10 cases):** UnnamedNoExit, UnnamedIfWithoutElse, NamedAssignedOnlyInIf, NamedLoopMayNotAssign, UnnamedCaseWithoutElse, UnnamedIfElseIfElseMissingReturn, NamedNestedIfElseIfMissingAssignment, NamedPassedAsByValueArgument, NamedNotAssignedFieldSameName, UnnamedUserDefinedFieldErrorNotTerminating.
**NoDiagnostic (22 cases):** UnnamedImmediateExit, UnnamedIfElseBothExit, NamedDirectAssignment, NamedAssignmentInBothBranches, NamedAssignedBeforeConditional, TryFunctionExcluded, NamedCaseAllBranchesAssigned, UnnamedIfElseIfElseAllReturn, NamedNestedIfElseIfAssigned, TriggerCases, UnnamedIfElseErrorTerminates, NamedIfElseErrorTerminates, UnnamedCaseElseErrorTerminates, UnnamedCaseElseExitTerminates, UnnamedGuardClauseErrorFirst, NamedInitializedByVarArgument, NamedInitializedByReceiverCall, UnnamedIfElseFieldErrorTerminates, NamedIfElseFieldErrorTerminates, UnnamedCaseElseFieldErrorTerminates, UnnamedGuardClauseFieldErrorFirst, UnnamedIfElseFieldRefFieldErrorTerminates.

## Test notes

- The test suite contains a hard-coded `AnalyzeTriggers = false` toggle so the existing trigger fixtures remain reusable while triggers stay intentionally excluded from the analyzer.

## Known issues

- `LoopKind.Repeat` handling depends on SDK loop metadata availability across versions; behavior is conservative for optional-loop execution.
- `case` line body extraction uses reflective fallback to remain compatible across SDK versions.
