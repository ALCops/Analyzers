using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.Common.Extensions;

public static class TypeSymbolExtensions
{
    public static NavTypeKind GetNavTypeKindSafeWithReflection(this ITypeSymbol type)
    {
        return type?.NavTypeKind ?? NavTypeKind.None;
    }
}