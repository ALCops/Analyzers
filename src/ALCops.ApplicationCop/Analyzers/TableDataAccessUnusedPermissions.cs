using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class TableDataAccessUnusedPermissions : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry,
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            AnalyzeApplicationObject,
            EnumProvider.SyntaxKind.CodeunitObject,
            EnumProvider.SyntaxKind.TableObject,
            EnumProvider.SyntaxKind.TableExtensionObject,
            EnumProvider.SyntaxKind.PageObject,
            EnumProvider.SyntaxKind.PageExtensionObject,
            EnumProvider.SyntaxKind.ReportObject,
            EnumProvider.SyntaxKind.ReportExtensionObject,
            EnumProvider.SyntaxKind.QueryObject,
            EnumProvider.SyntaxKind.XmlPortObject);
    }

    private static void AnalyzeApplicationObject(SyntaxNodeAnalysisContext ctx)
    {
        var declaredSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ctx.CancellationToken);
        if (declaredSymbol is not IApplicationObjectTypeSymbol containingObject)
            return;

        if (containingObject.Kind == EnumProvider.SymbolKind.PermissionSet
            || containingObject.Kind == EnumProvider.SymbolKind.PermissionSetExtension
            || containingObject.IsObsolete()
            || containingObject.IsTestCodeunitWithPermissionsDisabled())
            return;

        var permissionsProperty = containingObject.GetProperty(EnumProvider.PropertyKind.Permissions);
        if (permissionsProperty is null)
            return;

        var permissionsSyntax = permissionsProperty.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
        if (permissionsSyntax is null)
            return;

        var declaredEntries = permissionsSyntax.PermissionProperties;
        if (declaredEntries.Count == 0)
            return;

        var requiredPermissions = new List<RequiredPermission>();

        // Whole-object bailout: a DB operation on a RecordRef receiver can target any
        // table at runtime, so unused-permission analysis is unsound for this object.
        if (CollectFromInvocations(ctx, containingObject, requiredPermissions))
            return;

        CollectFromDataItems(containingObject, requiredPermissions);

        var pageContext = PermissionResolver.GetPageContext(containingObject);
        foreach (var entry in declaredEntries)
        {
            if (!entry.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue;
            AnalyzePermissionEntry(entry, requiredPermissions, pageContext, ctx.ReportDiagnostic);
        }
    }

    /// <summary>
    /// Collects required permissions from DB invocations in all method bodies.
    /// Returns true when a DB operation on a RecordRef receiver is found, in which case
    /// the caller must abort analysis of the whole object (the RecordRef's runtime table
    /// is statically unknowable, so any declared permission may be in use).
    /// </summary>
    private static bool CollectFromInvocations(
        SyntaxNodeAnalysisContext ctx,
        IApplicationObjectTypeSymbol containingObject,
        List<RequiredPermission> requiredPermissions)
    {
        // Build object-scope record map (global vars, data items, xmlport table elements)
        // and the set of object-scope RecordRef variable names
        Dictionary<string, IRecordTypeSymbol>? objectScopeRecordMap = null;
        HashSet<string>? objectScopeRecordRefNames = null;
        HashSet<string>? objectScopeDataTransferNames = null;
        foreach (var member in containingObject.GetMembers())
        {
            if (member is not IVariableSymbol globalVar)
                continue;

            if (globalVar.Type is IRecordTypeSymbol globalRecordType
                && !globalRecordType.IsTemporary())
            {
                objectScopeRecordMap ??= new(StringComparer.OrdinalIgnoreCase);
                objectScopeRecordMap.TryAdd(globalVar.Name, globalRecordType);
            }
            else if (globalVar.Type?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
            {
                objectScopeRecordRefNames ??= new(StringComparer.OrdinalIgnoreCase);
                objectScopeRecordRefNames.Add(globalVar.Name);
            }
            else if (globalVar.Type?.NavTypeKind == EnumProvider.NavTypeKind.DataTransfer)
            {
                objectScopeDataTransferNames ??= new(StringComparer.OrdinalIgnoreCase);
                objectScopeDataTransferNames.Add(globalVar.Name);
            }
        }

        // Add all nested data items (report and query, unlimited depth) to the object-scope record map
        foreach (var dataItem in containingObject.GetFlattenedDataItems())
        {
            if (dataItem.GetBooleanPropertyValue(EnumProvider.PropertyKind.UseTemporary) is not true
                && dataItem.GetTypeSymbol() is IRecordTypeSymbol nestedRecordType
                && !nestedRecordType.IsTemporary())
            {
                objectScopeRecordMap ??= new(StringComparer.OrdinalIgnoreCase);
                objectScopeRecordMap.TryAdd(dataItem.Name, nestedRecordType);
            }
        }

        // Add all nested xmlport table elements (unlimited depth) to the object-scope record map
        foreach (var xmlNode in containingObject.GetFlattenedXmlPortNodes())
        {
            AddXmlPortNodeToVarMap(xmlNode, ref objectScopeRecordMap);
        }

        foreach (var node in ctx.Node.DescendantNodes())
        {
            if (node is not MethodOrTriggerDeclarationSyntax methodSyntax)
                continue;

            var body = methodSyntax.Body;
            if (body is null)
                continue;

            if (!HasPossibleDbInvocation(body))
                continue;

            ctx.CancellationToken.ThrowIfCancellationRequested();

            var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(methodSyntax, ctx.CancellationToken) as IMethodSymbol;
            if (methodSymbol is null || methodSymbol.IsObsolete())
                continue;

            // Build per-method record variable map and RecordRef name set from locals + parameters
            Dictionary<string, IRecordTypeSymbol>? localRecordVarMap = null;
            HashSet<string>? localRecordRefNames = null;
            HashSet<string>? localDataTransferNames = null;

            foreach (var local in methodSymbol.LocalVariables)
            {
                if (local.Type is IRecordTypeSymbol recordType && !recordType.IsTemporary())
                {
                    localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordVarMap.TryAdd(local.Name, recordType);
                }
                else if (local.Type?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
                {
                    localRecordRefNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordRefNames.Add(local.Name);
                }
                else if (local.Type?.NavTypeKind == EnumProvider.NavTypeKind.DataTransfer)
                {
                    localDataTransferNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localDataTransferNames.Add(local.Name);
                }
            }

            foreach (var param in methodSymbol.Parameters)
            {
                if (param.ParameterType is IRecordTypeSymbol recordType && !recordType.IsTemporary())
                {
                    localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordVarMap.TryAdd(param.Name, recordType);
                }
                else if (param.ParameterType?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
                {
                    localRecordRefNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordRefNames.Add(param.Name);
                }
                else if (param.ParameterType?.NavTypeKind == EnumProvider.NavTypeKind.DataTransfer)
                {
                    localDataTransferNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localDataTransferNames.Add(param.Name);
                }
            }

            // Named return value acts as an implicit local variable in AL
            if (methodSymbol.ReturnValueSymbol is { IsNamed: true } returnValue)
            {
                if (returnValue.ReturnType is IRecordTypeSymbol returnRecordType
                    && !returnRecordType.IsTemporary())
                {
                    localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordVarMap.TryAdd(returnValue.Name, returnRecordType);
                }
                else if (returnValue.ReturnType?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
                {
                    localRecordRefNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordRefNames.Add(returnValue.Name);
                }
                else if (returnValue.ReturnType?.NavTypeKind == EnumProvider.NavTypeKind.DataTransfer)
                {
                    localDataTransferNames ??= new(StringComparer.OrdinalIgnoreCase);
                    localDataTransferNames.Add(returnValue.Name);
                }
            }

            // One walk per body, built on the first executor, callback-local (no shared state).
            DataTransferTableResolver? dataTransferResolver = null;

            // Walk method body for DB invocations (handles both with and without parentheses)
            foreach (var descendant in body.DescendantNodes())
            {
                if (descendant is InvocationExpressionSyntax
                    || (descendant is MemberAccessExpressionSyntax ma && ma.Parent is not InvocationExpressionSyntax))
                {
                    // Executors resolve through their own path; an unresolvable one triggers
                    // the same whole-object bailout as a RecordRef access.
                    if (descendant.TryGetMethodCall(out var callName, out var callReceiver, out _)
                        && callName is not null
                        && DataTransferOperations.IsExecutor(callName)
                        && IsDataTransferReceiver(
                            callReceiver, localDataTransferNames, objectScopeDataTransferNames,
                            localRecordVarMap, localRecordRefNames, ctx))
                    {
                        dataTransferResolver ??= DataTransferTableResolver.Create(
                            body, ctx.SemanticModel, ctx.CancellationToken);

                        if (dataTransferResolver is null
                            || ctx.SemanticModel.GetOperation(descendant, ctx.CancellationToken) is not IInvocationExpression dataTransferOperation
                            || !RequiredPermissionDetector.TryGetFromDataTransfer(
                                dataTransferOperation, ctx.SemanticModel, includeSystemTables: true, requiredPermissions,
                                ctx.CancellationToken, dataTransferResolver))
                            return true;

                        continue;
                    }

                    var permission = TryGetPermissionFromDbAccess(
                        descendant, containingObject, localRecordVarMap, objectScopeRecordMap,
                        localRecordRefNames, objectScopeRecordRefNames, ctx,
                        out var isRecordRefAccess);

                    if (isRecordRefAccess)
                        return true;

                    if (permission is not null)
                        requiredPermissions.Add(permission.Value);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves a DB access from either syntax form:
    /// - InvocationExpressionSyntax (with parentheses: MyTable.Find())
    /// - MemberAccessExpressionSyntax without parent InvocationExpressionSyntax (no parens: MyTable.Count)
    /// Uses variable-map fast path when possible, falls back to GetSymbolInfo for complex receivers.
    /// Sets <paramref name="isRecordRefAccess"/> when the receiver is a RecordRef, which
    /// requires the caller to abort analysis of the whole object.
    /// </summary>
    private static RequiredPermission? TryGetPermissionFromDbAccess(
        SyntaxNode node,
        IApplicationObjectTypeSymbol containingObject,
        Dictionary<string, IRecordTypeSymbol>? localRecordVarMap,
        Dictionary<string, IRecordTypeSymbol>? objectScopeRecordMap,
        HashSet<string>? localRecordRefNames,
        HashSet<string>? objectScopeRecordRefNames,
        SyntaxNodeAnalysisContext ctx,
        out bool isRecordRefAccess)
    {
        isRecordRefAccess = false;

        if (!node.TryGetMethodCall(out var methodName, out var receiverExpression, out var hasImplicitSelf)
            || methodName is null)
            return null;

        var operation = MethodOperationMap.GetOperation(methodName);
        if (operation == DatabaseOperation.None)
            return null;

        // Implicit self: bare `Method()` inside a table. The accessed table is the
        // containing object itself (an ITableTypeSymbol). The operation model reports a
        // null instance for bare self calls, so resolve directly from the object symbol.
        if (hasImplicitSelf)
            return TryGetPermissionForType(containingObject as ITypeSymbol, operation, node);

        // Fast path: resolve receiver via variable map lookup, honoring AL scoping:
        // locals/parameters shadow object-scope variables, so the full local scope is
        // consulted (RecordRef set + record map) before the object scope.
        if (receiverExpression is IdentifierNameSyntax identifierName)
        {
            var receiverName = identifierName.Identifier.ValueText?.UnquoteIdentifier();
            if (receiverName is not null)
            {
                if (localRecordRefNames is not null && localRecordRefNames.Contains(receiverName))
                {
                    isRecordRefAccess = true;
                    return null;
                }

                IRecordTypeSymbol? recordType = null;

                if (localRecordVarMap is not null)
                    localRecordVarMap.TryGetValue(receiverName, out recordType);

                if (recordType is null
                    && objectScopeRecordRefNames is not null && objectScopeRecordRefNames.Contains(receiverName))
                {
                    isRecordRefAccess = true;
                    return null;
                }

                if (recordType is null && objectScopeRecordMap is not null)
                    objectScopeRecordMap.TryGetValue(receiverName, out recordType);

                if (recordType is not null)
                {
                    var tableType = recordType.OriginalDefinition as ITableTypeSymbol;
                    if (tableType is not null)
                        return new RequiredPermission(tableType, recordType, operation, node.GetLocation());
                    return null;
                }
            }
        }

        // Non-identifier receiver (e.g. `this.Method()`, or an expression receiver such as
        // `GetRec().Method()`). Resolve the receiver's type off the base IOperation. This
        // mirrors AC0031 (RequiredPermissionDetector) and deliberately avoids referencing
        // ThisExpressionSyntax, which is absent from the netstandard2.1 compile floor
        // (AL 12.0.13, predating the Fall 2024 `this` feature). IOperation/GetOperation and
        // IOperation.Type all exist at that floor, so this works on every TFM and AL version:
        // in a table `this` binds to the record; in non-record objects (e.g. a codeunit,
        // where `this` is the codeunit instance) the type is not a record and is ignored.
        if (receiverExpression is not null && receiverExpression is not IdentifierNameSyntax)
        {
            var receiverType = ctx.SemanticModel.GetOperation(receiverExpression, ctx.CancellationToken)?.Type;
            if (receiverType?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
            {
                isRecordRefAccess = true;
                return null;
            }

            var permission = TryGetPermissionForType(receiverType, operation, node);
            if (permission is not null)
                return permission;
        }

        // Fallback: complex receiver or unresolved name (use GetSymbolInfo)
        return TryGetPermissionViaSymbolInfo(node, receiverExpression, containingObject, ctx, out isRecordRefAccess);
    }

    /// <summary>
    /// Determines whether the receiver of a <c>CopyFields</c>/<c>CopyRows</c> call is a
    /// <c>DataTransfer</c> variable. Identifiers resolve through the name sets, honoring AL
    /// scoping: a local record or RecordRef of the same name shadows an object-scope
    /// <c>DataTransfer</c>. Anything else falls back to the receiver's bound type, so a
    /// user-defined <c>CopyRows</c> procedure on a record keeps flowing through the normal path.
    /// </summary>
    private static bool IsDataTransferReceiver(
        ExpressionSyntax? receiverExpression,
        HashSet<string>? localDataTransferNames,
        HashSet<string>? objectScopeDataTransferNames,
        Dictionary<string, IRecordTypeSymbol>? localRecordVarMap,
        HashSet<string>? localRecordRefNames,
        SyntaxNodeAnalysisContext ctx)
    {
        if (receiverExpression is null)
            return false;

        if (receiverExpression is IdentifierNameSyntax identifierName)
        {
            var receiverName = identifierName.Identifier.ValueText?.UnquoteIdentifier();
            if (receiverName is null)
                return false;

            if (localDataTransferNames is not null && localDataTransferNames.Contains(receiverName))
                return true;

            if ((localRecordVarMap is not null && localRecordVarMap.ContainsKey(receiverName))
                || (localRecordRefNames is not null && localRecordRefNames.Contains(receiverName)))
                return false;

            return objectScopeDataTransferNames is not null && objectScopeDataTransferNames.Contains(receiverName);
        }

        return ctx.SemanticModel.GetOperation(receiverExpression, ctx.CancellationToken)?.Type?.NavTypeKind
            == EnumProvider.NavTypeKind.DataTransfer;
    }

    /// <summary>
    /// Builds a required permission from a resolved receiver/self type (bare `Method()`,
    /// `this.Method()`, or an expression receiver). Accepts either a record type (resolved to
    /// its backing table) or a table type directly. Returns null when the type is not a
    /// non-temporary record/table (e.g. inside a codeunit, where `this` is the codeunit instance).
    /// </summary>
    private static RequiredPermission? TryGetPermissionForType(
        ITypeSymbol? selfType,
        DatabaseOperation operation,
        SyntaxNode node)
    {
        switch (selfType)
        {
            case IRecordTypeSymbol record when !record.IsTemporary()
                && record.OriginalDefinition is ITableTypeSymbol recordTable:
                return new RequiredPermission(recordTable, record, operation, node.GetLocation());

            case ITableTypeSymbol table when !table.IsTemporary():
                return new RequiredPermission(table, table, operation, node.GetLocation());

            default:
                return null;
        }
    }

    private static RequiredPermission? TryGetPermissionViaSymbolInfo(
        SyntaxNode node,
        ExpressionSyntax? receiverExpression,
        IApplicationObjectTypeSymbol containingObject,
        SyntaxNodeAnalysisContext ctx,
        out bool isRecordRefAccess)
    {
        isRecordRefAccess = false;

        var symbolInfo = ctx.SemanticModel.GetSymbolInfo(node, ctx.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol targetMethod)
            return null;

        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return null;

        var operation = MethodOperationMap.GetOperation(targetMethod.Name);
        if (operation == DatabaseOperation.None)
            return null;

        IRecordTypeSymbol? recordType = null;

        if (receiverExpression is not null)
        {
            var receiverSymbolInfo = ctx.SemanticModel.GetSymbolInfo(receiverExpression, ctx.CancellationToken);
            ITypeSymbol? receiverType = receiverSymbolInfo.Symbol switch
            {
                IVariableSymbol v => v.Type,
                IParameterSymbol p => p.ParameterType,
                IMethodSymbol m => m.ReturnValueSymbol?.ReturnType,
                _ => null
            };

            if (receiverType?.NavTypeKind == EnumProvider.NavTypeKind.RecordRef)
            {
                isRecordRefAccess = true;
                return null;
            }

            recordType = receiverType as IRecordTypeSymbol;
        }
        else
        {
            recordType = containingObject as IRecordTypeSymbol;
        }

        if (recordType is null || recordType.Temporary)
            return null;

        var tableType = recordType.OriginalDefinition as ITableTypeSymbol;
        if (tableType is null || tableType.IsTemporary())
            return null;

        return new RequiredPermission(tableType, recordType, operation, node.GetLocation());
    }

    private static void CollectFromDataItems(
        IApplicationObjectTypeSymbol containingObject,
        List<RequiredPermission> requiredPermissions)
    {
        // Reports and queries: use GetFlattenedDataItems to include all nested data items
        foreach (var dataItem in containingObject.GetFlattenedDataItems())
        {
            if (dataItem.Kind == EnumProvider.SymbolKind.ReportDataItem)
            {
                var required = RequiredPermissionDetector.TryGetFromReportDataItem(dataItem, includeSystemTables: true);
                if (required is not null)
                    requiredPermissions.Add(required.Value);
            }
            else if (dataItem.Kind == EnumProvider.SymbolKind.QueryDataItem)
            {
                var required = RequiredPermissionDetector.TryGetFromQueryDataItem(dataItem, includeSystemTables: true);
                if (required is not null)
                    requiredPermissions.Add(required.Value);
            }
        }

        // XmlPort nodes: use GetFlattenedXmlPortNodes to include all nested levels
        foreach (var xmlNode in containingObject.GetFlattenedXmlPortNodes())
        {
            foreach (var r in RequiredPermissionDetector.GetFromXmlPortNode(xmlNode, includeSystemTables: true))
                requiredPermissions.Add(r);
        }
    }

    private static void AddXmlPortNodeToVarMap(
        IXmlPortNodeSymbol node,
        ref Dictionary<string, IRecordTypeSymbol>? objectScopeRecordMap)
    {
        if (node.SourceTypeKind == EnumProvider.XmlPortSourceTypeKind.Table
            && ((ISymbol)node).GetTypeSymbol() is IRecordTypeSymbol recordType
            && !recordType.IsTemporary())
        {
            objectScopeRecordMap ??= new(StringComparer.OrdinalIgnoreCase);
            objectScopeRecordMap.TryAdd(((ISymbol)node).Name, recordType);
        }
    }

    /// <summary>
    /// Syntax-level check: does the body contain any invocation with a name that maps to a DB operation?
    /// Checks both InvocationExpressionSyntax (with parens) and standalone MemberAccessExpressionSyntax (without parens).
    /// </summary>
    private static bool HasPossibleDbInvocation(BlockSyntax body)
    {
        foreach (var node in body.DescendantNodes())
        {
            if (node.TryGetMethodCall(out var methodName, out _, out _) && IsPossibleDbMethodName(methodName))
                return true;
        }

        return false;
    }

    private static bool IsPossibleDbMethodName(string? methodName) =>
        methodName is not null
        && (MethodOperationMap.GetOperation(methodName) != DatabaseOperation.None
            || DataTransferOperations.IsExecutor(methodName));

    private static void AnalyzePermissionEntry(
        PermissionSyntax entry,
        List<RequiredPermission> requiredPermissions,
        IPageBaseTypeSymbol? pageContext,
        Action<Diagnostic> reportDiagnostic)
    {
        var identifier = entry.ObjectReference.Identifier;

        // Page SourceTable exemption
        if (pageContext?.RelatedTable is not null
            && PermissionMatchesTable(identifier, pageContext.RelatedTable))
            return;

        // Find all required permissions that match this declared entry
        var matchingOps = new DeclaredPermissionSet();
        bool hasMatch = false;

        foreach (var required in requiredPermissions)
        {
            if (PermissionMatchesTable(identifier, required.Table))
            {
                matchingOps.Grant(required.Operation);
                hasMatch = true;
            }
        }

        var declaredChars = entry.Permissions.ValueText ?? string.Empty;
        var tableName = GetDisplayTableName(entry);

        if (!hasMatch)
        {
            var normalizedDeclared = GetUnusedChars(declaredChars, new DeclaredPermissionSet());
            var properties = BuildProperties(tableName, normalizedDeclared, string.Empty);
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry,
                entry.GetLocation(),
                properties,
                tableName));
            return;
        }

        var unusedChars = GetUnusedChars(declaredChars, matchingOps);
        if (unusedChars.Length > 0)
        {
            var requiredChars = GetRequiredChars(matchingOps);
            var properties = BuildProperties(tableName, unusedChars, requiredChars);
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars,
                entry.GetLocation(),
                properties,
                unusedChars,
                tableName,
                requiredChars));
        }
    }

    private static bool PermissionMatchesTable(SyntaxNode identifier, ITableTypeSymbol table)
    {
        if (identifier.Kind == EnumProvider.SyntaxKind.IdentifierName)
        {
            var name = ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier();
            return name is not null && SemanticFacts.IsSameName(name, table.Name);
        }

        if (identifier.Kind == EnumProvider.SyntaxKind.ObjectId)
        {
            if (int.TryParse(((ObjectIdSyntax)identifier).Value.ValueText, out var objectId))
                return objectId == table.Id;
            return false;
        }

        if (identifier.Kind == EnumProvider.SyntaxKind.QualifiedName)
        {
            var qualified = (QualifiedNameSyntax)identifier;
            var qualifier = qualified.Left.GetText().ToString();
            var name = qualified.Right.Identifier.ValueText?.UnquoteIdentifier();

            if (name is null)
                return false;

            var tableNamespace = table.OriginalDefinition.GetContainingNamespaceQualifiedNameWithReflection();
            return tableNamespace is not null && SemanticFacts.IsSameName(qualifier, tableNamespace)
                && SemanticFacts.IsSameName(name, table.Name);
        }

        return false;
    }

    private static string GetDisplayTableName(PermissionSyntax entry)
    {
        var identifier = entry.ObjectReference.Identifier;

        if (identifier.Kind == EnumProvider.SyntaxKind.IdentifierName)
            return ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier() ?? string.Empty;

        if (identifier.Kind == EnumProvider.SyntaxKind.QualifiedName)
        {
            var qualified = (QualifiedNameSyntax)identifier;
            return qualified.Right.Identifier.ValueText?.UnquoteIdentifier() ?? string.Empty;
        }

        if (identifier.Kind == EnumProvider.SyntaxKind.ObjectId)
            return ((ObjectIdSyntax)identifier).Value.ValueText ?? string.Empty;

        return entry.ObjectReference.GetText().ToString().Trim();
    }

    private static string GetRequiredChars(DeclaredPermissionSet required)
    {
        Span<char> buffer = stackalloc char[4];
        int count = 0;

        foreach (var c in MethodOperationMap.CanonicalOrder)
        {
            if (required.HasPermission(MethodOperationMap.FromPermissionChar(c)))
                buffer[count++] = c;
        }

        return new string(buffer[..count]);
    }

    private static string GetUnusedChars(string declaredChars, DeclaredPermissionSet required)
    {
        return new string(declaredChars
            .Where(c => MethodOperationMap.IsValidPermissionChar(c) && !required.HasPermission(MethodOperationMap.FromPermissionChar(c)))
            .Select(c => char.ToLowerInvariant(c))
            .Distinct()
            .OrderBy(c => MethodOperationMap.CanonicalOrder.IndexOf(c))
            .ToArray());
    }

    private static ImmutableDictionary<string, string> BuildProperties(
        string tableName, string unusedChars, string usedChars)
    {
        return ImmutableDictionary<string, string>.Empty
            .Add("TableName", tableName)
            .Add("UnusedChars", unusedChars)
            .Add("UsedChars", usedChars);
    }
}
