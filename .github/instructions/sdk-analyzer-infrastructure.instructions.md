---
applyTo: 'src/ALCops.*/Analyzers/**'
---

# NAV SDK Analyzer Infrastructure Internals

Critical knowledge about how the `Microsoft.Dynamics.Nav.CodeAnalysis` SDK executes analyzer callbacks. Understanding these internals is essential for writing correct and performant analyzers.

## Callback execution order (guaranteed)

The SDK executes analyzer callbacks in a guaranteed order per declaration/object:

1. **SyntaxNodeAction** (fires first)
2. **OperationAction** (fires second)
3. **CodeBlockAction** (fires last)

Source: `AnalyzerDriver.cs` lines 284-365 (`TryExecuteDeclaringReferenceActions`).

This ordering means that `GetOperation(body)` called from a `SyntaxNodeAction` fires BEFORE the SDK pre-computes operation trees, while `GetOperation(body)` in a `CodeBlockAction` benefits from pre-computation (effectively a cache hit).

## Incremental compilation and callback skipping

The SDK uses `AnalysisState.declarationAnalysisDataMap` to cache which declarations have been analyzed. During incremental compilation (e.g., in VS Code when editing a file):

- **`CodeBlockAction`** callbacks are SKIPPED for cached (unchanged) declarations
- **`CodeBlockStartAction`** callbacks are ALSO SKIPPED (same cache mechanism)
- **`CompilationEndAction`** ALWAYS fires regardless of what was skipped
- **`SyntaxNodeAction`** tracks per-node via `ProcessedNodes` (line 641-645): if the object node is skipped, no analysis runs and no stale results exist

Source: `AnalyzerExecutor.cs` lines 527-530 (`ShouldExecuteAction`), 881-887.

### Implications for analyzer patterns

| Pattern | Correct under incremental? | Notes |
|---|---|---|
| `RegisterSyntaxNodeAction` on object kinds | ✅ Yes | Either full analysis runs or none; no partial state |
| `RegisterSymbolAction` | ✅ Yes | Symbol-level, self-contained |
| `RegisterOperationAction` | ✅ Yes | Per-invocation, self-contained |
| `RegisterCodeBlockAction` | ⚠️ Risky | Can be skipped for cached declarations |
| `RegisterCodeBlockStartAction` + `CodeBlockEndAction` | ⚠️ Risky | Same skipping mechanism as CodeBlockAction |
| `CompilationStart` + accumulator + `CompilationEnd` | ❌ Broken | Accumulator is incomplete when CodeBlockActions are skipped |

### The two-phase accumulator anti-pattern

**NEVER** use this pattern for analyzers that need per-object completeness:

```csharp
// BROKEN PATTERN - DO NOT USE
context.RegisterCompilationStartAction(startCtx =>
{
    var accumulator = new ConcurrentDictionary<...>();
    
    startCtx.RegisterCodeBlockAction(blockCtx =>
    {
        // This callback is SKIPPED for cached declarations!
        accumulator.TryAdd(...);
    });
    
    startCtx.RegisterCompilationEndAction(endCtx =>
    {
        // This ALWAYS fires, even with incomplete accumulator!
        foreach (var entry in accumulator) { ... }
    });
});
```

Microsoft's own analyzers never use this pattern for the same reason. Their `Rule175` uses `CodeBlockStartAction` + scoped `RegisterSyntaxNodeAction` + `CodeBlockEndAction` for per-method analysis, but only reports within that method (no cross-method accumulation).

## GetOperation performance characteristics

`SemanticModel.GetOperation(node)` has very different performance depending on the callback context:

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
- **Best: variable-map + syntax resolution** for bulk invocation analysis (build type maps from `IMethodSymbol.LocalVariables`/`.Parameters`, resolve via dictionary lookup, fallback to `GetSymbolInfo` only for complex receivers)

## SemanticModel API availability

| Method | Available | Returns | Notes |
|---|---|---|---|
| `GetDeclaredSymbol(node)` | ✅ Public | `ISymbol?` | For declarations (objects, methods, fields, variables) |
| `GetSymbolInfo(node)` | ✅ Public | `SymbolInfo` (with `.Symbol`) | For references/expressions |
| `GetOperation(node)` | ✅ Public | `IOperation?` | Expensive in SyntaxNodeAction context |
| `GetTypeInfo(node)` | ❌ Internal only | N/A | Use `GetSymbolInfo` on variable/parameter to get type instead |

### Getting types without GetTypeInfo

Since `GetTypeInfo` is internal, get the type of an expression via `GetSymbolInfo`:

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

Cost: `GetDeclaredSymbol(methodSyntax)` is ~10μs/call (cheap, just resolves the method signature without binding the body).

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
- **Total: ~688ms** vs 3,100ms for GetOperation approach (4.5x faster)

## RegisterCompilationStartAction: safe uses

`CompilationStartAction` is safe when used to:
1. Load expensive resources once (XLIFF files, settings)
2. Register per-symbol/per-operation actions that are individually self-contained
3. Build read-only indexes that inner callbacks consume

It is NOT safe when used to accumulate mutable state across `CodeBlockAction` callbacks that is later consumed in `CompilationEndAction`.

## SDK source locations (decompiled, for reference)

| File | Key content |
|---|---|
| `AnalyzerExecutor.cs:527-530` | `ShouldExecuteAction` - skipping cached declarations |
| `AnalyzerExecutor.cs:641-645` | SyntaxNodeAction per-node tracking |
| `AnalyzerExecutor.cs:881-887` | `ShouldExecuteAction` method definition |
| `AnalyzerDriver.cs:284-365` | Guaranteed execution order (SyntaxNode → Operation → CodeBlock) |
| `AnalyzerDriver.cs:504-518` | `GetOperationBlocksToAnalyze` pre-computation |
| `AnalysisState.cs` | `declarationAnalysisDataMap` cache |
| `SemanticModel.cs:43-45` | `GetSymbolInfo(SyntaxNode)` public API |
| `SemanticModel.cs:302` | `GetSymbolInfo(ExpressionSyntax)` public API |
| `SemanticModel.cs:1130` | `GetOperation(SyntaxNode)` public API |

## Common pitfalls

### AL method calls without parentheses

**CRITICAL:** AL allows calling methods without parentheses (e.g., `MyTable.Count` instead of `MyTable.Count()`). When parentheses are omitted, the parser produces a `MemberAccessExpressionSyntax` instead of wrapping it in an `InvocationExpressionSyntax`.

**Impact on analyzers:**

1. **Manual syntax walks** that filter on `InvocationExpressionSyntax` will miss these calls entirely.
2. **Operation-based analyzers** that cast `operation.Syntax` to `InvocationExpressionSyntax` will get a null/failed cast (the operation is still delivered as `IInvocationExpression`, but `.Syntax` points to `MemberAccessExpressionSyntax`).

**Recommended patterns:**

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

**Best practice:** Prefer `RegisterOperationAction` which normalizes both forms into `IInvocationExpression`. Only use manual syntax walks when performance requires avoiding `GetOperation()` costs. When you do walk syntax, always handle both `InvocationExpressionSyntax` and parentheses-free `MemberAccessExpressionSyntax`.
