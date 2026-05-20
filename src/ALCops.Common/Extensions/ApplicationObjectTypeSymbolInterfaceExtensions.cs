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

    /// <summary>
    /// Gets the flattened list of all data items (including nested) for report and query objects.
    /// For reports on net8.0+, uses the public <see cref="IReportTypeSymbol.FlattenedDataItems"/> property.
    /// For queries (and reports on netstandard2.1), uses reflection to access the internal FlattenedDataItems.
    /// Returns an empty enumerable for non-report/query objects or if the property is unavailable.
    /// </summary>
    public static IEnumerable<ISymbol> GetFlattenedDataItems(this IApplicationObjectTypeSymbol containingObject)
    {
#if !NETSTANDARD2_1
        if (containingObject is IReportTypeSymbol reportType)
        {
            foreach (var dataItem in reportType.FlattenedDataItems)
                yield return (ISymbol)dataItem;
            yield break;
        }
#endif

        // Queries (all TFMs) and reports (netstandard2.1): FlattenedDataItems accessed via reflection
        var flattenedItems = ((object)containingObject).GetPropertyIfExists<System.Collections.IEnumerable>("FlattenedDataItems");
        if (flattenedItems is null)
            yield break;

        foreach (var item in flattenedItems)
        {
            if (item is ISymbol symbol)
                yield return symbol;
        }
    }
}