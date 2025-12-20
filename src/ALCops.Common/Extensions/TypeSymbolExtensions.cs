using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class TypeSymbolExtensions
{
    public static NavTypeKind GetNavTypeKindSafeWithReflection(this ITypeSymbol type)
    {
        return TypeSymbolHelper.GetNavTypeKindSafe(type);
    }
}