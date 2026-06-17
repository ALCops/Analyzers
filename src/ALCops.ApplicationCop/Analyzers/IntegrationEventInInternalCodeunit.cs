using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class IntegrationEventInInternalCodeunit : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.IntegrationEventInInternalCodeunit);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeIntegrationEvent,
            EnumProvider.SymbolKind.Method
        );

    private void AnalyzeIntegrationEvent(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.IsEvent)
            return;

        IApplicationObjectTypeSymbol? applicationObject = methodSymbol.GetContainingApplicationObjectTypeSymbol();

        if (applicationObject is null || !applicationObject.IsInternalCodeunit())
            return;

        if (!IsIntegrationEvent(methodSymbol))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.IntegrationEventInInternalCodeunit,
            methodSymbol.GetLocation(),
            methodSymbol.Name,
            applicationObject.Name));
    }

    private static bool IsIntegrationEvent(IMethodSymbol methodSymbol) =>
        methodSymbol.Attributes.Any(attr => attr.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent);
}