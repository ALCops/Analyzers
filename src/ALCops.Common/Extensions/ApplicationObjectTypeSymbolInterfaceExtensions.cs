using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class ApplicationObjectTypeSymbolInterfaceExtensions
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

    public static bool MethodImplementsInterfaceMethod(this IApplicationObjectTypeSymbol? objectSymbol, IMethodSymbol methodSymbol)
    {
        if (objectSymbol is not ICodeunitTypeSymbol codeunitSymbol)
            return false;

        foreach (var implementedInterface in codeunitSymbol.ImplementedInterfaces)
        {
            if (implementedInterface.GetMembers()
                                    .OfType<IMethodSymbol>()
                                    .Any(methodSymbol.MethodImplementsInterfaceMethod))
            {
                return true;
            }
        }

        return false;
    }
}