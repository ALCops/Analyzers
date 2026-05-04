using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common.Permissions;

/// <summary>
/// Detects required database permissions from code constructs.
/// Shared between AC0031 (missing permissions) and AC0032 (unused permissions).
/// </summary>
public static class RequiredPermissionDetector
{
    /// <summary>
    /// Determines if an invocation expression requires a database permission.
    /// Returns null if the invocation doesn't require a permission (not a DB method, temporary record, system table, etc.).
    /// </summary>
    /// <param name="includeSystemTables">
    /// When true, system tables (ID &gt; 2,000,000,000) are included in the results.
    /// AC0031 uses false (default) to avoid suggesting permissions on virtual tables, like for example the Integer table
    /// AC0032 uses true so that declared permissions on system tables are not flagged as unused.
    /// </param>
    public static RequiredPermission? TryGetFromInvocation(
        IInvocationExpression invocation,
        ISymbol containingSymbol,
        bool includeSystemTables = false)
    {
        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return null;

        var operation = MethodOperationMap.GetOperation(invocation.TargetMethod.Name);
        if (operation == DatabaseOperation.None)
            return null;

        IRecordTypeSymbol? recordType;

        if (invocation.Instance is not null)
            recordType = invocation.Instance.Type as IRecordTypeSymbol;
        else
            recordType = containingSymbol.ContainingType as IRecordTypeSymbol;

        if (recordType is null || recordType.Temporary)
            return null;

        var tableType = recordType.OriginalDefinition as ITableTypeSymbol;
        if (tableType is null || (!includeSystemTables && IsSystemTable(tableType)))
            return null;

        return new RequiredPermission(tableType, recordType, operation, invocation.Syntax.GetLocation());
    }

    /// <summary>
    /// Determines if a report data item requires a database permission.
    /// Returns null if it doesn't (temporary record, system table, wrong symbol type, etc.).
    /// </summary>
    public static RequiredPermission? TryGetFromReportDataItem(ISymbol symbol, bool includeSystemTables = false)
    {
        if (symbol.GetBooleanPropertyValue(EnumProvider.PropertyKind.UseTemporary) is true)
            return null;

        if (symbol is not IReportDataItemSymbol reportDataItem)
            return null;

        if (reportDataItem.GetTypeSymbol() is not IRecordTypeSymbol recordType)
            return null;

        if (recordType.Temporary)
            return null;

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType || (!includeSystemTables && IsSystemTable(tableType)))
            return null;

        return new RequiredPermission(tableType, recordType, DatabaseOperation.Read, symbol.GetLocation());
    }

    /// <summary>
    /// Determines if a query data item requires a database permission.
    /// Returns null if the underlying table is a system table.
    /// </summary>
    public static RequiredPermission? TryGetFromQueryDataItem(ISymbol symbol, bool includeSystemTables = false)
    {
        var targetSymbol = ((IQueryDataItemSymbol)symbol).GetTypeSymbol();
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || (!includeSystemTables && IsSystemTable(tableType)))
            return null;

        return new RequiredPermission(tableType, targetSymbol, DatabaseOperation.Read, symbol.GetLocation());
    }

    /// <summary>
    /// Gets required permissions for an xmlport table node.
    /// May yield multiple permissions depending on direction and auto-save/replace/update properties.
    /// </summary>
    public static IEnumerable<RequiredPermission> GetFromXmlPortNode(ISymbol symbol, bool includeSystemTables = false)
    {
        var nodeSymbol = (IXmlPortNodeSymbol)symbol.OriginalDefinition;
        if (nodeSymbol.SourceTypeKind != EnumProvider.XmlPortSourceTypeKind.Table)
            yield break;

        var targetSymbol = nodeSymbol.GetTypeSymbol();
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || (!includeSystemTables && IsSystemTable(tableType)))
            yield break;

        var xmlPort = (IXmlPortTypeSymbol)symbol.GetContainingObjectTypeSymbol();
        var direction = ResolveXmlPortDirection(xmlPort);
        var autoReplace = GetXmlPortNodeBoolProperty(symbol, EnumProvider.PropertyKind.AutoReplace) ?? true;
        var autoUpdate = GetXmlPortNodeBoolProperty(symbol, EnumProvider.PropertyKind.AutoUpdate) ?? true;
        var autoSave = GetXmlPortNodeBoolProperty(symbol, EnumProvider.PropertyKind.AutoSave) ?? true;

        var location = symbol.GetLocation();

        if (direction == EnumProvider.DirectionKind.Import || direction == EnumProvider.DirectionKind.Both)
        {
            if (autoReplace || autoUpdate)
                yield return new RequiredPermission(tableType, targetSymbol, DatabaseOperation.Modify, location);
            if (autoSave)
                yield return new RequiredPermission(tableType, targetSymbol, DatabaseOperation.Insert, location);
        }

        if (direction == EnumProvider.DirectionKind.Export || direction == EnumProvider.DirectionKind.Both)
            yield return new RequiredPermission(tableType, targetSymbol, DatabaseOperation.Read, location);
    }

    /// <summary>
    /// Returns true if the table is a system table (ID > 2,000,000,000).
    /// </summary>
    public static bool IsSystemTable(ITableTypeSymbol table) => table.Id > 2000000000;

    /// <summary>
    /// Returns true if the object is a test codeunit with TestPermissions = Disabled.
    /// </summary>
    public static bool IsTestCodeunitWithPermissionsDisabled(IApplicationObjectTypeSymbol? containingObject)
    {
        if (containingObject is not ICodeunitTypeSymbol codeunit)
            return false;

        var subtype = codeunit.GetEnumPropertyValue<CodeunitSubtypeKind>(EnumProvider.PropertyKind.Subtype);
        if (subtype is null || subtype != EnumProvider.CodeunitSubtypeKind.Test)
            return false;

        var testPermissions = codeunit.GetEnumPropertyValue<TestPermissionsKind>(EnumProvider.PropertyKind.TestPermissions);
        return testPermissions is not null && testPermissions == EnumProvider.TestPermissionsKind.Disabled;
    }

    private static DirectionKind ResolveXmlPortDirection(IXmlPortTypeSymbol xmlPort)
    {
        var direction = xmlPort.GetEnumPropertyValue<DirectionKind>(EnumProvider.PropertyKind.Direction);
        return direction ?? EnumProvider.DirectionKind.Both;
    }

    private static bool? GetXmlPortNodeBoolProperty(ISymbol nodeSymbol, PropertyKind propertyKind)
    {
        return (bool?)nodeSymbol.Properties
            .FirstOrDefault(p => p.PropertyKind == propertyKind)?.Value;
    }
}
