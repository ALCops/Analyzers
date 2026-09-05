---
paths:
  - "src/ALCops.*/Analyzers/**"
---

# How the host runs analyzer callbacks

What the NAV SDK driver and the VS Code host actually do with registered actions. Everything here was read in the decompiled SDK (`../nav-sdk-source`; files listed at the end). Re-check there when a newer SDK lands.

## Not every callback runs

The editor host never analyzes a whole project on each keystroke:

- **Per-file pass.** The host builds an `AnalysisScope` whose filter tree is the edited file. `ShouldAnalyze(ISymbol)` rejects every declaration outside that file, for **all** per-declaration action kinds alike (SyntaxNode, Operation, CodeBlock, Symbol). Compilation-level actions still run to completion.
- **Module-only pass.** When the background scope is `File` and the document count exceeds the partial-diagnostics threshold, the host enqueues only the module's `SymbolDeclaredCompilationEvent`. No per-declaration action of any kind fires; compilation-level actions still do.
- **Hash suppressor.** The host drops a diagnostics response when the file hash is unchanged, and its per-module diagnostics cache is invalidated on start, stop and document removal, but not on settings or ruleset changes.

There is no incremental diagnostic cache in shipping code paths: `CompilationWithAnalyzers` (the only owner of `AnalysisState`) is never instantiated, both driver entry points pass a null `AnalysisState`, so every skip gate evaluates to "execute". There is therefore no action-kind asymmetry either: "SyntaxNodeAction is safe, CodeBlockAction is skippable" is false. Callbacks are skipped by *scope*, uniformly across kinds.

### The two-phase accumulator is broken

```csharp
// BROKEN - never do this
context.RegisterCompilationStartAction(start =>
{
    var seen = new ConcurrentDictionary<...>();
    start.RegisterCodeBlockAction(ctx => seen.TryAdd(...));    // per-file pass: only the edited file; module pass: never
    start.RegisterCompilationEndAction(ctx => Report(seen));  // always fires, with a partial or empty accumulator
});
```

Under a per-file pass the end action sees only the edited file's data and reports everything else as missing or unused. Every cross-declaration rule in this repo is written so that each callback is self-contained: it gathers what it needs from the compilation (symbols, members, sibling extensions) and reports within the same callback. Microsoft's CodeCops follow the same discipline; their block start/end pairs only report within one method.

`CompilationStartAction` remains the right place for:

1. loading expensive resources once (XLIFF, `alcops.json`, indexes) and handing them to inner actions via closure;
2. registering inner actions that are individually self-contained;
3. read-only indexes that inner callbacks consult.

It is never the place for mutable state that a later action reports on.

## Callback order is not a contract

In the SDK source the driver runs SyntaxNode, then Operation, then CodeBlock actions per declaration, and pre-computes operation blocks before the CodeBlock action (which is why `GetOperation(body)` is cheap there; see `analyzer-performance.md`). Treat that as an implementation detail of one SDK version. Nothing in an analyzer may depend on one action having run before another.

## Analyzer instances are shared

Instances are materialized once per project and reused across passes and, in a multi-root workspace, across projects with different `alcops.json` files or rulesets. A mutable instance field is therefore a race. Per-compilation state (settings, thresholds, enablement) lives in `CompilationStart` closures or is threaded as a parameter.

## `RegisterOperationBlockStartAction` never fires

The SDK declares it, collects the actions, and the driver dispatches them, yet the callback does not run in the RoslynTestKit fixtures, and nothing registered inside it runs either. The cause has not been isolated in the SDK source. Do not use it. For per-body state shared across the invocations of one body use `RegisterCodeBlockAction`, walk `GetOperation(body)` once, and keep the state callback-local.

## `ConditionalWeakTable<Compilation, …>` caches

A `ConditionalWeakTable` keyed on `Compilation` is a correct per-compilation cache **inside one action kind** (`DuplicateODataEntityName` and `TransferFieldsSchemaCompatibility` use it from symbol actions; pattern in `analyzer-performance.md`). It silently fails across action kinds: the `Compilation` reached through `SemanticModel.Compilation` (CodeBlock, Operation, SyntaxNode contexts) is a different object from `ctx.Compilation` in Compilation and CompilationStart contexts, and the table compares by reference. To share a read-only index across action kinds, build it in `CompilationStart` and capture it.

## Where to read this in the SDK

| File | Look for |
|---|---|
| `AnalyzerExecutor.cs` | `TryStartAnalyzingDeclaration` skip gates, identical for SyntaxNode, Operation, OperationBlock and CodeBlock actions |
| `AnalyzerDriverBase.cs` | both entry points pass a null `AnalysisState` |
| `AnalyzerDriver.cs` | `TryExecuteDeclaringReferenceActions` (per-declaration dispatch), `GetOperationBlocksToAnalyze` |
| `AnalysisScope.cs` | `ShouldAnalyze(ISymbol)` per-file filtering |
| `AnalyzersHelper.cs` (EditorServices) | `GetPerModuleAnalyzerDiagnostics` module-only pass |
| `DiagnosticService.cs` (EditorServices.Protocol) | hash suppressor and module diagnostics cache |
| `Workspaces/ProjectInfo.cs` | analyzer instances materialized once per project |
