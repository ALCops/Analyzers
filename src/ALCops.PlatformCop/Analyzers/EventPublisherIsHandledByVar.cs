using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class EventPublisherIsHandledByVar : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.EventPublisherIsHandledByVar
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzerEventPublisher,
            EnumProvider.SymbolKind.Method
        );

    private void AnalyzerEventPublisher(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!HasPublisherEventAttribute(methodSymbol))
            return;

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (IsValidIsHandledParameter(parameter))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EventPublisherIsHandledByVar,
                parameter.GetLocation()));
        }
    }

    private static bool HasPublisherEventAttribute(IMethodSymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.Attributes)
        {
            var kind = attr.AttributeKind;

            if (kind == EnumProvider.AttributeKind.BusinessEvent ||
                kind == EnumProvider.AttributeKind.IntegrationEvent ||
                kind == EnumProvider.AttributeKind.InternalEvent)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidIsHandledParameter(IParameterSymbol parameter)
    {
        if (!IsHandledName(parameter.Name))
            return true;

        if (parameter.ParameterType.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Boolean)
            return true;

        return parameter.IsVar;
    }

    private static bool IsHandledName(string name) =>
        SemanticFacts.IsSameName(name, "IsHandled") ||
        SemanticFacts.IsSameName(name, "Handled");
}