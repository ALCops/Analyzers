using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class ArgumentInterfaceExtensions
{
    public static ITypeSymbol? GetTypeSymbol(this IArgument argument)
    {
        switch (argument.Value.Kind)
        {
            case var _ when argument.Value.Kind == EnumProvider.OperationKind.ConversionExpression:
                return ((IConversionExpression)argument.Value).Operand.Type;
            case var _ when argument.Value.Kind == EnumProvider.OperationKind.InvocationExpression:
                return ((IInvocationExpression)argument.Value).TargetMethod.ReturnValueSymbol.ReturnType;
        }
        return null;
    }
}