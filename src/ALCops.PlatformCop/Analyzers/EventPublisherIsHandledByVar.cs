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
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
            return;

        if (!method.IsEvent)
            return;

        foreach (var parameter in method.Parameters)
        {
            if (IsValidIsHandledParameter(parameter))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.EventPublisherIsHandledByVar,
                parameter.GetLocation()));
        }
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