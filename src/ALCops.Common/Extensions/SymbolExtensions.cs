using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class SymbolExtensions
{
    /// <summary>
    /// Gets the QualifiedName of the ContainingNamespace for a symbol using reflection.
    /// This method handles breaking changes between versions where ContainingNamespace
    /// may not exist in older versions of Microsoft.Dynamics.Nav.CodeAnalysis.
    /// </summary>
    /// <param name="symbol">The symbol to get the containing namespace qualified name from.</param>
    /// <returns>The qualified name of the containing namespace, or null if not available.</returns>
    public static string? GetContainingNamespaceQualifiedNameWithReflection(this ISymbol? symbol)
        => SymbolHelper.GetContainingNamespaceQualifiedName(symbol);
}
