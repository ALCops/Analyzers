using System.Collections.Concurrent;
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext compilationCtx)
    {
        var accumulator = new ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>>();

        compilationCtx.RegisterCodeBlockAction(ctx => CollectFromCodeBlock(ctx, accumulator));
        compilationCtx.RegisterSymbolAction(
            ctx => CollectFromReportDataItem(ctx, accumulator),
            EnumProvider.SymbolKind.ReportDataItem);
        compilationCtx.RegisterSymbolAction(
            ctx => CollectFromQueryDataItem(ctx, accumulator),
            EnumProvider.SymbolKind.QueryDataItem);
        compilationCtx.RegisterSymbolAction(
            ctx => CollectFromXmlPortNode(ctx, accumulator),
            EnumProvider.SymbolKind.XmlPortNode);
        compilationCtx.RegisterCompilationEndAction(ctx => AnalyzeCompilation(ctx, accumulator));
    }

    private static string GetObjectKey(IApplicationObjectTypeSymbol obj)
        => string.Concat(obj.Kind.ToString(), ":", obj.Id.ToString());

    private static void AddRequiredPermission(
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator,
        IApplicationObjectTypeSymbol obj,
        RequiredPermission permission)
    {
        var key = GetObjectKey(obj);
        var bag = accumulator.GetOrAdd(key, _ => new ConcurrentBag<RequiredPermission>());
        bag.Add(permission);
    }

    /// <summary>
    /// Collects required database permissions from invocations within a code block.
    /// Uses GetOperation on the full method body (one semantic call per method),
    /// then walks the operation tree in-memory via OperationWalker.
    /// </summary>
    private static void CollectFromCodeBlock(
        CodeBlockAnalysisContext context,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
    {
        if (context.IsObsolete() ||
            context.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var containingObject = context.OwningSymbol?.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        if (containingObject.GetProperty(EnumProvider.PropertyKind.Permissions) is null)
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        // Syntax-level pre-filter: scan for invocations with DB method names
        // before the expensive GetOperation call. Most methods have no DB calls.
        bool hasPossibleDbInvocation = false;
        body.WalkDescendantsAndPerformAction(node =>
        {
            if (hasPossibleDbInvocation)
                return;

            if (!node.IsKind(EnumProvider.SyntaxKind.InvocationExpression))
                return;

            var invocationSyntax = (InvocationExpressionSyntax)node;
            string? methodName = invocationSyntax.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                _ => null
            };

            if (methodName is not null && MethodOperationMap.GetOperation(methodName) != DatabaseOperation.None)
                hasPossibleDbInvocation = true;
        });

        if (!hasPossibleDbInvocation)
            return;

        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
        if (operation is null)
            return;

        var walker = new InvocationCollectorWalker(accumulator, containingObject, context.OwningSymbol!, context.CancellationToken);
        walker.Visit(operation);
    }

    private sealed class InvocationCollectorWalker : OperationWalker
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> _accumulator;
        private readonly IApplicationObjectTypeSymbol _containingObject;
        private readonly ISymbol _containingSymbol;
        private readonly CancellationToken _cancellationToken;

        public InvocationCollectorWalker(
            ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator,
            IApplicationObjectTypeSymbol containingObject,
            ISymbol containingSymbol,
            CancellationToken cancellationToken)
        {
            _accumulator = accumulator;
            _containingObject = containingObject;
            _containingSymbol = containingSymbol;
            _cancellationToken = cancellationToken;
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var required = RequiredPermissionDetector.TryGetFromInvocation(operation, _containingSymbol);
            if (required is not null)
                AddRequiredPermission(_accumulator, _containingObject, required.Value);

            base.VisitInvocationExpression(operation);
        }
    }

    private static void CollectFromReportDataItem(
        SymbolAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
    {
        var required = RequiredPermissionDetector.TryGetFromReportDataItem(ctx.Symbol);
        if (required is null)
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        AddRequiredPermission(accumulator, containingObject, required.Value);
    }

    private static void CollectFromQueryDataItem(
        SymbolAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
    {
        var required = RequiredPermissionDetector.TryGetFromQueryDataItem(ctx.Symbol);
        if (required is null)
            return;

        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        AddRequiredPermission(accumulator, containingObject, required.Value);
    }

    private static void CollectFromXmlPortNode(
        SymbolAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
    {
        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        foreach (var required in RequiredPermissionDetector.GetFromXmlPortNode(ctx.Symbol))
            AddRequiredPermission(accumulator, containingObject, required);
    }

    private static void AnalyzeCompilation(
        CompilationAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
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

            var requiredPermissions = accumulator.TryGetValue(GetObjectKey(obj), out var bag)
                ? bag.ToList()
                : new List<RequiredPermission>();
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
