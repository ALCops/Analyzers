---
paths:
  - "src/ALCops.PlatformCop/**/NotAllCodePathsReturnValue*"
---

# PC0038: NotAllCodePathsReturnValue

## Purpose

Detects procedure declarations with an explicit return type where at least one reachable path does not return a value.
The rule excludes TryFunction methods.

## Design decisions

| Decision | Rationale |
|---|---|
| Scope: procedure declarations with return values | The rule intentionally excludes triggers even when they declare a return type |
| Require explicit return syntax (`method.ReturnValue`) | Targets only methods that declare a return contract |
| Exclude TryFunction | TryFunction has implicit platform semantics and is intentionally out of scope |
| Flow analysis based on IOperation tree | Works consistently for nested blocks and AL control-flow constructs |
| Named return variable counts as return value when definitely assigned on fallthrough paths | Matches AL named-return pattern without forcing exit() usage |
| `exit` without explicit expression is treated as missing value unless named return was already assigned on that path | Prevents silent default-value returns on early exits |
| Built-in `Error(...)` and `FieldError(...)` invocations terminate the path (return empty state set) | `FlowTerminatingBuiltIns` identifies only semantically bound built-ins, including incomplete calls on Dialog, Record, and FieldRef while editing; user-defined methods with the same names do not terminate a path |
| Passing a named return to a `var` parameter or using it as an invocation receiver counts as assignment (e.g. `Rec.Get(No)`) | These operations can write the value. PC0038 intentionally stops at the call boundary and does not inspect callees to prove that a write occurs; a potential write therefore counts as assignment to avoid unbounded interprocedural analysis and noise |
| Direct `if` conditions contribute `var` assignment side effects | Covers guard clauses such as `if not JsonObject.Get(Key, Result) then Error(...)`. For `and` and `or`, the left operand is always analyzed while right-operand states are unioned with the short-circuit path. Conditional expressions union both result branches when the target SDK exposes their operation interface; older SDKs remain conservative |
| Case selectors, loop conditions, `for` bounds, and `foreach` collection expressions contribute `var` initialization side effects | Extends the same guard-clause treatment to constructs whose expression is guaranteed to execute at least once regardless of body iteration |
| `xor` in conditions is not treated as a branching operator | AL's `xor` always evaluates both operands, so a `var` invocation under `xor` is guaranteed to execute. Only `and`, `or`, and conditional (ternary-like) expressions are guarded against short-circuit skipping |
| Case-else clauses are traversed through both `IBlockStatement` and `IStatementList` | The AL SDK wraps a case's else clause in `IStatementList` (`BoundStatementList`), so an additional case was added alongside the block handler |
| Exhaustive `case` statements do not require `else` | When every enum or option ordinal known to the current compilation is handled, the unmatched path is considered unreachable. This includes enum extensions available at development time; values added later by downstream apps are outside the analyzer's compilation scope. System options expose their option values through the public `IContainerSymbol.GetMembers()` contract |
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
  - `if` condition invocations update state before branch analysis unless the condition contains `and`, `or`, or a conditional expression.
  - `case` selector invocations update state before branch analysis under the same guard.
  - `while` conditions run before the body; `repeat until` conditions run after the body. `for` bounds (initial + end) and `foreach` collection expressions run before the body. All contribute state under the same guard.
  - Branches (`if`, `case`) union successor states.
  - Loops conservatively include non-executed path for optional loops.
- Reports diagnostic when at least one reachable path can end without value.

## Test notes

- The test suite contains a hard-coded `AnalyzeTriggers = false` toggle so the existing trigger fixtures remain reusable while triggers stay intentionally excluded from the analyzer.

## Known issues

- `LoopKind.Repeat` handling depends on SDK loop metadata availability across versions; behavior is conservative for optional-loop execution.
- `case` line body extraction uses reflective fallback to remain compatible across SDK versions.
