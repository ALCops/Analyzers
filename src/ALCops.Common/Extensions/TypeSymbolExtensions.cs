using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Extensions;

public static class TypeSymbolExtensions
{
    public static IMethodSymbol? FindMethodByNameAcrossModules(this IApplicationObjectTypeSymbol applicationObject, string memberName, Compilation compilation)
    {
        foreach (ISymbol member in applicationObject.GetMembers(memberName))
        {
            if (member.Kind == EnumProvider.SymbolKind.Method)
                return (IMethodSymbol)member;
        }
        foreach (var extensionsAcrossModule in compilation.GetApplicationObjectExtensionTypeSymbolsAcrossModules(applicationObject))
        {
            foreach (var member in extensionsAcrossModule.GetMembers(memberName))
            {
                if (member.Kind == EnumProvider.SymbolKind.Method)
                {
                    IMethodSymbol firstMethod = (IMethodSymbol)member;
                    return firstMethod;
                }
            }
        }
        return null;
    }

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