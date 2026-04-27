using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Extensions;

public static class SymbolInterfaceExtensions
{
    /// <summary>
    /// Gets the QualifiedName of the ContainingNamespace for a symbol.
    /// COMPAT(netstandard2.1, net8.0): Uses reflection to handle versions where
    /// ContainingNamespace or INamespaceSymbol.QualifiedName may not exist.
    /// On net10.0+, calls the SDK directly.
    /// TODO: When netstandard2.1 and net8.0 are dropped, inline as
    /// symbol?.ContainingNamespace?.QualifiedName and delete SymbolHelper.GetContainingNamespaceQualifiedName.
    /// </summary>
    /// <param name="symbol">The symbol to get the containing namespace qualified name from.</param>
    /// <returns>The qualified name of the containing namespace, or null if not available.</returns>
    public static string? GetContainingNamespaceQualifiedNameWithReflection(this ISymbol? symbol)
#if NET10_0_OR_GREATER
        => symbol?.ContainingNamespace?.QualifiedName;
#else
        => SymbolHelper.GetContainingNamespaceQualifiedName(symbol);
#endif

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
    // COMPAT(netstandard2.1, net8.0): IsObsoletePendingMove and IsObsoleteMoved may not exist in older SDK versions.
    // TODO: When netstandard2.1 and net8.0 are dropped, check if these properties are on ISymbol directly.
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

    /// <summary>
    /// Returns true when the symbol has ObsoleteState = Removed (or Moved, when supported).
    /// Unlike <see cref="IsObsolete"/>, this intentionally excludes the Pending state
    /// because pending symbols still participate in runtime operations such as TransferFields.
    /// </summary>
    public static bool IsRemoved(this ISymbol symbol)
    {
        if (symbol.IsObsoleteRemoved)
        {
            return true;
        }

        if (GetObsoletePropertyValue(symbol, _isObsoleteMovedProperty.Value))
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