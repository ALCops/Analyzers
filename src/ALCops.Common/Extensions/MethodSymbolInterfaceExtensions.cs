using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public enum MethodSymbolDisplayFormat
{
    MethodOnly,
    MethodSignature,
    ObjectNameWithMethod,
    ObjectNameWithMethodSignature
}

public static class MethodSymbolInterfaceExtensions
{
    public static string GetDiagnosticDisplayText(this IMethodSymbol methodSymbol, MethodSymbolDisplayFormat format)
    {
        var methodName = methodSymbol.Name.QuoteIdentifierIfNeededWithReflection();

        return format switch
        {
            MethodSymbolDisplayFormat.MethodOnly => $"{methodName}()",
            MethodSymbolDisplayFormat.MethodSignature => $"{methodName}({GetParameterTypeList(methodSymbol)})",
            MethodSymbolDisplayFormat.ObjectNameWithMethod => $"{GetObjectPrefix(methodSymbol)}{methodName}()",
            MethodSymbolDisplayFormat.ObjectNameWithMethodSignature =>
                $"{GetObjectPrefix(methodSymbol)}{methodName}({GetParameterTypeList(methodSymbol)})",
            _ => $"{GetObjectPrefix(methodSymbol)}{methodName}({GetParameterTypeList(methodSymbol)})"
        };
    }

    public static bool MethodImplementsInterfaceMethod(this IMethodSymbol methodSymbol)
    {
        if (methodSymbol is null)
            return false;

        return methodSymbol.GetContainingApplicationObjectTypeSymbol()
                           .MethodImplementsInterfaceMethod(methodSymbol);
    }

    /// <summary>
    /// Checks whether the method is a handler function (e.g. MessageHandler, ConfirmHandler, etc.)
    /// by reflecting on the internal MethodSymbol.IsHandler property.
    /// Returns false if the property is not available (fails open: diagnostic fires).
    /// </summary>
    public static bool IsHandler(this IMethodSymbol method)
        => method.GetPropertyIfExists<bool>("IsHandler");

    /// <summary>
    /// Checks whether the method is an IntegrationEvent or BusinessEvent.
    /// </summary>
    public static bool IsIntegrationOrBusinessEvent(this IMethodSymbol methodSymbol) =>
        methodSymbol.Attributes.Any(attr => (attr.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent) || (attr.AttributeKind == EnumProvider.AttributeKind.BusinessEvent));

    /// <summary>
    /// Checks whether the method is an InternalEvent.
    /// </summary>
    public static bool IsInternalEvent(this IMethodSymbol methodSymbol) =>
        methodSymbol.Attributes.Any(attr => attr.AttributeKind == EnumProvider.AttributeKind.InternalEvent);


    public static bool MethodImplementsInterfaceMethod(this IMethodSymbol methodSymbol, IMethodSymbol interfaceMethodSymbol)
    {
        if (methodSymbol is null || interfaceMethodSymbol is null)
            return false;

        if (!SemanticFacts.IsSameName(methodSymbol.Name, interfaceMethodSymbol.Name))
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

    private static string GetObjectPrefix(IMethodSymbol methodSymbol)
    {
        var containingObject = methodSymbol.GetContainingApplicationObjectTypeSymbol();

        if (containingObject is null)
        {
            return string.Empty;
        }

        return $"{containingObject.Name.QuoteIdentifierIfNeededWithReflection()}.";
    }

    private static string GetParameterTypeList(IMethodSymbol methodSymbol) =>
        string.Join(", ",
            methodSymbol.Parameters.Select(parameter => GetParameterTypeDisplay(parameter.ParameterType)));

    private static string GetParameterTypeDisplay(ITypeSymbol typeSymbol)
#if NETSTANDARD2_1
        => typeSymbol.ToDisplayStringWithReflection();
#else
        => typeSymbol.ToDisplayString();
#endif
}