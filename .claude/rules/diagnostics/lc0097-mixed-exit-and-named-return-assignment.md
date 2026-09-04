---
paths:
  - "src/ALCops.LinterCop/**/MixedExitAndNamedReturnAssignment*"
  - "src/ALCops.LinterCop.Test/Rules/MixedExitAndNamedReturnAssignment/**"
---

# LC0097: MixedExitAndNamedReturnAssignment

## Purpose

Detects methods with a named return variable that mix both styles:
- assignment to the named return variable, and
- usage of `exit(...)` or `exit`.

Registers `CodeBlockAction`; main type `MixedExitAndNamedReturnAssignment` with an `OperationWalker`.

## Design decisions

| Decision | Rationale |
|---|---|
| Disabled by default; no CodeFix | Opt-in rule; tests enable it through a ruleset (see Test notes). |
| Scope: method and trigger declarations with a named return value | Every return-capable declaration is evaluated; page triggers such as `OnQueryClosePage` return unnamed values and therefore only yield NoDiagnostic cases. |
| Report at each `exit` statement; one `exit` suffices and the assignment may sit at any nesting depth | One mixed path already makes the declaration inconsistent, and the readability problem exists regardless of control-flow depth; the `exit` is the conflicting style. |
| Assignment-target detection prefers `ReturnValueReferenceExpression` and falls back to symbol identity only when the symbol `Kind` is `ReturnValue` (`IsNamedReturnTarget` in Common) | Without the kind check a field whose name matches the return variable (`Buf.Result := 5;` with return name `Result`) is misidentified as a return-variable assignment. |

## Deliberate non-reports

- Methods without a named return variable.
- `TryFunction` methods: platform-defined return semantics.
- Methods that only use `exit` without assigning the named return variable.
- Triggers whose return value is unnamed.

## Test notes

- `MixedExitAndNamedReturnAssignment.ruleset.json` is injected via `RuleSetPath` because the rule is disabled by default.
