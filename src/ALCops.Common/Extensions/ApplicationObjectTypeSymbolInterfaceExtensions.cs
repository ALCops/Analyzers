using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

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

        // ImplementedInterfaces is the declared list; methods inherited through 'extends' live on the
        // extended interfaces.
        foreach (var implementedInterface in codeunitSymbol.ImplementedInterfaces)
        {
            if (DeclaresMethodImplementedBy(implementedInterface, methodSymbol))
                return true;

            foreach (var extendedInterface in implementedInterface.GetAllExtendedInterfacesWithReflection())
            {
                if (DeclaresMethodImplementedBy(extendedInterface, methodSymbol))
                    return true;
            }
        }

        return false;
    }

    private static bool DeclaresMethodImplementedBy(IInterfaceTypeSymbol interfaceSymbol, IMethodSymbol methodSymbol) =>
        interfaceSymbol.GetMembers()
                       .OfType<IMethodSymbol>()
                       .Any(methodSymbol.MethodImplementsInterfaceMethod);

    /// <summary>
    /// Returns true if the object is a test codeunit with TestPermissions = Disabled.
    /// </summary>
    public static bool IsTestCodeunitWithPermissionsDisabled(this IApplicationObjectTypeSymbol? containingObject)
    {
        if (!containingObject.IsTestCodeunit())
        {
            return false;
        }

        var testPermissions = (containingObject as ICodeunitTypeSymbol)?.GetEnumPropertyValue<TestPermissionsKind>(EnumProvider.PropertyKind.TestPermissions);

        return testPermissions is not null && testPermissions == EnumProvider.TestPermissionsKind.Disabled;
    }

    /// <summary>
    /// Returns true if the object is a test codeunit.
    /// </summary>
	public static bool IsTestCodeunit(this IApplicationObjectTypeSymbol? objectSymbol)
    {
        if (objectSymbol is not ICodeunitTypeSymbol codeunit)
        {
            return false;
        }

        var subtype = codeunit.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);

        return (subtype is not null) && (subtype == EnumProvider.CodeunitSubtypeKind.Test);
    }

    /// <summary>
    /// Returns true if the object is a codeunit with accessability internal.
    /// </summary>
    public static bool IsInternalCodeunit(this IApplicationObjectTypeSymbol applicationObject) =>
        applicationObject.Kind == EnumProvider.SymbolKind.Codeunit &&
        applicationObject.DeclaredAccessibility == EnumProvider.Accessibility.Internal;

    /// <summary>
    /// Gets the flattened list of all xmlport nodes (including deeply nested) for xmlport objects.
    /// Uses reflection to access the internal <c>SourceXmlPortTypeSymbol.FlattenedNodes</c> property,
    /// which recursively includes all nested tableelement/textelement/fieldelement nodes at any depth.
    /// Returns an empty enumerable for non-xmlport objects or if the property is unavailable.
    /// </summary>
    public static IEnumerable<IXmlPortNodeSymbol> GetFlattenedXmlPortNodes(this IApplicationObjectTypeSymbol containingObject)
    {
        var flattenedNodes = ((object)containingObject).GetPropertyIfExists<System.Collections.IEnumerable>("FlattenedNodes");
        if (flattenedNodes is null)
            yield break;

        foreach (var item in flattenedNodes)
        {
            if (item is IXmlPortNodeSymbol node)
                yield return node;
        }
    }

    /// <summary>
    /// Gets the flattened list of all data items (including nested) for report and query objects.
    /// For reports on net8.0+, uses the public <c>IReportTypeSymbol.FlattenedDataItems</c> property (not present in the netstandard2.1 SDK).
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