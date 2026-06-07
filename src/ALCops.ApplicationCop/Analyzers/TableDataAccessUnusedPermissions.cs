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
public class TableDataAccessUnusedPermissions : DiagnosticAnalyzer
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
            || RequiredPermissionDetector.IsTestCodeunitWithPermissionsDisabled(containingObject))
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

        CollectFromInvocations(ctx, containingObject, requiredPermissions);
        CollectFromDataItems(containingObject, requiredPermissions);

        var pageContext = PermissionResolver.GetPageContext(containingObject);
        foreach (var entry in declaredEntries)
        {
            if (!entry.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue;
            AnalyzePermissionEntry(entry, requiredPermissions, pageContext, ctx.ReportDiagnostic);
        }
    }

    private static void CollectFromInvocations(
        SyntaxNodeAnalysisContext ctx,
        IApplicationObjectTypeSymbol containingObject,
        List<RequiredPermission> requiredPermissions)
    {
        // Build object-scope record map (global vars, data items, xmlport table elements)
        Dictionary<string, IRecordTypeSymbol>? objectScopeRecordMap = null;
        foreach (var member in containingObject.GetMembers())
        {
            if (member is IVariableSymbol globalVar
                && globalVar.Type is IRecordTypeSymbol globalRecordType
                && !globalRecordType.Temporary)
            {
                objectScopeRecordMap ??= new(StringComparer.OrdinalIgnoreCase);
                objectScopeRecordMap.TryAdd(globalVar.Name, globalRecordType);
            }
        }

        // Add all nested data items (report and query, unlimited depth) to the object-scope record map
        foreach (var dataItem in containingObject.GetFlattenedDataItems())
        {
            if (dataItem.GetBooleanPropertyValue(EnumProvider.PropertyKind.UseTemporary) is not true
                && dataItem.GetTypeSymbol() is IRecordTypeSymbol nestedRecordType
                && !nestedRecordType.Temporary)
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

            // Build per-method record variable map from locals + parameters
            Dictionary<string, IRecordTypeSymbol>? localRecordVarMap = null;

            foreach (var local in methodSymbol.LocalVariables)
            {
                if (local.Type is IRecordTypeSymbol recordType && !recordType.Temporary)
                {
                    localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordVarMap.TryAdd(local.Name, recordType);
                }
            }

            foreach (var param in methodSymbol.Parameters)
            {
                if (param.ParameterType is IRecordTypeSymbol recordType && !recordType.Temporary)
                {
                    localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                    localRecordVarMap.TryAdd(param.Name, recordType);
                }
            }

            // Named return value acts as an implicit local variable in AL
            if (methodSymbol.ReturnValueSymbol is { IsNamed: true } returnValue
                && returnValue.ReturnType is IRecordTypeSymbol returnRecordType
                && !returnRecordType.Temporary)
            {
                localRecordVarMap ??= new(StringComparer.OrdinalIgnoreCase);
                localRecordVarMap.TryAdd(returnValue.Name, returnRecordType);
            }

            // Walk method body for DB invocations (handles both with and without parentheses)
            foreach (var descendant in body.DescendantNodes())
            {
                if (descendant is InvocationExpressionSyntax
                    || (descendant is MemberAccessExpressionSyntax ma && ma.Parent is not InvocationExpressionSyntax))
                {
                    var permission = TryGetPermissionFromDbAccess(
                        descendant, containingObject, localRecordVarMap, objectScopeRecordMap, ctx);

                    if (permission is not null)
                        requiredPermissions.Add(permission.Value);
                }
            }
        }
    }

    /// <summary>
    /// Resolves a DB access from either syntax form:
    /// - InvocationExpressionSyntax (with parentheses: MyTable.Find())
    /// - MemberAccessExpressionSyntax without parent InvocationExpressionSyntax (no parens: MyTable.Count)
    /// Uses variable-map fast path when possible, falls back to GetSymbolInfo for complex receivers.
    /// </summary>
    private static RequiredPermission? TryGetPermissionFromDbAccess(
        SyntaxNode node,
        IApplicationObjectTypeSymbol containingObject,
        Dictionary<string, IRecordTypeSymbol>? localRecordVarMap,
        Dictionary<string, IRecordTypeSymbol>? objectScopeRecordMap,
        SyntaxNodeAnalysisContext ctx)
    {
        // Extract method name, receiver expression, and location from either syntax form
        string? methodName;
        ExpressionSyntax? receiverExpression;
        bool hasImplicitSelf = false;

        if (node is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax invMemberAccess)
            {
                methodName = invMemberAccess.Name.Identifier.ValueText;
                receiverExpression = invMemberAccess.Expression;
            }
            else if (invocation.Expression is IdentifierNameSyntax simpleName)
            {
                methodName = simpleName.Identifier.ValueText;
                receiverExpression = null;
                hasImplicitSelf = true;
            }
            else
            {
                return null;
            }
        }
        else if (node is MemberAccessExpressionSyntax memberAccess)
        {
            methodName = memberAccess.Name.Identifier.ValueText;
            receiverExpression = memberAccess.Expression;
        }
        else
        {
            return null;
        }

        if (methodName is null)
            return null;

        var operation = MethodOperationMap.GetOperation(methodName);
        if (operation == DatabaseOperation.None)
            return null;

        // Implicit self (no receiver, inside table/tableext)
        if (hasImplicitSelf)
        {
            if (containingObject is IRecordTypeSymbol selfRecord && !selfRecord.Temporary)
            {
                var tableType = selfRecord.OriginalDefinition as ITableTypeSymbol;
                if (tableType is not null)
                    return new RequiredPermission(tableType, selfRecord, operation, node.GetLocation());
            }
            return null;
        }

        // Fast path: resolve receiver via variable map lookup
        if (receiverExpression is IdentifierNameSyntax identifierName)
        {
            var receiverName = identifierName.Identifier.ValueText?.UnquoteIdentifier();
            if (receiverName is not null)
            {
                IRecordTypeSymbol? recordType = null;

                if (localRecordVarMap is not null)
                    localRecordVarMap.TryGetValue(receiverName, out recordType);

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

        // Fallback: complex receiver or unresolved name (use GetSymbolInfo)
        return TryGetPermissionViaSymbolInfo(node, receiverExpression, containingObject, ctx);
    }

    private static RequiredPermission? TryGetPermissionViaSymbolInfo(
        SyntaxNode node,
        ExpressionSyntax? receiverExpression,
        IApplicationObjectTypeSymbol containingObject,
        SyntaxNodeAnalysisContext ctx)
    {
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
            recordType = receiverType as IRecordTypeSymbol;
        }
        else
        {
            recordType = containingObject as IRecordTypeSymbol;
        }

        if (recordType is null || recordType.Temporary)
            return null;

        var tableType = recordType.OriginalDefinition as ITableTypeSymbol;
        if (tableType is null)
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
            && !recordType.Temporary)
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
            if (node.IsKind(EnumProvider.SyntaxKind.InvocationExpression))
            {
                var invocationSyntax = (InvocationExpressionSyntax)node;
                string? methodName = invocationSyntax.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                    _ => null
                };

                if (methodName is not null && MethodOperationMap.GetOperation(methodName) != DatabaseOperation.None)
                    return true;
            }
            else if (node is MemberAccessExpressionSyntax memberAccessNode
                && memberAccessNode.Parent is not InvocationExpressionSyntax)
            {
                string? methodName = memberAccessNode.Name.Identifier.ValueText;
                if (methodName is not null && MethodOperationMap.GetOperation(methodName) != DatabaseOperation.None)
                    return true;
            }
        }

        return false;
    }

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
