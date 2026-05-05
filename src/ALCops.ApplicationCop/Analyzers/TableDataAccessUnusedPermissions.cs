using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public class TableDataAccessUnusedPermissions : DiagnosticAnalyzer
{
    private static readonly SyntaxKind[] ObjectSyntaxKinds =
    [
        EnumProvider.SyntaxKind.CodeunitObject,
        EnumProvider.SyntaxKind.TableObject,
        EnumProvider.SyntaxKind.TableExtensionObject,
        EnumProvider.SyntaxKind.PageObject,
        EnumProvider.SyntaxKind.PageExtensionObject,
        EnumProvider.SyntaxKind.ReportObject,
        EnumProvider.SyntaxKind.ReportExtensionObject,
        EnumProvider.SyntaxKind.QueryObject,
        EnumProvider.SyntaxKind.XmlPortObject
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry,
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeApplicationObject, ObjectSyntaxKinds);
    }

    private static void AnalyzeApplicationObject(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IApplicationObjectTypeSymbol obj)
            return;

        if (obj.Kind == EnumProvider.SymbolKind.PermissionSet
            || obj.Kind == EnumProvider.SymbolKind.PermissionSetExtension)
            return;

        if (RequiredPermissionDetector.IsTestCodeunitWithPermissionsDisabled(obj))
            return;

        var permissionsProperty = obj.GetProperty(EnumProvider.PropertyKind.Permissions);
        if (permissionsProperty is null)
            return;

        var permissionsSyntax = permissionsProperty.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
        if (permissionsSyntax is null)
            return;

        var declaredEntries = permissionsSyntax.PermissionProperties;
        if (declaredEntries.Count == 0)
            return;

        var requiredPermissions = new List<RequiredPermission>();

        CollectFromInvocations(ctx, requiredPermissions);
        CollectFromDataItems(obj, requiredPermissions);

        var pageContext = PermissionResolver.GetPageContext(obj);

        foreach (var entry in declaredEntries)
        {
            if (!entry.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                continue;

            AnalyzePermissionEntry(entry, requiredPermissions, pageContext, ctx.ReportDiagnostic);
        }
    }

    /// <summary>
    /// Walks the object syntax tree for invocation expressions, calling GetOperation
    /// per individual node. If GetOperation fails for one invocation, others still succeed.
    /// Skips invocations inside obsolete methods.
    /// </summary>
    private static void CollectFromInvocations(
        SyntaxNodeAnalysisContext ctx,
        List<RequiredPermission> requiredPermissions)
    {
        // Iterate method/trigger bodies within the object, skip obsolete ones
        foreach (var descendant in ctx.Node.DescendantNodes())
        {
            if (descendant is not MethodOrTriggerDeclarationSyntax method)
                continue;

            var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(method, ctx.CancellationToken);
            if (methodSymbol is null || methodSymbol.IsObsolete())
                continue;

            var body = method.Body;
            if (body is null)
                continue;

            CollectFromMethodBody(ctx, methodSymbol, body, requiredPermissions);
        }
    }

    private static void CollectFromMethodBody(
        SyntaxNodeAnalysisContext ctx,
        ISymbol containingSymbol,
        BlockSyntax body,
        List<RequiredPermission> requiredPermissions)
    {
        foreach (var node in body.DescendantNodes())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (!node.IsKind(EnumProvider.SyntaxKind.InvocationExpression))
                continue;

            var invocationSyntax = (InvocationExpressionSyntax)node;

            // Syntax pre-filter: check method name before expensive GetOperation
            string? methodName = invocationSyntax.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                _ => null
            };

            if (methodName is null || MethodOperationMap.GetOperation(methodName) == DatabaseOperation.None)
                continue;

            var operation = ctx.SemanticModel.GetOperation(invocationSyntax, ctx.CancellationToken);
            if (operation is null || operation.Kind != EnumProvider.OperationKind.InvocationExpression)
                continue;

            var invocationOp = (IInvocationExpression)operation;
            var required = RequiredPermissionDetector.TryGetFromInvocation(
                invocationOp,
                containingSymbol,
                includeSystemTables: true);

            if (required is not null)
                requiredPermissions.Add(required.Value);
        }
    }

    /// <summary>
    /// Collects required permissions from data items (report, query, xmlport members).
    /// </summary>
    private static void CollectFromDataItems(
        IApplicationObjectTypeSymbol obj,
        List<RequiredPermission> requiredPermissions)
    {
        foreach (var member in obj.GetMembers())
        {
            if (member.Kind == EnumProvider.SymbolKind.ReportDataItem)
            {
                var required = RequiredPermissionDetector.TryGetFromReportDataItem(member, includeSystemTables: true);
                if (required is not null)
                    requiredPermissions.Add(required.Value);
            }
            else if (member.Kind == EnumProvider.SymbolKind.QueryDataItem)
            {
                var required = RequiredPermissionDetector.TryGetFromQueryDataItem(member, includeSystemTables: true);
                if (required is not null)
                    requiredPermissions.Add(required.Value);
            }
            else if (member.Kind == EnumProvider.SymbolKind.XmlPortNode)
            {
                foreach (var required in RequiredPermissionDetector.GetFromXmlPortNode(member, includeSystemTables: true))
                    requiredPermissions.Add(required);
            }
        }
    }

    private static void AnalyzePermissionEntry(
        PermissionSyntax entry,
        List<RequiredPermission> requiredPermissions,
        IPageBaseTypeSymbol? pageContext,
        Action<Diagnostic> reportDiagnostic)
    {
        var identifier = entry.ObjectReference.Identifier;

        // Page SourceTable exemption: the page's own source table implicitly needs permissions
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
            // Table not accessed at all
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
            var usedChars = GetUsedChars(declaredChars, matchingOps);
            var properties = BuildProperties(tableName, unusedChars, usedChars);
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars,
                entry.GetLocation(),
                properties,
                unusedChars,
                tableName,
                usedChars));
        }
    }

    /// <summary>
    /// Matches a permission entry's table reference against a table type symbol.
    /// Handles IdentifierNameSyntax, QualifiedNameSyntax, and ObjectIdSyntax.
    /// </summary>
    private static bool PermissionMatchesTable(SyntaxNode identifier, ITableTypeSymbol table)
    {
        if (identifier.Kind == EnumProvider.SyntaxKind.IdentifierName)
        {
            var name = ((IdentifierNameSyntax)identifier).Identifier.ValueText?.UnquoteIdentifier();
            return name is not null && name.Equals(table.Name, StringComparison.OrdinalIgnoreCase);
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
            return qualifier.Equals(tableNamespace, StringComparison.OrdinalIgnoreCase)
                && name.Equals(table.Name, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Gets a display name for the table from a permission entry.
    /// Uses the original text as written in the permission declaration.
    /// </summary>
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

    private static string GetUsedChars(string declaredChars, DeclaredPermissionSet required)
    {
        return new string(declaredChars
            .Where(c => MethodOperationMap.IsValidPermissionChar(c) && required.HasPermission(MethodOperationMap.FromPermissionChar(c)))
            .Select(c => char.ToLowerInvariant(c))
            .Distinct()
            .OrderBy(c => MethodOperationMap.CanonicalOrder.IndexOf(c))
            .ToArray());
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
