using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Extensions;

public static class TypeSymbolInterfaceExtensions
{
    public static int GetTypeLength(this ITypeSymbol type, ref bool isError)
    {
        if (!type.IsTextType())
        {
            isError = true;
            return 0;
        }
        if (type.HasLength)
            return type.Length;
        return type.NavTypeKind == EnumProvider.NavTypeKind.Label ? GetLabelTypeLength(type) : int.MaxValue;
    }

    private static int GetLabelTypeLength(ITypeSymbol type)
    {
        ILabelTypeSymbol labelType = (ILabelTypeSymbol)type;

        if (labelType.Locked is true)
            return labelType.Text?.Length ?? 0;

        return labelType.MaxLength;
    }
}