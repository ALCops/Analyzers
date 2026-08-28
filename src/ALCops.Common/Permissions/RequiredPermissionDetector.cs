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
        if (tableType is null || tableType.IsTemporary() || (!includeSystemTables && IsSystemTable(tableType)))
            return null;

        return new RequiredPermission(tableType, recordType, operation, invocation.Syntax.GetLocation());
    }

    /// <summary>
    /// Collects the permissions required by a <c>DataTransfer</c> executor
    /// (<c>CopyFields</c> / <c>CopyRows</c>).
    /// <para>
    /// The tables are not on the receiver but come from the <c>SetTables(Database::X, Database::Y)</c>
    /// calls on the same variable, within the same method or trigger body. Every resolvable
    /// <c>SetTables</c> pair in that body is attributed to the executor (union, no order or branch
    /// analysis): AL allows a transfer to be reconfigured before each execution, and picking one
    /// pair would be a guess.
    /// </para>
    /// </summary>
    /// <param name="executor">The <c>CopyFields</c>/<c>CopyRows</c> invocation.</param>
    /// <param name="semanticModel">Semantic model for the executor's syntax tree.</param>
    /// <param name="includeSystemTables">See <see cref="TryGetFromInvocation"/>.</param>
    /// <param name="results">Receives the required permissions; only written when this returns true.</param>
    /// <returns>
    /// <c>false</c> when the invocation is a <c>DataTransfer</c> executor whose tables cannot be
    /// resolved (no <c>SetTables</c> in the body, a non-literal table argument, or a receiver that
    /// is not a plain identifier). Callers must then treat the access as targeting an unknown table.
    /// <c>true</c> when the tables were resolved, and also when the invocation is not a
    /// <c>DataTransfer</c> executor at all (no results are added in that case).
    /// </returns>
    public static bool TryGetFromDataTransfer(
        IInvocationExpression executor,
        SemanticModel semanticModel,
        bool includeSystemTables,
        List<RequiredPermission> results)
    {
        if (executor.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod
            || executor.Instance?.Type?.NavTypeKind != EnumProvider.NavTypeKind.DataTransfer
            || !DataTransferOperations.TryGetOperations(executor.TargetMethod.Name, out var sourceOperation, out var destinationOperation))
            return true;

        var receiverName = GetReceiverIdentifierName(executor.Syntax);
        if (receiverName is null)
            return false;

        var body = executor.Syntax.FirstAncestorOrSelf<MethodOrTriggerDeclarationSyntax>()?.Body;
        if (body is null)
            return false;

        var location = executor.Syntax.GetLocation();
        bool resolvedAny = false;

        foreach (var node in body.DescendantNodes())
        {
            if (node is not InvocationExpressionSyntax invocation
                || invocation.Expression is not MemberAccessExpressionSyntax memberAccess
                || !SemanticFacts.IsSameName(memberAccess.Name.Identifier.ValueText ?? string.Empty, DataTransferOperations.SetTablesMethodName))
                continue;

            if (memberAccess.Expression is not IdentifierNameSyntax setTablesReceiver
                || setTablesReceiver.Identifier.ValueText?.UnquoteIdentifier() is not { } setTablesReceiverName
                || !string.Equals(setTablesReceiverName, receiverName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (semanticModel.GetOperation(invocation) is not IInvocationExpression setTablesOperation
                || setTablesOperation.Arguments.Length != 2)
                return false;

            var sourceTable = ResolveTableArgument(setTablesOperation.Arguments[0].Value);
            var destinationTable = ResolveTableArgument(setTablesOperation.Arguments[1].Value);
            if (sourceTable is null || destinationTable is null)
                return false;

            resolvedAny = true;
            AddDataTransferPermission(sourceTable, sourceOperation, location, includeSystemTables, results);
            AddDataTransferPermission(destinationTable, destinationOperation, location, includeSystemTables, results);
        }

        return resolvedAny;
    }

    /// <summary>
    /// Extracts the receiver of a member-access call from either syntax form: with parentheses
    /// (<c>dt.CopyFields()</c>) or without (<c>dt.CopyFields;</c>). Returns null when the receiver
    /// is not a plain identifier, which puts the DataTransfer variable out of reach of the
    /// same-body <c>SetTables</c> lookup.
    /// </summary>
    private static string? GetReceiverIdentifierName(SyntaxNode syntax)
    {
        var memberAccess = syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression as MemberAccessExpressionSyntax,
            MemberAccessExpressionSyntax bare => bare,
            _ => null
        };

        if (memberAccess?.Expression is not IdentifierNameSyntax identifier)
            return null;

        return identifier.Identifier.ValueText?.UnquoteIdentifier();
    }

    /// <summary>
    /// Resolves a <c>SetTables</c> argument to the table it names. Only object-access literals
    /// (<c>Database::"My Table"</c>) resolve; integer variables and expressions do not.
    /// </summary>
    private static ITableTypeSymbol? ResolveTableArgument(IOperation argument)
    {
        while (argument is IConversionExpression conversion)
            argument = conversion.Operand;

        return argument.GetSymbolSafe() as ITableTypeSymbol;
    }

    private static void AddDataTransferPermission(
        ITableTypeSymbol table,
        DatabaseOperation operation,
        Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location,
        bool includeSystemTables,
        List<RequiredPermission> results)
    {
        if (table.IsTemporary() || (!includeSystemTables && IsSystemTable(table)))
            return;

        foreach (var existing in results)
        {
            if (existing.Operation == operation && existing.Table.Id == table.Id)
                return;
        }

        results.Add(new RequiredPermission(table, table, operation, location));
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

        if (recordType.OriginalDefinition is not ITableTypeSymbol tableType || tableType.IsTemporary() || (!includeSystemTables && IsSystemTable(tableType)))
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
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || tableType.IsTemporary() || (!includeSystemTables && IsSystemTable(tableType)))
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
        if (targetSymbol.OriginalDefinition is not ITableTypeSymbol tableType || tableType.IsTemporary() || (!includeSystemTables && IsSystemTable(tableType)))
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
