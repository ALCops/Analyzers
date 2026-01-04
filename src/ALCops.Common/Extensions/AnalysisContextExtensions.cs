using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Common.Extensions;

public static class AnalysisContextExtensions
{
    public static bool IsObsolete(this SymbolAnalysisContext context)
    {
        if (context.Symbol.IsObsolete())
        {
            return true;
        }
        if (context.Symbol.GetContainingObjectTypeSymbol().IsObsolete())
        {
            return true;
        }
        return false;
    }

    public static bool IsObsolete(this OperationAnalysisContext context)
    {
        if (context.ContainingSymbol.IsObsolete())
        {
            return true;
        }
        if (context.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsolete())
        {
            return true;
        }
        return false;
    }

    public static bool IsDiagnosticEnabled(this SyntaxNodeAnalysisContext ctx, DiagnosticDescriptor descriptor)
    {
        if (ctx.SemanticModel.Compilation.Options.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var report))
            return report != ReportDiagnostic.Suppress;

        return true;
    }

    public static bool IsObsolete(this SyntaxNodeAnalysisContext context)
    {
        if (context.ContainingSymbol.IsObsolete())
        {
            return true;
        }
        if (context.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsolete())
        {
            return true;
        }
        return false;
    }

    public static bool IsObsolete(this CodeBlockAnalysisContext context)
    {
        if (context.OwningSymbol.IsObsolete())
        {
            return true;
        }
        if (context.OwningSymbol.GetContainingObjectTypeSymbol().IsObsolete())
        {
            return true;
        }
        return false;
    }
}