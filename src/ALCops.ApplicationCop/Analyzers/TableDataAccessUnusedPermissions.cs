using System.Collections.Concurrent;
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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry,
            DiagnosticDescriptors.TableDataAccessUnusedPermissionsPartialChars);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext compilationCtx)
    {
        var accumulator = new ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>>();
        var objectsWithPermissions = new ConcurrentDictionary<string, bool>();

        compilationCtx.RegisterCodeBlockAction(ctx => CollectFromCodeBlock(ctx, accumulator, objectsWithPermissions));

        compilationCtx.RegisterSymbolAction(
            ctx => CollectFromDataItem(ctx, accumulator, objectsWithPermissions),
            EnumProvider.SymbolKind.ReportDataItem,
            EnumProvider.SymbolKind.QueryDataItem,
            EnumProvider.SymbolKind.XmlPortNode);

        compilationCtx.RegisterCompilationEndAction(ctx => AnalyzeCompilation(ctx, accumulator));
    }

    private static string GetObjectKey(IApplicationObjectTypeSymbol obj)
        => string.Concat(obj.Kind.ToString(), ":", obj.Id.ToString());

    private static void CollectFromCodeBlock(
        CodeBlockAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator,
        ConcurrentDictionary<string, bool> objectsWithPermissions)
    {
        if (ctx.IsObsolete() ||
            ctx.CodeBlock is not MethodOrTriggerDeclarationSyntax methodOrTrigger)
            return;

        var containingObject = ctx.OwningSymbol?.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        // Fast skip: only process objects that have a Permissions property.
        // Cache the lookup to avoid repeated property access for the same object.
        var key = GetObjectKey(containingObject);
        if (!objectsWithPermissions.GetOrAdd(key, _ =>
            containingObject.GetProperty(EnumProvider.PropertyKind.Permissions) is not null))
            return;

        var body = methodOrTrigger.Body;
        if (body is null)
            return;

        // Syntax-level pre-filter: skip methods with no potential DB invocations
        if (!HasPossibleDbInvocation(body))
            return;

        var operation = ctx.SemanticModel.GetOperation(body, ctx.CancellationToken);
        if (operation is null)
            return;

        var walker = new InvocationCollectorWalker(accumulator, containingObject, key, ctx.CancellationToken);
        walker.Visit(operation);
    }

    private static void CollectFromDataItem(
        SymbolAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator,
        ConcurrentDictionary<string, bool> objectsWithPermissions)
    {
        var containingObject = ctx.Symbol.GetContainingApplicationObjectTypeSymbol();
        if (containingObject is null)
            return;

        var key = GetObjectKey(containingObject);
        if (!objectsWithPermissions.GetOrAdd(key, _ =>
            containingObject.GetProperty(EnumProvider.PropertyKind.Permissions) is not null))
            return;

        RequiredPermission? required = null;

        if (ctx.Symbol.Kind == EnumProvider.SymbolKind.ReportDataItem)
            required = RequiredPermissionDetector.TryGetFromReportDataItem(ctx.Symbol, includeSystemTables: true);
        else if (ctx.Symbol.Kind == EnumProvider.SymbolKind.QueryDataItem)
            required = RequiredPermissionDetector.TryGetFromQueryDataItem(ctx.Symbol, includeSystemTables: true);
        else if (ctx.Symbol.Kind == EnumProvider.SymbolKind.XmlPortNode)
        {
            var bag = accumulator.GetOrAdd(key, _ => new ConcurrentBag<RequiredPermission>());
            foreach (var r in RequiredPermissionDetector.GetFromXmlPortNode(ctx.Symbol, includeSystemTables: true))
                bag.Add(r);
            return;
        }

        if (required is not null)
        {
            var bag = accumulator.GetOrAdd(key, _ => new ConcurrentBag<RequiredPermission>());
            bag.Add(required.Value);
        }
    }

    private static void AnalyzeCompilation(
        CompilationAnalysisContext ctx,
        ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator)
    {
        foreach (var symbol in ctx.Compilation.GetDeclaredApplicationObjectSymbols())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (symbol.Kind == EnumProvider.SymbolKind.PermissionSet
                || symbol.Kind == EnumProvider.SymbolKind.PermissionSetExtension)
                continue;

            if (RequiredPermissionDetector.IsTestCodeunitWithPermissionsDisabled(symbol))
                continue;

            var permissionsProperty = symbol.GetProperty(EnumProvider.PropertyKind.Permissions);
            if (permissionsProperty is null)
                continue;

            var permissionsSyntax = permissionsProperty.GetPropertyValueSyntax<PermissionPropertyValueSyntax>();
            if (permissionsSyntax is null)
                continue;

            var declaredEntries = permissionsSyntax.PermissionProperties;
            if (declaredEntries.Count == 0)
                continue;

            var key = GetObjectKey(symbol);
            accumulator.TryGetValue(key, out var collectedBag);
            var requiredPermissions = collectedBag?.ToList() ?? [];

            var pageContext = PermissionResolver.GetPageContext(symbol);

            foreach (var entry in declaredEntries)
            {
                if (!entry.ObjectType.IsKind(EnumProvider.SyntaxKind.TableDataKeyword))
                    continue;

                AnalyzePermissionEntry(entry, requiredPermissions, pageContext, ctx.ReportDiagnostic);
            }
        }
    }

    /// <summary>
    /// Syntax-level check: does the body contain any invocation with a name that maps to a DB operation?
    /// </summary>
    private static bool HasPossibleDbInvocation(BlockSyntax body)
    {
        foreach (var node in body.DescendantNodes())
        {
            if (!node.IsKind(EnumProvider.SyntaxKind.InvocationExpression))
                continue;

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

        return false;
    }

    private sealed class InvocationCollectorWalker : OperationWalker
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> _accumulator;
        private readonly IApplicationObjectTypeSymbol _containingObject;
        private readonly string _key;
        private readonly CancellationToken _cancellationToken;

        public InvocationCollectorWalker(
            ConcurrentDictionary<string, ConcurrentBag<RequiredPermission>> accumulator,
            IApplicationObjectTypeSymbol containingObject,
            string key,
            CancellationToken cancellationToken)
        {
            _accumulator = accumulator;
            _containingObject = containingObject;
            _key = key;
            _cancellationToken = cancellationToken;
        }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var required = RequiredPermissionDetector.TryGetFromInvocation(operation, _containingObject, includeSystemTables: true);
            if (required is not null)
            {
                var bag = _accumulator.GetOrAdd(_key, _ => new ConcurrentBag<RequiredPermission>());
                bag.Add(required.Value);
            }

            base.VisitInvocationExpression(operation);
        }
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
