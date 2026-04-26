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
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private void AnalyzeCompilation(CompilationAnalysisContext ctx)
    {
        var compilation = ctx.Compilation;

        foreach (var obj in compilation.GetDeclaredApplicationObjectSymbols())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (obj.Kind == EnumProvider.SymbolKind.PermissionSet
                || obj.Kind == EnumProvider.SymbolKind.PermissionSetExtension)
                continue;

            if (RequiredPermissionDetector.IsTestCodeunitWithPermissionsDisabled(obj))
                continue;

            var permissionsProperty = obj.GetProperty(EnumProvider.PropertyKind.Permissions);
            if (permissionsProperty is null)
                continue;

            var permissionsSyntax = permissionsProperty.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
            if (permissionsSyntax is null)
                continue;

            var declaredEntries = permissionsSyntax.PermissionProperties;
            if (declaredEntries.Count == 0)
                continue;

            var requiredPermissions = CollectRequiredPermissions(obj, compilation, ctx.CancellationToken);
            var pageContext = PermissionResolver.GetPageContext(obj);

            foreach (var entry in declaredEntries)
            {
                if (!entry.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                    continue;

                AnalyzePermissionEntry(entry, requiredPermissions, pageContext, ctx.ReportDiagnostic);
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
            var normalizedDeclared = NormalizePermissionChars(declaredChars);
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
    /// Walks all code in the object and collects required database permissions.
    /// </summary>
    private static List<RequiredPermission> CollectRequiredPermissions(
        IApplicationObjectTypeSymbol obj,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var result = new List<RequiredPermission>();

        CollectFromInvocations(obj, compilation, result, cancellationToken);
        CollectFromReportDataItems(obj, result);
        CollectFromQueryDataItems(obj, result);
        CollectFromXmlPortNodes(obj, result);

        return result;
    }

    private static void CollectFromInvocations(
        IApplicationObjectTypeSymbol obj,
        Compilation compilation,
        List<RequiredPermission> result,
        CancellationToken cancellationToken)
    {
        var syntaxRef = obj.DeclaringSyntaxReference;
        if (syntaxRef is null)
            return;

        var syntaxNode = syntaxRef.GetSyntax();
        var syntaxTree = syntaxNode.SyntaxTree;
        if (syntaxTree is null)
            return;

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        syntaxNode.WalkDescendantsAndPerformAction(node =>
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (!node.IsKind(EnumProvider.SyntaxKind.InvocationExpression))
                return;

            var operation = semanticModel.GetOperation(node, cancellationToken) as IInvocationExpression;
            if (operation is null)
                return;

            var containingSymbol = semanticModel.GetDeclaredSymbol(
                FindContainingMethodOrTrigger(node), cancellationToken);

            var required = RequiredPermissionDetector.TryGetFromInvocation(operation, containingSymbol ?? obj);
            if (required is not null)
                result.Add(required.Value);
        });
    }

    private static SyntaxNode FindContainingMethodOrTrigger(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current.IsKind(EnumProvider.SyntaxKind.MethodDeclaration)
                || current.IsKind(EnumProvider.SyntaxKind.TriggerDeclaration))
                return current;
            current = current.Parent;
        }

        return node;
    }

    private static void CollectFromReportDataItems(
        IApplicationObjectTypeSymbol obj,
        List<RequiredPermission> result)
    {
        if (obj is not IReportTypeSymbol report)
            return;

        var dataItems = new List<ISymbol>();
        CollectReportDataItemsRecursively(report, dataItems);

        foreach (var dataItem in dataItems)
        {
            var required = RequiredPermissionDetector.TryGetFromReportDataItem(dataItem);
            if (required is not null)
                result.Add(required.Value);
        }
    }

    /// <summary>
    /// Recursively collects all report data item symbols, including nested ones.
    /// </summary>
    private static void CollectReportDataItemsRecursively(IContainerSymbol container, List<ISymbol> result)
    {
        foreach (var member in container.GetMembers())
        {
            if (member.Kind == EnumProvider.SymbolKind.ReportDataItem)
            {
                result.Add(member);
                if (member is IContainerSymbol nestedContainer)
                    CollectReportDataItemsRecursively(nestedContainer, result);
            }
        }
    }

    private static void CollectFromQueryDataItems(
        IApplicationObjectTypeSymbol obj,
        List<RequiredPermission> result)
    {
        if (obj is not IQueryTypeSymbol query)
            return;

        foreach (var member in query.GetMembers())
        {
            if (member.Kind != EnumProvider.SymbolKind.QueryDataItem)
                continue;

            var required = RequiredPermissionDetector.TryGetFromQueryDataItem(member);
            if (required is not null)
                result.Add(required.Value);
        }
    }

    private static void CollectFromXmlPortNodes(
        IApplicationObjectTypeSymbol obj,
        List<RequiredPermission> result)
    {
        if (obj is not IXmlPortTypeSymbol xmlPort)
            return;

        foreach (var member in xmlPort.GetMembers())
        {
            if (member.Kind != EnumProvider.SymbolKind.XmlPortNode)
                continue;

            foreach (var required in RequiredPermissionDetector.GetFromXmlPortNode(member))
                result.Add(required);
        }
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

    private static string NormalizePermissionChars(string chars)
    {
        return new string(chars
            .Where(c => "rimdRIMD".Contains(c))
            .Select(c => char.ToLowerInvariant(c))
            .Distinct()
            .OrderBy(c => "rimd".IndexOf(c))
            .ToArray());
    }

    private static string GetUsedChars(string declaredChars, DeclaredPermissionSet required)
    {
        return new string(declaredChars
            .Where(c => "rimdRIMD".Contains(c) && required.HasPermission(MethodOperationMap.FromPermissionChar(c)))
            .Select(c => char.ToLowerInvariant(c))
            .Distinct()
            .OrderBy(c => "rimd".IndexOf(c))
            .ToArray());
    }

    private static string GetUnusedChars(string declaredChars, DeclaredPermissionSet required)
    {
        return new string(declaredChars
            .Where(c => "rimdRIMD".Contains(c) && !required.HasPermission(MethodOperationMap.FromPermissionChar(c)))
            .Select(c => char.ToLowerInvariant(c))
            .Distinct()
            .OrderBy(c => "rimd".IndexOf(c))
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
