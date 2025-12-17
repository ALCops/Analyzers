using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Reflection;

/// <summary>
/// Provides safe symbol property access methods using reflection.
/// These methods are designed to maintain compatibility across different API versions
/// where properties like ContainingNamespace may not exist in older versions.
/// </summary>
public static class SymbolHelper
{
    // Cache the PropertyInfo for ContainingNamespace on ISymbol (may not exist in older versions)
    private static readonly Lazy<PropertyInfo?> _containingNamespaceProperty =
        new(() => typeof(ISymbol).GetProperty("ContainingNamespace", BindingFlags.Public | BindingFlags.Instance));

    // Cache the PropertyInfo for QualifiedName on INamespaceSymbol
    private static readonly Lazy<PropertyInfo?> _qualifiedNameProperty =
        new(() =>
        {
            var namespaceSymbolType = Assembly.GetAssembly(typeof(ISymbol))?.GetType("Microsoft.Dynamics.Nav.CodeAnalysis.INamespaceSymbol");
            return namespaceSymbolType?.GetProperty("QualifiedName", BindingFlags.Public | BindingFlags.Instance);
        });

    /// <summary>
    /// Gets the QualifiedName of the ContainingNamespace for a symbol using reflection.
    /// Returns null if the ContainingNamespace property doesn't exist (older versions)
    /// or if the symbol has no containing namespace.
    /// </summary>
    /// <param name="symbol">The symbol to get the containing namespace qualified name from.</param>
    /// <returns>The qualified name of the containing namespace, or null if not available.</returns>
    public static string? GetContainingNamespaceQualifiedName(ISymbol? symbol)
    {
        if (symbol == null)
            return null;

        try
        {
            // Get the ContainingNamespace property (may not exist in older versions)
            var containingNamespaceProp = _containingNamespaceProperty.Value;
            if (containingNamespaceProp == null)
                return null;

            // Get the namespace symbol value
            var namespaceSymbol = containingNamespaceProp.GetValue(symbol);
            if (namespaceSymbol == null)
                return null;

            // Get the QualifiedName from the namespace symbol
            var qualifiedNameProp = _qualifiedNameProperty.Value;
            if (qualifiedNameProp == null)
                return null;

            return qualifiedNameProp.GetValue(namespaceSymbol) as string;
        }
        catch (Exception)
        {
            // Silently ignore if properties don't exist or can't be read
            // This maintains compatibility across different API versions
            return null;
        }
    }
}
