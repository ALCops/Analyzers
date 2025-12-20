using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Reflection;

/// <summary>
/// Provides safe type symbol access methods using reflection.
/// These methods are designed to maintain compatibility across different API versions
/// </summary>
public static class TypeSymbolHelper
{
    // Cache the MethodInfo for GetNavTypeKindSafe extension method (may not exist in older versions)
    private static readonly Lazy<MethodInfo?> _getNavTypeKindSafeMethod =
        new(() =>
        {
            var extensionsType = typeof(ITypeSymbol).Assembly.GetType("Microsoft.Dynamics.Nav.CodeAnalysis.Symbols.TypeSymbolExtensions");
            return extensionsType?.GetMethod("GetNavTypeKindSafe", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ITypeSymbol) }, null);
        });

    // Cache the PropertyInfo for NavTypeKind on ITypeSymbol (fallback for older versions)
    private static readonly Lazy<PropertyInfo?> _navTypeKindProperty =
        new(() => typeof(ITypeSymbol).GetProperty("NavTypeKind", BindingFlags.Public | BindingFlags.Instance));

    /// <summary>
    /// Gets the NavTypeKind for a type symbol using reflection.
    /// If the GetNavTypeKindSafe method doesn't exist (older versions of the AL Language API),
    /// falls back to directly accessing the NavTypeKind property.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the NavTypeKind from.</param>
    /// <returns>The NavTypeKind of the type symbol.</returns>
    public static NavTypeKind GetNavTypeKindSafe(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return NavTypeKind.None;

        try
        {
            // Check if GetNavTypeKindSafe extension method exists
            var method = _getNavTypeKindSafeMethod.Value;
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { typeSymbol });
                if (result is NavTypeKind navTypeKind)
                    return navTypeKind;
            }

            // Fallback: Method doesn't exist in this version, access NavTypeKind property directly
            return GetNavTypeKindFallback(typeSymbol);
        }
        catch (Exception)
        {
            // Silently ignore if method doesn't exist or can't be invoked
            // This maintains compatibility across different API versions
            return GetNavTypeKindFallback(typeSymbol);
        }
    }

    /// <summary>
    /// Fallback logic to determine the NavTypeKind when the GetNavTypeKindSafe method
    /// is not available in older versions of the API.
    /// Directly accesses the NavTypeKind property on ITypeSymbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the NavTypeKind from.</param>
    /// <returns>The NavTypeKind of the type symbol.</returns>
    private static NavTypeKind GetNavTypeKindFallback(ITypeSymbol typeSymbol)
    {
        var property = _navTypeKindProperty.Value;
        if (property != null)
        {
            var result = property.GetValue(typeSymbol);
            if (result is NavTypeKind navTypeKind)
                return navTypeKind;
        }

        return NavTypeKind.None;
    }
}