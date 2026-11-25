using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Extensions;

public static class InterfaceTypeSymbolExtensions
{
    /// <summary>
    /// Returns every interface this interface extends, directly or transitively, excluding itself.
    /// Empty on SDKs without interface <c>extends</c>.
    /// </summary>
    public static ImmutableArray<IInterfaceTypeSymbol> GetAllExtendedInterfacesWithReflection(this IInterfaceTypeSymbol interfaceSymbol)
        => InterfaceTypeSymbolHelper.GetAllExtendedInterfacesWithReflection(interfaceSymbol);
}
