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
| Threshold read once from `ALCopsSettingsProvider` at compilation start, not per method | The provider caches statically by directory with no invalidation, so an `alcops.json` edit only applies after restart wherever it is read; a per-method read adds cost without changing staleness. |
| `CodeBlockAction` rather than `SyntaxNodeAction`/`OperationAction` | The score needs one walk of the full body with nesting tracking; the body is available directly and the pre-computed operation tree serves recursion detection. |
| Recursion detection in a separate compilation-scoped `CognitiveComplexityRecursionGraphService` | The call graph is built once per compilation while the complexity walk is per method; mixing the two scopes in one class would be wrong. |
| LC0089 and LC0089i are `Info` and `isEnabledByDefault: false`; LC0089i is gated behind `IsDiagnosticEnabled` | A metric is not a violation and per-increment detail is noise unless a specific method is being investigated; the gate makes the walk-and-report cost zero when disabled. |
| Guard-clause discount | Standard Cognitive Complexity: an early exit `if <condition> then exit/error/break/continue/skip/quit` simplifies flow and does not increment. |

## Deliberate non-reports

- LC0089 and LC0089i are never emitted unless explicitly enabled via `.editorconfig` or a ruleset.
- Guard clauses (`if cond then exit/error/break/continue/skip/quit`) add no complexity, so methods made of early exits stay below the threshold.

## Known issues

- `ALCopsSettingsProvider` caches the threshold statically by directory with no invalidation; changing `alcops.json` requires restarting the language server. Cross-cop limitation, not specific to this analyzer.

## Test notes

- Fixtures are version-gated with `SkipTestIfVersionIsTooLow`: nested conditional expressions and `this` need `14.0`, `continue` needs `15.0`.
- The shared-instance race has no regression fixture: it requires overlapping compilation passes against different `alcops.json` files, which unit tests cannot reproduce.

## Settings

| Setting | Default | Effect |
|---|---|---|
| `CognitiveComplexityThreshold` | `15` | Score above which LC0090 is reported. |
