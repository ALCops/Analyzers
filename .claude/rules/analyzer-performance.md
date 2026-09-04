---
paths:
  - "src/ALCops.*/Analyzers/**"
---

# Analyzer performance

The Base Application has on the order of a hundred thousand method and trigger bodies and more invocation expressions than that. Any per-method or per-invocation cost is multiplied by those counts, and the editor re-runs analyzers on every keystroke pass (`sdk-analysis-scope.md`). The patterns below come from regressions that were measured on the Base Application.

## Cost model

| Call | Cost | Why |
|---|---|---|
| `GetOperation(body)` in `CodeBlockAction` | negligible | the driver pre-computes operation blocks before the callback |
| `GetOperation(node)` in `SyntaxNodeAction` or `SymbolAction` | expensive | full binding of the enclosing body per call |
| `GetSymbolInfo(node)` | moderate | targeted resolution of one node; fine for the few nodes you care about |
| `GetDeclaredSymbol(methodSyntax)` | cheap | resolves the signature, not the body |
| `IOperation.GetSymbolSafe()` | free | a type check on an already bound node |
| `RegisterOperationAction(kind)` | one dispatch per matching operation in the compilation | for `InvocationExpression` that is every call site in the app |

## Prefer one callback per body over one per invocation

`RegisterOperationAction(..., InvocationExpression)` fires for every call in the compilation, each with dispatch overhead, even when the rule only cares about bodies that meet a cheap precondition (an object with a `Permissions` property, a body that mentions a relevant method name). Use `RegisterCodeBlockAction`, apply the pre-filter, then bind once:

```csharp
ctx.RegisterCodeBlockAction(ctx =>
{
    if (ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax method) return;
    var obj = ctx.OwningSymbol?.GetContainingApplicationObjectTypeSymbol();
    if (obj?.GetProperty(EnumProvider.PropertyKind.Permissions) is null) return;   // cheap pre-filter

    var operation = ctx.SemanticModel.GetOperation(method.Body, ctx.CancellationToken);
    if (operation is null) return;
    new MyWalker(/* ... */).Visit(operation);                                       // one bind, one walk
});
```

`PartialRecordOperations` (PC0030/PC0031) and `TableDataAccessUnusedPermissions` (AC0032) are the reference implementations. `RegisterOperationAction` stays right for rules that need no per-body state and whose operation kind is rare.

## Pre-filter on syntax before binding

The syntax tree is already parsed and free to walk. Before `GetOperation`, scan the body for the method names or node kinds the rule can react to and skip the body when none occur. The scan is a case-insensitive text check, acceptable **only** as a pre-filter: a false positive costs one unnecessary bind, a false negative is a correctness bug, so the name set must be complete (`RecordMethodClassification`, `MethodOperationMap`).

When walking syntax for calls, handle both `InvocationExpressionSyntax` and a `MemberAccessExpressionSyntax` whose parent is not an invocation: AL allows `MyTable.Count` without parentheses and the parser produces no invocation node for it. Operation-based code does not have this problem, `IInvocationExpression` covers both spellings, but its `.Syntax` may be either node type.

## The variable-map pattern

For bulk invocation analysis inside an object (permissions, partial records) the fastest correct approach avoids binding altogether for the common case:

1. Object scope: from `containingObject.GetMembers()` collect `IVariableSymbol`s whose `Type` is a record (plus report data items and xmlport table elements via `GetTypeSymbol()`), keyed by name with `SemanticFacts.NameEqualityComparer`.
2. Per method: `GetDeclaredSymbol(methodSyntax)` gives `LocalVariables`, `Parameters` and the named `ReturnValueSymbol` pre-typed; build the local map.
3. Walk the body's invocations; resolve identifier receivers through the local map, then the object map (`record-receiver-forms.md` for the scoping rules and the bare and `this` forms); fall back to `GetSymbolInfo` or `GetOperation(receiver)?.Type` only for the rare complex receivers.

On the Base Application this was several times faster than binding every body, because most receivers are plain identifiers.

## Per-compilation caches

Cross-object lookups such as "all page extensions targeting this page" are expensive (`GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection` walks every referenced module). Compute them once per compilation:

```csharp
private static readonly ConditionalWeakTable<Compilation, Entry> Cache = new();

private static ImmutableArray<IPageExtensionBaseTypeSymbol> GetPageExtensions(Compilation compilation)
    => Cache.GetValue(compilation, static c => new Entry(c)).Value.Value;

private sealed class Entry(Compilation compilation)
{
    public Lazy<ImmutableArray<IPageExtensionBaseTypeSymbol>> Value { get; } = new(
        () => compilation
            .GetApplicationObjectTypeSymbolsByKindAcrossModulesWithReflection(EnumProvider.SymbolKind.PageExtension)
            .OfType<IPageExtensionBaseTypeSymbol>()
            .ToImmutableArray(),
        LazyThreadSafetyMode.ExecutionAndPublication);
}
```

- Keyed on `Compilation`, so the cache dies with it; `ExecutionAndPublication` makes concurrent symbol callbacks compute it once.
- Works within one action kind only; across action kinds the `Compilation` object differs (`sdk-analysis-scope.md`).
- Compare extension targets to base objects with `OriginalDefinition` and, for cross-module symbols where reference equality fails, `ISymbolWithId.Id` plus `Kind` (see `SameApplicationObject` in `DuplicateODataEntityName` and `TransferFieldsSchemaCompatibility`).

## Other rules of thumb

- `ctx.CancellationToken.ThrowIfCancellationRequested()` in loops over fields, members, or bodies.
- Settings and other per-compilation resources are loaded once in `CompilationStart`, never per callback (`common-library.md` for the settings cache).
- `AppSourceCopConfigurationProvider.GetMandatoryNameAffixes` re-reads `AppSourceCop.json` on every call; cache per compilation at the call site.
