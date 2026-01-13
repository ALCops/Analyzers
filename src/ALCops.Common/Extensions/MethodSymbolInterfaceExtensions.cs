using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class MethodSymbolInterfaceExtensions
{
    public static bool MethodImplementsInterfaceMethod(this IMethodSymbol methodSymbol)
    {
        if (methodSymbol is null)
            return false;

        return methodSymbol.GetContainingApplicationObjectTypeSymbol()
                           .MethodImplementsInterfaceMethod(methodSymbol);
    }

    public static bool MethodImplementsInterfaceMethod(this IMethodSymbol methodSymbol, IMethodSymbol interfaceMethodSymbol)
    {
        if (methodSymbol is null || interfaceMethodSymbol is null)
            return false;

        if (!string.Equals(methodSymbol.Name, interfaceMethodSymbol.Name, StringComparison.Ordinal))
            return false;

        if (methodSymbol.Parameters.Length != interfaceMethodSymbol.Parameters.Length)
            return false;

        var methodReturnValType = methodSymbol.ReturnValueSymbol?.ReturnType.NavTypeKind ?? EnumProvider.NavTypeKind.None;
        var interfaceMethodReturnValType = interfaceMethodSymbol.ReturnValueSymbol?.ReturnType.NavTypeKind ?? EnumProvider.NavTypeKind.None;
        if (methodReturnValType != interfaceMethodReturnValType)
            return false;

        for (int i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            var methodParameter = methodSymbol.Parameters[i];
            var interfaceMethodParameter = interfaceMethodSymbol.Parameters[i];

            if (methodParameter.IsVar != interfaceMethodParameter.IsVar)
                return false;

            if (!methodParameter.ParameterType.Equals(interfaceMethodParameter.ParameterType))
                return false;
        }

        return true;
    }
}