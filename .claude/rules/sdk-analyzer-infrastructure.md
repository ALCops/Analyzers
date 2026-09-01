---
paths:
  - "src/ALCops.*/Analyzers/**"
---

# NAV SDK Analyzer Infrastructure Internals

How the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK executes analyzer callbacks. Understanding these internals is essential for writing correct and performant analyzers.

## Callback execution order (guaranteed)

Per declaration/object the SDK fires callbacks in this order:

1. **SyntaxNodeAction** (first)
2. **OperationAction** (second)
3. **CodeBlockAction** (last)

Source: `AnalyzerDriver.cs` lines 284-365 (`TryExecuteDeclaringReferenceActions`).

Consequence: `GetOperation(body)` called from a `SyntaxNodeAction` runs BEFORE the SDK pre-computes operation trees, while `GetOperation(body)` in a `CodeBlockAction` benefits from pre-computation (effectively a cache hit).

## Partial-analysis pass and callback scope filtering

The SDK's `AnalysisState.declarationAnalysisDataMap` (declaration-level diagnostic caching and replay) exists in the source but is **dead code in all shipping paths**. `CompilationWithAnalyzers` — the only owner of `AnalysisState` — is never instantiated anywhere in the SDK tree. Both driver entry points (`AnalyzerDriverBase.cs:371-379` whole-project, `:381-394` per-file) pass a **null** `AnalysisState` and a fresh `CompilationData`. Every skip gate evaluates `analysisStateOpt?.TryStartAnalyzingDeclaration(...) ?? true` (`AnalyzerExecutor.cs:996-1001`) → always execute. `declarationAnalysisDataMap` lives on `CompilationData` (not `AnalysisState`), holds only syntax-shape data (no diagnostics), and its `cacheAnalysisData` flag evaluates false on every real path (`AnalyzerDriver.cs:274-275`). There is **no per-declaration action-kind asymmetry**: SyntaxNode, Operation, OperationBlock, and CodeBlock actions all gate on the identical `TryStartAnalyzingDeclaration` call (`AnalyzerExecutor.cs:983 / 1086 / 1110 / 1128`); if skipping were ever activated they'd be skipped in lockstep.

The real mechanism that filters callbacks is host-level partial-analysis scope:

- **Per-file keystroke pass:** the host builds an `AnalysisScope` with `FilterTreeOpt` = the edited file. `ShouldAnalyze(ISymbol)` (`AnalysisScope.cs:80-100`) rejects every declaration outside that file — for **all** per-declaration action kinds uniformly — while compilation-level events still complete.
- **Module-only pass:** when `BackgroundCodeAnalysisScope == File` and doc count exceeds `PartialDiagnosticsDocumentThreshold`, `GetPerModuleAnalyzerDiagnostics` enqueues only a module `SymbolDeclaredCompilationEvent` (`AnalyzersHelper.cs:70-78`) — no per-declaration action of any kind fires; compilation-level actions still complete.
- **Hash-suppressor staleness:** `vsCodeDiagnosticState` in `EditorServices.Protocol/DiagnosticService.cs:715-736` drops the response when the file hash is unchanged; `moduleAnalyzerDiagnosticsCache` is invalidated on Start/Stop/DocumentRemoved but not on settings/ruleset changes.

Source: verified against the decompiled SDK at the current tag (18.0.x, net10.0 tree).

### Implications for analyzer patterns

| Pattern | Correct under partial analysis? | Notes |
|---|---|---|
| Self-contained per-declaration action (any kind) | ✅ Yes | SyntaxNode, Operation, CodeBlock actions are equally safe when each callback reports only about its own declaration |
| `RegisterSymbolAction` | ✅ Yes | Symbol-level, self-contained |
| `CompilationStart` + per-declaration accumulator + `CompilationEnd` | ❌ Broken | Under per-file or module-only passes, per-declaration callbacks run only for the edited file (or none); `CompilationEnd` fires with an incomplete/empty accumulator |

### The two-phase accumulator anti-pattern

**NEVER** use this pattern for analyzers that need cross-declaration completeness:

```csharp
// BROKEN PATTERN - DO NOT USE
context.RegisterCompilationStartAction(startCtx =>
{
    var accumulator = new ConcurrentDictionary<...>();

    startCtx.RegisterCodeBlockAction(blockCtx =>
    {
        // Under per-file pass: only fires for declarations in the edited file
        // Under module-only pass: never fires at all
        accumulator.TryAdd(...);
    });

    startCtx.RegisterCompilationEndAction(endCtx =>
    {
        // Always fires, but accumulator is incomplete or empty
        foreach (var entry in accumulator) { ... }
    });
});
```

This is what caused #243/#253: AC0032's old accumulator contained only the edited file's usages when `CompilationEnd` fired under the per-file pass, so permission entries from other files were flagged as "unused". A `SyntaxNodeAction`-based accumulator would have failed identically — the fix worked because each object became self-contained, not because of an action-kind difference.

Microsoft's own analyzers never use this pattern. Their `Rule175` uses `CodeBlockStartAction` + scoped `RegisterSyntaxNodeAction` + `CodeBlockEndAction` for per-method analysis, but only reports within that method (no cross-method accumulation).

### Analyzer instances are shared — no mutable instance fields

Analyzer instances are materialized once per project (`ProjectInfo.cs:113`) and shared across passes. Per-compilation state (settings, thresholds, enablement flags) must live in `CompilationStart` closures or be threaded as parameters, never stored in instance fields. An overlapping pass or different project with a different `alcops.json` or ruleset would overwrite instance fields mid-analysis. See also `analyzer-development.md` ("Pass loaded data via lambda captures or a state object, not instance fields").

## GetOperation performance characteristics

`SemanticModel.GetOperation(node)` cost depends heavily on the callback context:

| Context | Cost per call | Why |
|---|---|---|
| `CodeBlockAction` | ~0μs (cache hit) | SDK pre-computes via `GetOperationBlocksToAnalyze()` before firing callback |
| `SyntaxNodeAction` | ~300μs | No pre-computation; full binding required |
| `OperationAction` | N/A (operation already provided) | SDK passes the operation directly |

Source: `AnalyzerDriver.cs` lines 504-518 (`GetOperationBlocksToAnalyze` pre-computation).

### Performance guidance

When using `RegisterSyntaxNodeAction` and needing invocation/operation info:
- **Avoid `GetOperation(body)`** per method body (300μs × thousands of methods = seconds)
- **Prefer `GetSymbolInfo(node)`** for targeted resolution (~100μs/call, but only for nodes you care about)
- **Best: variable-map + syntax resolution** for bulk invocation analysis (build type maps from `IMethodSymbol.LocalVariables`/`.Parameters`, resolve via dictionary lookup, fall back to `GetSymbolInfo` only for complex receivers)

## SemanticModel API availability

| Method | Available | Returns | Notes |
|---|---|---|---|
| `GetDeclaredSymbol(node)` | ✅ Public | `ISymbol?` | For declarations (objects, methods, fields, variables) |
| `GetSymbolInfo(node)` | ✅ Public | `SymbolInfo` (with `.Symbol`) | For references/expressions |
| `GetOperation(node)` | ✅ Public | `IOperation?` | Expensive in SyntaxNodeAction context |
| `GetTypeInfo(node)` | ❌ Internal only | N/A | Use `GetSymbolInfo` on variable/parameter to get type instead |

### Getting types without GetTypeInfo

```csharp
var symbolInfo = semanticModel.GetSymbolInfo(receiverExpression, ct);
ITypeSymbol? type = symbolInfo.Symbol switch
{
    IVariableSymbol v => v.Type,
    IParameterSymbol p => p.ParameterType,
    IMethodSymbol m => m.ReturnValueSymbol?.ReturnType,
    _ => null
};
```

## IMethodSymbol members for variable resolution

`IMethodSymbol` exposes locals and parameters with their types pre-resolved:

```csharp
var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, ct) as IMethodSymbol;

// Local variables with types
foreach (var local in methodSymbol.LocalVariables)
{
    // local.Name: variable name
    // local.Type: ITypeSymbol (already resolved, can cast to IRecordTypeSymbol etc.)
}

// Parameters with types
foreach (var param in methodSymbol.Parameters)
{
    // param.Name: parameter name
    // param.ParameterType: ITypeSymbol
    // param.IsVar: whether it's a var parameter
}
```

Cost: `GetDeclaredSymbol(methodSyntax)` is ~10μs/call (cheap; resolves the method signature without binding the body).

## Key symbol interfaces

| Interface | Key properties | Use for |
|---|---|---|
| `IVariableSymbol` | `.Name`, `.Type`, `.VariableKind` | Local/global variables |
| `IParameterSymbol` | `.Name`, `.ParameterType`, `.IsVar`, `.Ordinal` | Method parameters |
| `IMethodSymbol` | `.Name`, `.MethodKind`, `.LocalVariables`, `.Parameters`, `.ReturnValueSymbol` | Methods/triggers |
| `IReturnValueSymbol` | `.ReturnType`, `.IsNamed`, `.IsOptional` | Method return values |
| `IRecordTypeSymbol` | `.BaseTable`, `.Temporary` (extends `IApplicationObjectTypeSymbol`) | Record variables |
| `ITableTypeSymbol` | `.Id`, `.Name`, `.TableType` | Table declarations |
| `IApplicationObjectTypeSymbol` | `.Kind`, `.Id`, `.Name`, `.GetMembers()`, `.GetProperty()` | Any AL object |

## Variable-map pattern for bulk invocation analysis

When analyzing all DB invocations within an object (e.g., for permissions analysis), the optimal pattern avoids `GetOperation` entirely:

```csharp
// 1. Build global Record variable map from object members
Dictionary<string, IRecordTypeSymbol>? globalMap = null;
foreach (var member in containingObject.GetMembers())
{
    if (member is IVariableSymbol v && v.Type is IRecordTypeSymbol r && !r.Temporary)
    {
        globalMap ??= new(StringComparer.OrdinalIgnoreCase);
        globalMap.TryAdd(v.Name, r);
    }
}

// 2. For each method: build local map + walk invocations
var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, ct) as IMethodSymbol;
Dictionary<string, IRecordTypeSymbol>? localMap = null;
foreach (var local in methodSymbol.LocalVariables)
{
    if (local.Type is IRecordTypeSymbol r && !r.Temporary)
    {
        localMap ??= new(StringComparer.OrdinalIgnoreCase);
        localMap.TryAdd(local.Name, r);
    }
}

// 3. Resolve invocations via map lookup (fast) or GetSymbolInfo (fallback)
foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
{
    // Extract receiver name from syntax
    // Try localMap then globalMap
    // Only call GetSymbolInfo for complex receivers (function calls, etc.)
}
```

Performance profile (Base Application, 611 objects with Permissions):
- Variable map build: ~87ms (includes GetDeclaredSymbol for 7,348 methods)
- Walk + syntax resolve: ~144ms
- GetSymbolInfo fallback: ~13ms (only ~34% of invocations need this)
- **Total: ~688ms** vs 3,100ms for the GetOperation approach (4.5x faster)

## RegisterCompilationStartAction: safe uses

`CompilationStartAction` is safe for:
1. Loading expensive resources once (XLIFF files, settings)
2. Registering per-symbol/per-operation actions that are individually self-contained
3. Building read-only indexes that inner callbacks consume

It is NOT safe for accumulating mutable state across `CodeBlockAction` callbacks that is later consumed in `CompilationEndAction`.

## SDK source locations (decompiled, for reference)

| File | Key content |
|---|---|
| `AnalyzerExecutor.cs:996-1001` | `TryStartAnalyzingDeclaration` skip gate (null `AnalysisState` → always execute) |
| `AnalyzerExecutor.cs:983 / 1086 / 1110 / 1128` | Identical skip gate for SyntaxNode, Operation, OperationBlock, CodeBlock actions |
| `AnalyzerDriverBase.cs:371-394` | Both driver entry points pass null `AnalysisState` |
| `AnalyzerDriver.cs:274-275` | `cacheAnalysisData` flag evaluates false on all real paths |
| `AnalyzerDriver.cs:284-365` | Guaranteed execution order (SyntaxNode → Operation → CodeBlock) |
| `AnalyzerDriver.cs:504-518` | `GetOperationBlocksToAnalyze` pre-computation |
| `AnalysisScope.cs:80-100` | `ShouldAnalyze(ISymbol)` per-file scope filtering |
| `AnalyzersHelper.cs:70-78` | Module-only pass: enqueues only `SymbolDeclaredCompilationEvent` |
| `ProjectInfo.cs:113` | Analyzer instances materialized once per project (shared across passes) |
| `SemanticModel.cs:43-45` | `GetSymbolInfo(SyntaxNode)` public API |
| `SemanticModel.cs:302` | `GetSymbolInfo(ExpressionSyntax)` public API |
| `SemanticModel.cs:1130` | `GetOperation(SyntaxNode)` public API |

## Common pitfalls

### AL method calls without parentheses

**CRITICAL:** AL allows calling methods without parentheses (e.g., `MyTable.Count` instead of `MyTable.Count()`). When parentheses are omitted, the parser produces a `MemberAccessExpressionSyntax` instead of wrapping it in an `InvocationExpressionSyntax`.

Impact on analyzers:

1. **Manual syntax walks** that filter on `InvocationExpressionSyntax` miss these calls entirely.
2. **Operation-based analyzers** that cast `operation.Syntax` to `InvocationExpressionSyntax` get a null/failed cast (the operation is still delivered as `IInvocationExpression`, but `.Syntax` points to `MemberAccessExpressionSyntax`).

Recommended patterns:

```csharp
// Pattern 1: Manual syntax walk — handle both forms
foreach (var descendant in body.DescendantNodes())
{
    if (descendant is InvocationExpressionSyntax invocation)
    {
        // Handle invocation with parentheses
    }
    else if (descendant is MemberAccessExpressionSyntax memberAccess
        && memberAccess.Parent is not InvocationExpressionSyntax)
    {
        // Handle method call without parentheses
    }
}

// Pattern 2: Operation-based — handle both syntax forms
if (operation.Syntax is InvocationExpressionSyntax invocationSyntax)
{
    // Extract from InvocationExpressionSyntax
}
else if (operation.Syntax is MemberAccessExpressionSyntax memberAccessSyntax)
{
    // Extract from MemberAccessExpressionSyntax (no-parens form)
}
```

**Best practice:** Prefer `RegisterOperationAction`, which normalizes both forms into `IInvocationExpression`. Only use manual syntax walks when performance requires avoiding `GetOperation()` costs, and then always handle both `InvocationExpressionSyntax` and parentheses-free `MemberAccessExpressionSyntax`.
