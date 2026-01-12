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

#if NETSTANDARD2_1
    /// Handling breaking changes between different versions of Microsoft.Dynamics.Nav.CodeAnalysis
    /// .ToDisplayString() < 13.0 vs .ToDisplayString(SymbolDisplayFormat) >= 13.0
    private static readonly Lazy<Func<ISymbol, string>> _toDisplayString = new(CreateToDisplayStringInvoker);
    private static Func<ISymbol, string> CreateToDisplayStringInvoker()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        // Prefer newer overload: ToDisplayString(SymbolDisplayFormat)
        var withFormat =
            typeof(ISymbol).GetMethod(
                "ToDisplayString",
                flags,
                binder: null,
                types: new[] { typeof(SymbolDisplayFormat) },
                modifiers: null);

        if (withFormat is not null)
            return s => (string)withFormat.Invoke(s, new object?[] { null })!;

        // Fallback: ToDisplayString()
        var parameterless =
            typeof(ISymbol).GetMethod(
                "ToDisplayString",
                flags,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

        if (parameterless is not null)
            return s => (string)parameterless.Invoke(s, null)!;

        // Absolute fallback
        return s => s.Name ?? string.Empty;
    }

    public static string ToDisplayStringWithReflection(ISymbol? symbol)
    {
        if (symbol is null)
            return string.Empty;

        return _toDisplayString.Value(symbol);
    }
#endif
}