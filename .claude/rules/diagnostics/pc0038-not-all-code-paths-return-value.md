---
paths:
  - "src/ALCops.PlatformCop/**/NotAllCodePathsReturnValue*"
  - "src/ALCops.PlatformCop.Test/Rules/NotAllCodePathsReturnValue/**"
---

# PC0038: NotAllCodePathsReturnValue

## Purpose

Detects procedure declarations with an explicit return type where at least one reachable path does not return a value.
The rule excludes TryFunction methods.

Registers `RegisterSyntaxNodeAction` on `SyntaxKind.MethodDeclaration`; main type `NotAllCodePathsReturnValue`.

## Design decisions

| Decision | Rationale |
|---|---|
| Procedure declarations with explicit return syntax only; triggers are excluded even when they declare a return type | Targets methods that declare a return contract |
| TryFunction methods excluded | TryFunction has implicit platform semantics |
| Path-state analysis over the `IOperation` tree | Works consistently for nested blocks and AL control-flow constructs |
| A named return variable counts as returned when definitely assigned on every fallthrough path | Matches the AL named-return pattern without forcing `exit()` |
| Bare `exit` is a missing value unless the named return was already assigned on that path | Prevents silent default-value returns on early exits |
| Built-in `Error(...)` and `ThrowError` terminate the path | Guard clauses like `if Cond then exit(x) else Error('...')` are pervasive in AL; without this the rule would fire on every such branch |
| Passing the named return to a `var` parameter or using it as an invocation receiver (`Rec.Get(No)`) counts as assignment | Covers out-parameter initialization and `Get`/`FindFirst` into the return record; intentionally conservative to avoid noise |
| Named-return target matching falls back to symbol kind `ReturnValue`, never to name alone | Name comparison misclassified member accesses sharing the return variable's name (`Buf.Result := 5;` with a field named `Result`) |
| Loops conservatively include the non-executed path for optional loops | A loop body that may not run cannot guarantee a return |
| Reported at the method name | User requirement |

## Deliberate non-reports

- Triggers, even with a return type.
- TryFunction methods.
- Paths ending in `Error(...)` or `ThrowError`.
- Named returns passed by `var` or used as a receiver: assumed assigned.

## Known issues

- `LoopKind.Repeat` handling depends on SDK loop metadata availability across versions; behaviour is conservative for optional-loop execution.
- `case` line body extraction uses a reflective fallback to remain compatible across SDK versions.

## SDK facts

- The AL SDK wraps a case's else clause in `IStatementList` (`BoundStatementList`), not `IBlockStatement`, so both shapes must be traversed.
