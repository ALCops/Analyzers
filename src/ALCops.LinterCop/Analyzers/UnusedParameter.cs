using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class UnusedParameter : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UnusedParameter);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCodeBlockStartAction(AnalyzeCodeBlockStart);
    }

    private static void AnalyzeCodeBlockStart(CodeBlockStartAnalysisContext startContext)
    {
        if (startContext.OwningSymbol.Kind != SymbolKind.Method)
            return;

        IMethodSymbol methodSymbol = (IMethodSymbol)startContext.OwningSymbol;

        if (!ShouldAnalyzeMethod(methodSymbol))
            return;

        if (methodSymbol.Parameters.IsEmpty)
            return;

        var tracker = new ParameterUsageTracker(methodSymbol, startContext.CancellationToken);

        if (tracker.IsEmpty)
            return;

        startContext.RegisterSyntaxNodeAction(tracker.AnalyzeSyntaxNode, SyntaxKind.IdentifierName, SyntaxKind.IdentifierNameOrEmpty);
        startContext.RegisterCodeBlockEndAction(tracker.CodeBlockEndAction);
    }

    private static bool ShouldAnalyzeMethod(IMethodSymbol method)
    {
        // Event subscribers are local but explicitly excluded by AA0137,
        // so we handle them here
        if (method.IsEventSubscriber())
            return true;

        // AA0137 already handles local procedures (except event subscribers above)
        if (method.IsLocal)
            return false;

        // Triggers have platform-defined signatures
        if (method.MethodKind == MethodKind.Trigger)
            return false;

        // Event declarations define the subscriber contract
        if (method.IsEvent)
            return false;

        // Obsolete methods should not be modified
        if (method.IsObsoleteRemoved || method.IsObsoletePending)
            return false;

        // Interface implementations are bound by the interface contract
        if (method.MethodImplementsInterfaceMethod())
            return false;

        return true;
    }

    private sealed class ParameterUsageTracker
    {
        private readonly HashSet<ISymbol> unusedParameters;
        private readonly HashSet<string> unusedParameterNames;
        private readonly IMethodSymbol method;

        public bool IsEmpty => unusedParameters.Count == 0;

        public ParameterUsageTracker(IMethodSymbol method, CancellationToken cancellationToken)
        {
            this.method = method;
            unusedParameters = new HashSet<ISymbol>();
            unusedParameterNames = new HashSet<string>(SemanticFacts.NameEqualityComparer);

            ImmutableArray<IParameterSymbol>.Enumerator enumerator = method.Parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                IParameterSymbol parameter = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                if (!parameter.IsSynthesized)
                {
                    unusedParameters.Add(parameter);
                    unusedParameterNames.Add(parameter.Name);
                }
            }
        }

        public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            if (unusedParameters.Count == 0)
                return;

            // Skip identifiers that are part of parameter declarations themselves
            if (context.Node.Parent.IsKind(SyntaxKind.Parameter, SyntaxKind.ReturnValue))
                return;

            if (context.Node is not IdentifierNameSyntax identifierNameSyntax)
                return;

            if (!unusedParameterNames.Contains(identifierNameSyntax.Unquoted()))
                return;

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax, context.CancellationToken).Symbol;
            if (symbol is not null && unusedParameters.Contains(symbol))
            {
                unusedParameters.Remove(symbol);
                unusedParameterNames.Remove(symbol.Name);
            }
        }

        public void CodeBlockEndAction(CodeBlockAnalysisContext context)
        {
            foreach (ISymbol parameter in unusedParameters)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.UnusedParameter,
                        parameter.GetLocation(),
                        parameter.Name,
                        method.Name));
            }
        }
    }
}
