using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Extensions;

public static class SymbolInterfaceExtensions
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

    public static IPageTypeSymbol? GetPageTypeSymbol(this ISymbol symbol)
    {
        var declaredType = (symbol.OriginalDefinition ?? symbol).GetTypeSymbol();
        declaredType = (declaredType?.OriginalDefinition as ITypeSymbol) ?? declaredType;
        return declaredType as IPageTypeSymbol;
    }

    public static IEnumerable<IControlSymbol>? GetFlattenedControls(this ISymbol? symbol) =>
        symbol switch
        {
            IPageBaseTypeSymbol page => page.FlattenedControls,
            IPageExtensionBaseTypeSymbol pageExtension => pageExtension.AddedControlsFlattened,
            _ => null
        };

    public static string GetFullyQualifiedObjectName(this ISymbol symbol, bool quoteIdentifierIfNeeded = false)
    {
        var symbolName = quoteIdentifierIfNeeded
               ? symbol.Name.QuoteIdentifierIfNeededWithReflection()
               : symbol.Name;

        var containingNamespace = symbol.GetContainingNamespaceQualifiedNameWithReflection();
        if (string.IsNullOrEmpty(containingNamespace))
            return symbolName;

        return $"{containingNamespace}.{symbolName}";
    }

    #region Obsolete Extension Methods
    private static readonly Lazy<PropertyInfo?> _isObsoletePendingMoveProperty =
        new(() => typeof(ISymbol).GetProperty("IsObsoletePendingMove"));

    private static readonly Lazy<PropertyInfo?> _isObsoleteMovedProperty =
        new(() => typeof(ISymbol).GetProperty("IsObsoleteMoved"));

    private static bool GetObsoletePropertyValue(ISymbol symbol, PropertyInfo? property) =>
        property?.GetValue(symbol) as bool? ?? false;

    public static bool IsObsolete(this ISymbol symbol)
    {
        // Check the "always available" properties first
        if (symbol.IsObsoletePending || symbol.IsObsoleteRemoved)
        {
            return true;
        }

        // Use reflection to check properties that are not available in older versions
        if (GetObsoletePropertyValue(symbol, _isObsoleteMovedProperty.Value))
        {
            return true;
        }

        if (GetObsoletePropertyValue(symbol, _isObsoletePendingMoveProperty.Value))
        {
            return true;
        }

        return false;
    }
    #endregion

#if NETSTANDARD2_1
    public static string ToDisplayStringWithReflection(this ISymbol symbol) =>
        SymbolHelper.ToDisplayStringWithReflection(symbol);
#endif
}