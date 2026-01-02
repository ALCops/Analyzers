using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class PublicEventPublisher : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.PublicEventPublisher
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeEventPublisher,
            EnumProvider.SymbolKind.Method
        );

    private static void AnalyzeEventPublisher(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
            return;

        if (!method.IsEvent)
            return;

        if (!IsPublicEventPublisher(method))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PublicEventPublisher,
            method.GetLocation(),
            method.Name.ToString()));
    }

    private static bool IsPublicEventPublisher(IMethodSymbol methodSymbol) =>
        !methodSymbol.IsLocal && !methodSymbol.IsInternal;

}