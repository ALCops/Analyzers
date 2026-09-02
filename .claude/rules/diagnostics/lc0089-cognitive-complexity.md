---
paths:
  - "src/ALCops.LinterCop/**/*CognitiveComplexity*"
---

# LC0089 / LC0089i / LC0090: CognitiveComplexity

## Purpose

LC0089 reports a hidden metric diagnostic with the cognitive complexity score of each method/trigger. LC0089i (opt-in) reports per-increment diagnostics showing where each complexity point comes from and the nesting penalty. LC0090 reports a warning when cognitive complexity exceeds the configurable threshold. Together they give AL developers actionable feedback on method readability using the Cognitive Complexity model (Sonar).

## Design decisions

| Decision | Rationale |
|---|---|
| `CompilationStart` closure for threshold and LC0089i enablement (not instance fields) | Analyzer instances are shared across passes/projects (`ProjectInfo.cs:113`). Mutable instance fields are overwritten by an overlapping pass with a different `alcops.json` or ruleset, producing wrong thresholds or silently enabling/disabling LC0089i mid-analysis. Fixed in #254. |
| Threshold loaded from `ALCopsSettingsProvider`, not per-method | `ALCopsSettingsProvider` caches statically by directory path with no invalidation API — an `alcops.json` edit takes effect only after restart regardless of where settings are read. Moving the read into a per-method callback would add cost without changing staleness behavior. |
| `RegisterCodeBlockAction` (not `SyntaxNodeAction` or `OperationAction`) | Cognitive complexity requires walking the full method body once with nesting tracking. `CodeBlockAction` provides the body directly and benefits from pre-computed operation trees for recursion detection. |
| Recursion detection via `CognitiveComplexityRecursionGraphService` | Builds a call graph at `CompilationStart` from all method bodies, then queries it per-method for cycles. Lives in a separate service class because the graph is compilation-scoped while the complexity walk is per-method. |
| LC0089 as `Info` / `isEnabledByDefault: false` | A metric, not a violation — only useful to developers who opt in via `.editorconfig`. |
| LC0089i as `Info` / `isEnabledByDefault: false` | Per-increment detail is noise unless actively investigating a specific method's score. Gated behind `IsDiagnosticEnabled` so the walk-and-report cost is zero when disabled. |
| Guard-clause discount | Standard Cognitive Complexity model: early-exit `if <condition> then exit/error/break/continue/skip/quit` is a flow simplification, not additional cognitive load, so it does not increment. Built-in `Dialog.Error`, `Table.FieldError`, and `FieldRef.FieldError` are resolved through `FlowTerminatingBuiltIns`; user-defined same-name methods remain regular calls. Existing `CurrReport` / `CurrXMLport` handling for `Break`, `Skip`, and `Quit` remains syntax-specific because those calls are guard exits but not general procedure terminators. |
| #254 verification: no regression fixture for the instance-field race | The race requires overlapping compilation passes against different `alcops.json` files, which is not reproducible in unit tests. Existing `CognitiveComplexity` tests verify the functional behavior is unchanged. |

## Architecture

- Registers `CompilationStartAction` → captures `complexityThreshold`, `isIncrementDiagnosticsEnabled`, and `CognitiveComplexityRecursionGraphService` as locals → registers `CodeBlockAction`.
- Iterative stack-based walk (no recursion) of the method body counts flow-breaking structures, nesting penalties, logical-operator sequences, and guard-clause discounts.
- Guard invocation classification resolves built-in errors from the invocation operation through `FlowTerminatingBuiltIns`; report/xmlport and loop-control guards retain their dedicated syntax checks.
- Recursion detection: `CognitiveComplexityRecursionGraphService` builds an adjacency list of method-ID edges at compilation start; per-method, checks for cycles back to the current method via DFS.

## Known issues

- Collectible `Error(ErrorInfo)` calls are discounted as guards even in an `ErrorBehavior::Collect` scope where execution can continue. This is inherited from the shared invocation classifier and requires broader flow analysis to distinguish.
- `ALCopsSettingsProvider` caches the threshold statically by directory with no invalidation. Changing `alcops.json` requires restarting the language server. This is a pre-existing cross-cop limitation, not specific to this analyzer.
