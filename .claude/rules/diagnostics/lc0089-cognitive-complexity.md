---
paths:
  - "src/ALCops.LinterCop/**/*CognitiveComplexity*"
  - "src/ALCops.LinterCop.Test/Rules/CognitiveComplexity/**"
---

# LC0089 / LC0089i / LC0090: CognitiveComplexity

## Purpose

LC0089 reports a hidden metric diagnostic with the cognitive complexity score of each method/trigger. LC0089i (opt-in) reports per-increment diagnostics showing where each complexity point comes from and the nesting penalty. LC0090 reports a warning when cognitive complexity exceeds the configurable threshold. Together they give AL developers actionable feedback on method readability using the Cognitive Complexity model (Sonar).

Registers `CompilationStartAction` (captures threshold, LC0089i enablement and the recursion graph) then `CodeBlockAction`; main type `CognitiveComplexity` with `CognitiveComplexityRecursionGraphService`.

## Design decisions

| Decision | Rationale |
|---|---|
| Threshold and LC0089i enablement live in the `CompilationStart` closure, not instance fields | Analyzer instances are shared across passes and projects (see `.claude/rules/sdk-analysis-scope.md`); instance fields would be overwritten by an overlapping pass with a different `alcops.json` or ruleset. |
| Threshold read once from the compilation settings snapshot with the callback's cancellation token | Every method uses the same threshold. Failed HTTP requests can retry on a later compilation; successful settings and deterministic configuration errors remain cached across compilations. |
| `CodeBlockAction` rather than `SyntaxNodeAction`/`OperationAction` | The score needs one walk of the full body with nesting tracking; the body is available directly and the pre-computed operation tree serves recursion detection. |
| Recursion detection in a separate compilation-scoped `CognitiveComplexityRecursionGraphService` | The call graph is built once per compilation while the complexity walk is per method; mixing the two scopes in one class would be wrong. |
| LC0089 and LC0089i are `Info` and `isEnabledByDefault: false`; LC0089i is gated behind `IsDiagnosticEnabled` | A metric is not a violation and per-increment detail is noise unless a specific method is being investigated; the gate makes the walk-and-report cost zero when disabled. |
| Guard-clause discount | Standard Cognitive Complexity: an early exit `if <condition> then exit/error/break/continue/skip/quit` simplifies flow and does not increment. |
| Built-in error guards are recognized through the shared `FlowTerminatingBuiltIns` classifier (`Dialog.Error`, `Table.FieldError`, `FieldRef.FieldError`); user-defined procedures named `Error` or `FieldError` stay ordinary calls | One semantic definition of "terminates the procedure" shared with PC0038 and FC0007; a name match alone would discount a user procedure that merely happens to be called `Error`. |
| `CurrReport`/`CurrXMLport` `Break`, `Skip` and `Quit` keep their own syntax check instead of joining `FlowTerminatingBuiltIns` | They are valid guard exits for complexity but not general procedure terminators, so they must not leak into PC0038's flow analysis. |

## Deliberate non-reports

- LC0089 and LC0089i are never emitted unless explicitly enabled via `.editorconfig` or a ruleset.
- Guard clauses (`if cond then exit/error/fielderror/break/continue/skip/quit`) add no complexity, so methods made of early exits stay below the threshold.

## Known issues

- Successful settings and deterministic configuration errors remain cached by workspace path; editing those requires restarting the language server. Failed HTTP requests retry on a later compilation. This is shared provider behavior.
- A collectible `Error(ErrorInfo)` inside an `ErrorBehavior::Collect` scope is discounted as a guard although execution continues; inherited from the shared classifier (see PC0038's Known issues).

## Test notes

- Fixtures are version-gated with `SkipTestIfVersionIsTooLow`: nested conditional expressions and `this` need `14.0`, `continue` needs `15.0`.
- The shared-instance race has no regression fixture: it requires overlapping compilation passes against different `alcops.json` files, which unit tests cannot reproduce.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `CognitiveComplexityThreshold` | `15` | Score above which LC0090 is reported. |
