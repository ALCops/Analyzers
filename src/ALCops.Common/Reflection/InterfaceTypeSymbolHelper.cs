using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Reflection;

/// <summary>
/// COMPAT(netstandard2.1): Reflection-based access to <c>IInterfaceTypeSymbol.GetAllExtendedInterfaces()</c>.
/// Interface <c>extends</c> arrived with SDK 14.0, so the member is absent from the SDK the
/// netstandard2.1 binary compiles against, while that binary also runs on SDKs that have it.
/// The reflection call is used on every target framework to keep one code path.
/// </summary>
public static class InterfaceTypeSymbolHelper
{
    private static readonly Lazy<Func<IInterfaceTypeSymbol, ImmutableArray<IInterfaceTypeSymbol>>?> _getAllExtendedInterfaces =
        new(CreateGetAllExtendedInterfacesInvoker, LazyThreadSafetyMode.PublicationOnly);

    private static Func<IInterfaceTypeSymbol, ImmutableArray<IInterfaceTypeSymbol>>? CreateGetAllExtendedInterfacesInvoker()
    {
        var method = typeof(IInterfaceTypeSymbol).GetMethod(
            "GetAllExtendedInterfaces",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method is null || method.ReturnType != typeof(ImmutableArray<IInterfaceTypeSymbol>))
            return null;

        return (Func<IInterfaceTypeSymbol, ImmutableArray<IInterfaceTypeSymbol>>)Delegate.CreateDelegate(
            typeof(Func<IInterfaceTypeSymbol, ImmutableArray<IInterfaceTypeSymbol>>), method);
    }

    /// <summary>
    /// Returns every interface the given interface extends, directly or transitively, excluding
    /// the interface itself. Returns an empty array when the loaded SDK has no interface <c>extends</c>.
    /// </summary>
    public static ImmutableArray<IInterfaceTypeSymbol> GetAllExtendedInterfacesWithReflection(IInterfaceTypeSymbol interfaceSymbol)
    {
        var getAllExtendedInterfaces = _getAllExtendedInterfaces.Value;
        if (getAllExtendedInterfaces is null)
            return ImmutableArray<IInterfaceTypeSymbol>.Empty;

        var extendedInterfaces = getAllExtendedInterfaces(interfaceSymbol);
        return extendedInterfaces.IsDefault ? ImmutableArray<IInterfaceTypeSymbol>.Empty : extendedInterfaces;
    }
}
