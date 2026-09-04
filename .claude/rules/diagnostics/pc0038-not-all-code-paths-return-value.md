---
paths:
  - "src/ALCops.PlatformCop/**/NotAllCodePathsReturnValue*"
  - "src/ALCops.PlatformCop.Test/Rules/NotAllCodePathsReturnValue/**"
  - "src/ALCops.Common/FlowTerminatingBuiltIns.cs"
---

# PC0038: NotAllCodePathsReturnValue

## Purpose

Detects procedure declarations with an explicit return type where at least one reachable path does not return a value.
The rule excludes TryFunction methods.

Registers `RegisterSyntaxNodeAction` on `SyntaxKind.MethodDeclaration`; main type `NotAllCodePathsReturnValue`, with flow-terminating calls classified by `ALCops.Common.FlowTerminatingBuiltIns`.

**References:** [#463](https://github.com/ALCops/Analyzers/issues/463) (`FieldError` false positive), [#468](https://github.com/ALCops/Analyzers/issues/468) (built-in name matching), [#471](https://github.com/ALCops/Analyzers/issues/471) (flow edge cases).

## Design decisions

| Decision | Rationale |
|---|---|
| Procedure declarations with explicit return syntax only; triggers are excluded even when they declare a return type | Targets methods that declare a return contract |
| TryFunction methods excluded | TryFunction has implicit platform semantics |
| Path-state analysis over the `IOperation` tree | Works consistently for nested blocks and AL control-flow constructs |
| A named return variable counts as returned when definitely assigned on every fallthrough path | Matches the AL named-return pattern without forcing `exit()` |
| Bare `exit` is a missing value unless the named return was already assigned on that path; `exit(<value>)`, bare `exit` and assigned fallthrough stay distinct | Prevents silent default-value returns on early exits |
| Only `Dialog.Error`, `Table.FieldError` and `FieldRef.FieldError` terminate a path, matched on the exact built-in class and method through the shared `FlowTerminatingBuiltIns` classifier; the former `ThrowError` special case is gone | Guard clauses like `if Cond then exit(x) else Error('...')` are pervasive; matching `MethodKind.BuiltInMethod` plus a name would also accept a future unrelated built-in or a crossed pair, and `ThrowError` is not an AL built-in |
| Incomplete (invalid) calls terminate only when the binder's synthesized receiver is the matching `Dialog`, `Record` or `FieldRef` type | Stops the diagnostic from flickering while an argument is being typed, without treating user-defined `Error`/`FieldError` procedures as terminators |
| Passing the named return to a `var` parameter or using it as an invocation receiver (`Rec.Get(No)`) counts as assignment | These calls can write the value; the rule stops at the call boundary rather than proving the write interprocedurally, trading a possible false negative for bounded analysis and no noise |
| `if` conditions, `case` selectors, `while` and `repeat until` conditions, `for` bounds and `foreach` collection expressions contribute the `var` side effects of the invocations they contain | Those expressions execute at least once regardless of the body, so `if not JsonObject.Get(Key, Result) then Error(...)` initializes `Result` |
| Under `and`/`or` only the left operand is guaranteed; right-operand states are unioned with the short-circuit path, and conditional expressions union both branches. `xor` is not treated as branching | AL short-circuits `and`/`or` and evaluates only one branch of a conditional expression, while `xor` always evaluates both operands |
| A `case` over an enum or option without `else` is exhaustive when every value visible in the current compilation is covered | Matches how the compiler sees the enum, including enum-extension values; a missing `else` on an exhaustive `case` is not a missing return |
| Named-return target matching falls back to symbol kind `ReturnValue`, never to name alone | Name comparison misclassified member accesses sharing the return variable's name (`Buf.Result := 5;` with a field named `Result`) |
| Loops conservatively include the non-executed path for optional loops; `break` is a loop exit, not body fallthrough | A loop body that may not run cannot guarantee a return, and a `repeat until` left through `break` never evaluates its condition |
| Reported at the method name | User requirement |

## Deliberate non-reports

- Triggers, even with a return type.
- TryFunction methods.
- Paths ending in a clean `Dialog.Error`, `Table.FieldError` or `FieldRef.FieldError` call, or in an incomplete call whose receiver binds to one of those types.
- Named returns passed by `var` or used as a receiver: assumed assigned, even when the callee never writes.
- `case` statements without `else` that cover every enum or option value visible in the compilation.

## Known issues

- `Error(ErrorInfo)` is treated as terminating even when `ErrorInfo.Collectible = true` inside an `ErrorBehavior::Collect` scope, where execution continues. Telling the two apart needs data flow on the `ErrorInfo` value and the enclosing call context, which the invocation-level classifier does not have.
- `LoopKind.Repeat` handling depends on SDK loop metadata availability across versions; behaviour is conservative for optional-loop execution.
- `case` line body extraction uses a reflective fallback to remain compatible across SDK versions.

## SDK facts

- The AL SDK wraps a case's else clause in `IStatementList` (`BoundStatementList`), not `IBlockStatement`, so both shapes must be traversed.
- `CompilationUtilities.GetEnumValues` (internal) is the only complete source of enum values: the public enum value lists omit values added by enum extensions. System options expose their values through the public `IContainerSymbol.GetMembers()`.
- The operation interfaces for conditional expressions differ per target framework, so the analyzer switches on `OperationKind` and reads the operands reflectively instead of naming the interface.
- Parenthesized expressions wrap their operand in a separate operation and must be unwrapped before the short-circuit rules apply.
- Bad-call and built-in identity facts (single-candidate bad calls keep the real symbol, built-in classes `Table`/`FieldRef` have `NavTypeKind.None`, bare `Error` is a static built-in on `Dialog`) are in `.claude/rules/symbol-resolution.md`.

## Test notes

- Fixtures are gated with `SkipTestIfVersionIsTooLow`: an enum extension declared in the same module needs runtime `13.0`; the ternary conditional expression and `this` need `14.0`.
- Incomplete `Error`/`FieldError` calls are tested with a second fixture created with `ThrowsWhenInputDocumentContainsError = false`; those test methods are named `*InDocumentWithErrors`.
