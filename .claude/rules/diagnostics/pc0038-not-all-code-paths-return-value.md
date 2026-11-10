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

**References:** [#463](https://github.com/ALCops/Analyzers/issues/463) (`FieldError` false positive), [#468](https://github.com/ALCops/Analyzers/issues/468) (built-in name matching), [#471](https://github.com/ALCops/Analyzers/issues/471) (flow edge cases), [#596](https://github.com/ALCops/Analyzers/issues/596) (named record return).

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
| Passing the named return or one of its fields to a `var` parameter, or using either as an invocation receiver (`Rec.Get(No)`, `Evaluate(Result."Entry No.", Txt)`, `Result.Content.CreateOutStream(Output)`), counts as assignment | These calls can write the returned instance; the rule stops at the call boundary rather than proving the write interprocedurally, trading a possible false negative for bounded analysis and no noise. Assignments, receivers and `var` arguments share the same identity check, so fields on unrelated records do not initialize the return |
| Assigning a field of the named return (`Result."No." := No;`) counts as assignment | Fields belong to the returned instance. For record returns the named form is the one Microsoft recommends and the only one that hands filters and keys to a caller using the result directly (see Runtime facts). Property assignments such as `Result.ReadIsolation := ...` were already accepted before field assignments were, through the receiver rule |
| Conversions around a `var` argument are peeled before matching the named return or its field; invocation receivers are matched directly | The binder inserts a conversion when the parameter type differs: a temporary record passed to a non-temporary `var Record`, and `Clear(Result)`. Receiver-conversion handling is omitted because no supported receiver scenario demonstrates a need for it |
| `Clear(Result)` and `Result.Init()` count as assignment, even as the only statement | Both are explicit initialization. At runtime both apply the `InitValue` defaults, which a fresh record variable already holds, so neither changes an untouched return value; the rule still accepts them because the developer visibly chose the default value. They differ only when something was set before: `Init` keeps primary key, filters and key, `Clear` resets them |
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
- Named returns or their fields passed by `var` or used as a receiver: assumed assigned, even when the callee never writes.
- Named record returns initialized only through field or property assignments, or only through view-state calls (`SetRange`, `SetFilter`, `SetCurrentKey`, `SetLoadFields`).
- Named record returns whose only initialization is `Init()` or `Clear()`, although neither changes an untouched record.
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
- `Clear(Result)` binds its argument through a conversion to the built-in's `var` parameter, as does a temporary record passed to a non-temporary `var Record`; unwrapping the argument is what makes both count as assignment.

## Runtime facts (verified on BC 29)

- A record return keeps filters and the current key only when the caller uses the call result directly (`GetLines(No).GetFilters()`) and the procedure sets them on the named return. `exit(LocalRec)` and a caller-side `Rec := GetLines(No)` are record assignments and keep only field values.
- `Rec.CopyFilters(GetLines(No))` and `Rec.Copy(GetLines(No))` fail with AL0130 because the argument is `var`.
- The same assignment applies to `temporary` record returns: the caller receives the current row, not the temporary table's rows. Passing filters or temporary data to the caller needs a `var` parameter, or `GetView`/`SetView` for filters.
- None of this changes PC0038's verdict, but it is why the rule must accept the named-return form without `exit` and must never push record returns towards `exit(LocalRec)`.

## Test notes

- Fixtures are gated with `SkipTestIfVersionIsTooLow`: an enum extension declared in the same module needs runtime `13.0`; the ternary conditional expression and `this` need `14.0`.
- Incomplete `Error`/`FieldError` calls are tested with a second fixture created with `ThrowsWhenInputDocumentContainsError = false`; those test methods are named `*InDocumentWithErrors`.
