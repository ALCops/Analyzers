using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

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
    /// <param name="invocation">The invocation expression to inspect.</param>
    /// <param name="containingSymbol">The symbol whose body contains the invocation; its containing type is used as the record when the call has no explicit instance.</param>
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
        if (tableType is null || !IsPermissionRelevant(tableType, includeSystemTables))
            return null;

        return new RequiredPermission(tableType, recordType, operation, invocation.Syntax.GetLocation());
    }

    /// <summary>
    /// Collects the permissions required by a <c>DataTransfer</c> executor
    /// (<c>CopyFields</c> / <c>CopyRows</c>). Equivalent to the overload taking a
    /// <see cref="CancellationToken"/>, which callers should prefer.
    /// </summary>
    public static bool TryGetFromDataTransfer(
        IInvocationExpression executor,
        SemanticModel semanticModel,
        bool includeSystemTables,
        List<RequiredPermission> results) =>
        TryGetFromDataTransfer(executor, semanticModel, includeSystemTables, results, CancellationToken.None);

    /// <summary>
    /// Collects the permissions required by a <c>DataTransfer</c> executor
    /// (<c>CopyFields</c> / <c>CopyRows</c>).
    /// <para>
    /// The tables are not on the receiver but come from the <c>SetTables(Database::X, Database::Y)</c>
    /// call on the same variable that reaches the executor in flow order, within the same method
    /// or trigger body: a later <c>SetTables</c> replaces an earlier one, so the sequential
    /// "configure, copy, reconfigure, copy" pattern attributes each executor only to its own
    /// pair. When branches configure the variable differently the merge is the union of their
    /// pairs, which keeps the result conservative. See <see cref="DataTransferTableResolver"/>.
    /// </para>
    /// </summary>
    /// <param name="executor">The <c>CopyFields</c>/<c>CopyRows</c> invocation.</param>
    /// <param name="semanticModel">Semantic model for the executor's syntax tree.</param>
    /// <param name="includeSystemTables">See <see cref="TryGetFromInvocation"/>.</param>
    /// <param name="results">Receives the required permissions; only written when this returns true.</param>
    /// <param name="cancellationToken">Cancellation token for the body walk.</param>
    /// <param name="resolver">
    /// A resolver already built for the enclosing body, so callers that inspect several
    /// executors in one body walk it only once. Built on demand when null.
    /// </param>
    /// <returns>
    /// <c>false</c> when the invocation is a <c>DataTransfer</c> executor whose tables cannot be
    /// resolved: no <c>SetTables</c> reaches it on any path, a <c>SetTables</c> that reaches it
    /// names a table with something other than a <c>Database::X</c> literal, or the receiver is
    /// neither a plain identifier nor <c>this.&lt;variable&gt;</c>. Callers must then treat the
    /// access as targeting an unknown table.
    /// <c>true</c> when the tables were resolved, and also when the invocation is not a
    /// <c>DataTransfer</c> executor at all (no results are added in that case).
    /// </returns>
    public static bool TryGetFromDataTransfer(
        IInvocationExpression executor,
        SemanticModel semanticModel,
        bool includeSystemTables,
        List<RequiredPermission> results,
        CancellationToken cancellationToken,
        DataTransferTableResolver? resolver = null)
    {
        if (executor.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod
            || executor.Instance?.Type?.NavTypeKind != EnumProvider.NavTypeKind.DataTransfer
            || !DataTransferOperations.TryGetOperations(executor.TargetMethod.Name, out var sourceOperation, out var destinationOperation))
            return true;

        resolver ??= DataTransferTableResolver.CreateForEnclosingBody(executor, semanticModel, cancellationToken);
        if (resolver is null || !resolver.TryGetTables(executor, out var pairs))
            return false;

        var location = executor.Syntax.GetLocation();
        var staged = new List<RequiredPermission>();

        foreach (var pair in pairs)
        {
            AddDataTransferPermission(pair.Source, sourceOperation, location, includeSystemTables, results, staged);
            AddDataTransferPermission(pair.Destination, destinationOperation, location, includeSystemTables, results, staged);
        }

        results.AddRange(staged);
        return true;
    }

    private static void AddDataTransferPermission(
        ITableTypeSymbol table,
        DatabaseOperation operation,
        Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location,
        bool includeSystemTables,
        List<RequiredPermission> alreadyCollected,
        List<RequiredPermission> staged)
    {
        if (!IsPermissionRelevant(table, includeSystemTables)
            || Contains(alreadyCollected, table, operation)
            || Contains(staged, table, operation))
            return;

        staged.Add(new RequiredPermission(table, table, operation, location));
    }

    private static bool Contains(List<RequiredPermission> permissions, ITableTypeSymbol table, DatabaseOperation operation)
    {
        foreach (var permission in permissions)
        {
            if (permission.Operation == operation && permission.Table.Id == table.Id)
                return true;
        }

        return false;
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

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType || !IsPermissionRelevant(tableType, includeSystemTables))
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
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || !IsPermissionRelevant(tableType, includeSystemTables))
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
        if (targetSymbol is IRecordTypeSymbol { Temporary: true })
            yield break;
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || !IsPermissionRelevant(tableType, includeSystemTables))
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
    /// A table only carries a permission worth reporting when it is a real, non-temporary table.
    /// System tables count only when <paramref name="includeSystemTables"/> is set; see the
    /// parameter of the same name on <see cref="TryGetFromInvocation"/>.
    /// </summary>
    private static bool IsPermissionRelevant(ITableTypeSymbol table, bool includeSystemTables) =>
        !table.IsTemporary() && (includeSystemTables || !IsSystemTable(table));


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
