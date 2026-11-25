using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
#if NETSTANDARD2_1
using ALCops.Common.Reflection;
#endif

namespace ALCops.Common.Extensions;

public static class InterfaceTypeSymbolExtensions
{
    /// <summary>
    /// Returns every interface this interface extends, directly or transitively, excluding itself.
    /// COMPAT(netstandard2.1): The member is absent from the 12.0 SDK the netstandard2.1 binary
    /// compiles against, so that build uses reflection and returns empty on SDKs without interface
    /// <c>extends</c> (before 14.0). On net8.0+, calls the SDK directly.
    /// TODO: When netstandard2.1 is dropped, call GetAllExtendedInterfaces() directly and delete
    /// InterfaceTypeSymbolHelper.
    /// </summary>
    public static ImmutableArray<IInterfaceTypeSymbol> GetAllExtendedInterfacesWithReflection(this IInterfaceTypeSymbol interfaceSymbol)
#if NETSTANDARD2_1
        => InterfaceTypeSymbolHelper.GetAllExtendedInterfaces(interfaceSymbol);
#else
        => interfaceSymbol.GetAllExtendedInterfaces();
#endif
}
