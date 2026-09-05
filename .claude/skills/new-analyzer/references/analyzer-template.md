# Analyzer class templates

Used by `/new-analyzer` step 3. The rules every analyzer follows are in `.claude/rules/analyzer-development.md`; pick the shape below that matches the registration you chose, then copy the nearest existing analyzer with the same shape for the details.

## Symbol action (most rules)

```csharp
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.{Cop}.Analyzers;

[DiagnosticAnalyzer]
public sealed class {RuleName} : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.{RuleName});

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeSymbol,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.Page);

    private static void AnalyzeSymbol(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Symbol.GetProperty(EnumProvider.PropertyKind.Access) is not null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.{RuleName},
            ctx.Symbol.GetLocation(),
            ctx.Symbol.Kind.ToString(),   // fills {0}
            ctx.Symbol.Name));            // fills {1}
    }
}
```

## Operation action (a rare operation kind, no per-body state)

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterOperationAction(AnalyzeInvocation, EnumProvider.OperationKind.InvocationExpression);

private static void AnalyzeInvocation(OperationAnalysisContext ctx)
{
    if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
        return;

    if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
        !SemanticFacts.IsSameName(invocation.TargetMethod.Name, "Commit"))
        return;

    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.{RuleName}, ctx.Operation.Syntax.GetLocation()));
}
```

For `InvocationExpression` on a hot path, use a code block action with a pre-filter instead (`analyzer-performance.md`).

## Syntax node action

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, EnumProvider.SyntaxKind.StringLiteralValue);

private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext ctx)
{
    if (ctx.IsObsolete() || ctx.Node is not StringLiteralValueSyntax literal)
        return;

    // ctx.SemanticModel for resolution, ctx.ContainingSymbol for the enclosing symbol
    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.{RuleName}, ctx.Node.GetLocation()));
}
```

## Compilation start with a once-per-compilation resource

```csharp
public override void Initialize(AnalysisContext context) =>
    context.RegisterCompilationStartAction(CompilationStart);

private static void CompilationStart(CompilationStartAnalysisContext ctx)
{
    var fileSystem = ctx.Compilation.FileSystem;          // null in some test and IDE contexts
    if (fileSystem is null)
        return;

    var settings = ALCopsSettingsProvider.GetSettings(fileSystem);
    var index = BuildIndex(fileSystem);                   // XLIFF, cross-object index, ...
    if (index is null)
        return;                                           // rule becomes a no-op without the resource

    ctx.RegisterSymbolAction(
        symbolCtx => AnalyzeSymbol(symbolCtx, index, settings),   // closure, never an instance field
        EnumProvider.SymbolKind.Table,
        EnumProvider.SymbolKind.Page);
}
```

Inner actions must stay self-contained: they read `index` and `settings`, they never write to shared state for a later action to report (`sdk-analysis-scope.md`).
