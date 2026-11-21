using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Permissions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(TableDataAccessRequiresPermissionsCodeFixProvider))]
public sealed class TableDataAccessRequiresPermissionsCodeFixProvider : CodeFixProvider
{
#if NETSTANDARD2_1
    // C# 9 records require 'System.Runtime.CompilerServices.IsExternalInit' which doesn't exist in netstandard2.1.
    // We use a regular class for netstandard2.1 and a record for .NET 8+ to maintain compatibility with both targets.
    private sealed class CodeFixProperties
    {
        public string TableName { get; }
        public string TableNamespace { get; }
        public char PermissionChar { get; }

        private CodeFixProperties(string tableName, string tableNamespace, char permissionChar)
        {
            TableName = tableName;
            TableNamespace = tableNamespace;
            PermissionChar = permissionChar;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(PermissionChar), out var charStr) || string.IsNullOrEmpty(charStr))
                return null;

            properties.TryGetValue(nameof(TableNamespace), out var tableNamespace);

            return new CodeFixProperties(tableName, tableNamespace ?? string.Empty, charStr[0]);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string TableName, string TableNamespace, char PermissionChar)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(PermissionChar), out var charStr) || string.IsNullOrEmpty(charStr))
                return null;

            properties.TryGetValue(nameof(TableNamespace), out var tableNamespace);

            return new CodeFixProperties(tableName, tableNamespace ?? string.Empty, charStr[0]);
        }
    }
#endif

    private sealed class TableDataAccessRequiresPermissionsCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }

        public TableDataAccessRequiresPermissionsCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    private sealed class TableDataAccessRequiresPermissionsObjectCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll => false;

        public TableDataAccessRequiresPermissionsObjectCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
            : base(title, createChangedDocument, equivalenceKey)
        {
        }
    }

    /// <summary>
    /// The permission chars one table needs, merged in canonical order so a table reported
    /// several times (one diagnostic per char) is edited and named once.
    /// </summary>
    private sealed class TablePermissionFix
    {
        public ResolvedTableName Resolved { get; }
        public string RawTableName { get; }
        public string TableNamespace { get; }
        public string PermissionChars { get; set; } = string.Empty;

        public TablePermissionFix(ResolvedTableName resolved, string rawTableName, string tableNamespace)
        {
            Resolved = resolved;
            RawTableName = rawTableName;
            TableNamespace = tableNamespace;
        }

        public string DisplayName => Resolved.QualifyingNamespace is not null
            ? $"{Resolved.QualifyingNamespace}.{Resolved.TableName}"
            : Resolved.TableName;
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.TableDataAccessRequiresPermissions.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;
        TextSpan span = context.Span;
        CancellationToken cancellationToken = context.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        RegisterInstanceCodeFix(context, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot,
        TextSpan span, Document document)
    {
        var node = syntaxRoot.FindNode(span);
        var objectSyntax = node.FirstAncestorOrSelf<ObjectSyntax>();
        if (objectSyntax is null)
            return;

        // Skip extension objects
        if (objectSyntax is ApplicationObjectExtensionSyntax)
            return;

        // Capture the object's identity so we can re-find it in a (potentially modified) document.
        // This is critical for FixAll: BatchFixer applies fixes sequentially, and each fix may
        // modify the tree. Using a stale objectSyntax reference causes ReplaceNode to fail silently.
        var objectIdentity = GetObjectIdentity(objectSyntax);
        var compilationUnit = objectSyntax.FirstAncestorOrSelf<CompilationUnitSyntax>();

        // The host hands every fixable diagnostic on one span to a single context: a DataTransfer
        // executor reports one per table, DeleteAll and ModifyAll one per char. One action fixes them all.
        var fixes = CollectFixes(ctx.Diagnostics, compilationUnit);
        if (fixes.Count == 0)
            return;

        ctx.RegisterCodeFix(
            new TableDataAccessRequiresPermissionsCodeAction(
                GetTitle(fixes),
                ct => ApplyFixes(document, objectIdentity, fixes, ct),
                nameof(TableDataAccessRequiresPermissionsCodeFixProvider),
                generateFixAll: true),
            ctx.Diagnostics);

        if (objectIdentity.Name is null)
            return;

        ctx.RegisterCodeFix(
            new TableDataAccessRequiresPermissionsObjectCodeAction(
                string.Format(ApplicationCopAnalyzers.TableDataAccessRequiresPermissionsObjectCodeAction, objectIdentity.Name),
                ct => ApplyObjectWideFix(document, objectIdentity, ct),
                nameof(TableDataAccessRequiresPermissionsCodeFixProvider) + "ObjectWide"),
            ctx.Diagnostics);
    }

    private static List<TablePermissionFix> CollectFixes(IEnumerable<Diagnostic> diagnostics,
        CompilationUnitSyntax? compilationUnit)
    {
        var fixes = new List<TablePermissionFix>();
        foreach (var diagnostic in diagnostics)
        {
            var props = CodeFixProperties.TryParse(diagnostic.Properties);
            if (props is null)
                continue;

            var fix = fixes.Find(f =>
                f.RawTableName.IsSameName(props.TableName) && f.TableNamespace.IsSameName(props.TableNamespace));
            if (fix is null)
            {
                // Resolve table name using C#-like namespace resolution
                var resolved = ResolveTableNameForFix(props.TableName, props.TableNamespace, compilationUnit);
                fix = new TablePermissionFix(resolved, props.TableName, props.TableNamespace);
                fixes.Add(fix);
            }

            fix.PermissionChars = PermissionSyntaxHelper.NormalizePermissionString(fix.PermissionChars, props.PermissionChar);
        }

        return fixes;
    }

    private static string GetTitle(List<TablePermissionFix> fixes)
    {
        if (fixes.Count == 1 && fixes[0].PermissionChars.Length == 1)
        {
            return string.Format(
                ApplicationCopAnalyzers.TableDataAccessRequiresPermissionsCodeAction,
                fixes[0].PermissionChars,
                fixes[0].DisplayName);
        }

        return string.Format(
            ApplicationCopAnalyzers.TableDataAccessRequiresPermissionsMultipleCodeAction,
            string.Join(", ", fixes.Select(f => $"{f.DisplayName} = {f.PermissionChars}")));
    }

    /// <summary>
    /// Applies one char at a time on the evolving document: <see cref="ApplyFix"/> re-finds the
    /// object and merges or inserts in canonical order, so the edits compose without new merge logic.
    /// </summary>
    private static async Task<Document> ApplyFixes(Document document,
        (SyntaxKind Kind, string? Name) objectIdentity, List<TablePermissionFix> fixes,
        CancellationToken cancellationToken)
    {
        foreach (var fix in fixes)
        {
            foreach (var permissionChar in fix.PermissionChars)
            {
                document = await ApplyFix(document, objectIdentity, fix.Resolved, fix.RawTableName,
                    permissionChar, cancellationToken).ConfigureAwait(false);
            }
        }

        return document;
    }

    /// <summary>
    /// Re-runs the AC0031 analyzer and keeps the diagnostics inside the object's span in this document,
    /// so the object-wide action fixes exactly what the rule reports (pragma suppressions included)
    /// without duplicating its detection. It runs only when the action is applied, never while registering.
    /// </summary>
    private static async Task<Document> ApplyObjectWideFix(Document document,
        (SyntaxKind Kind, string? Name) objectIdentity, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        if (semanticModel is null)
            return document;

        var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var objectSyntax = FindObjectByIdentity(root, objectIdentity);
        if (objectSyntax is null)
            return document;

        var compilationWithAnalyzers = new CompilationWithAnalyzers(
            semanticModel.Compilation,
            ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzers.TableDataAccessRequiresPermissions()),
            document.Project.AnalyzerOptions,
            cancellationToken);

        // The whole compilation is analyzed: the per-tree GetAnalyzerSemanticDiagnosticsAsync returns
        // no diagnostics at all for an AL compilation (with or without a filter span).
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        // The driver keeps pragma-suppressed diagnostics, flagged IsSuppressed; the editor hides them.
        var objectDiagnostics = diagnostics.Where(d =>
            !d.IsSuppressed
            && d.Id == DiagnosticDescriptors.TableDataAccessRequiresPermissions.Id
            && d.Location.SourceTree == semanticModel.SyntaxTree
            && objectSyntax.Span.Contains(d.Location.SourceSpan));

        var fixes = CollectFixes(objectDiagnostics, objectSyntax.FirstAncestorOrSelf<CompilationUnitSyntax>());
        if (fixes.Count == 0)
            return document;

        return await ApplyFixes(document, objectIdentity, fixes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> ApplyFix(Document document,
        (SyntaxKind Kind, string? Name) objectIdentity,
        ResolvedTableName resolved, string rawTableName,
        char permissionChar, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        if (root is null)
            return document;

        // Re-find the object in the current tree to avoid stale node references.
        // BatchFixer applies fixes sequentially, modifying the document between each.
        var objectSyntax = FindObjectByIdentity(root, objectIdentity);
        if (objectSyntax is null)
            return document;

        var propertyList = objectSyntax.PropertyList;
        PropertyListSyntax newPropertyList;

        var permissionsProperty = FindPermissionsProperty(propertyList);

        if (permissionsProperty is null)
        {
            newPropertyList = AddNewPermissionsProperty(propertyList, resolved, permissionChar);
        }
        else
        {
            newPropertyList = UpdateExistingPermissionsProperty(propertyList, permissionsProperty, resolved, rawTableName, permissionChar);
        }

        var newRoot = root.ReplaceNode(propertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }

    private static (SyntaxKind Kind, string? Name) GetObjectIdentity(ObjectSyntax objectSyntax)
    {
        var name = objectSyntax.Name?.Identifier.ValueText;
        return (objectSyntax.Kind, name);
    }

    private static ObjectSyntax? FindObjectByIdentity(SyntaxNode root,
        (SyntaxKind Kind, string? Name) identity)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node is ObjectSyntax obj
                && obj is not ApplicationObjectExtensionSyntax
                && obj.Kind == identity.Kind
                && obj.Name?.Identifier.ValueText.IsSameName(identity.Name) == true)
            {
                return obj;
            }
        }

        return null;
    }

    private static PropertyListSyntax AddNewPermissionsProperty(
        PropertyListSyntax propertyList, ResolvedTableName resolved, char permissionChar)
    {
        var permissionEntry = PermissionSyntaxHelper.CreatePermissionSyntax(
            resolved.TableName, resolved.QualifyingNamespace, permissionChar.ToString());
        var permissionValue = SyntaxFactory.PermissionPropertyValue()
            .AddPermissionProperties(permissionEntry);
        var property = SyntaxFactory.Property(
            EnumProvider.PropertyKind.Permissions,
            (PropertyValueSyntax)permissionValue);

        return propertyList.AddProperties(property);
    }

    private static PropertyListSyntax UpdateExistingPermissionsProperty(
        PropertyListSyntax propertyList, PropertySyntax permissionsProperty,
        ResolvedTableName resolved, string rawTableName, char permissionChar)
    {
        if (permissionsProperty.Value is not PermissionPropertyValueSyntax permissionValue)
            return propertyList;

        // FindExistingEntry compares against GetObjectNameFromPermission output (always unquoted).
        // Construct the search name in the same format: raw name for simple, namespace.raw for qualified.
        var searchName = resolved.QualifyingNamespace is not null
            ? $"{resolved.QualifyingNamespace}.{rawTableName}"
            : rawTableName;

        var existingEntry = PermissionSyntaxHelper.FindExistingEntry(permissionValue, searchName);
        PermissionPropertyValueSyntax newPermissionValue;

        if (existingEntry is not null)
        {
            // Table already listed: merge the missing permission char
            newPermissionValue = MergePermissionChar(permissionValue, existingEntry, permissionChar);
        }
        else
        {
            // Table not listed: add a new entry
            newPermissionValue = AddNewEntry(permissionValue, resolved, rawTableName, permissionChar);
        }

        var newProperty = permissionsProperty.WithValue(newPermissionValue);
        return propertyList.WithProperties(propertyList.Properties.Replace(permissionsProperty, newProperty));
    }

    private static PermissionPropertyValueSyntax MergePermissionChar(
        PermissionPropertyValueSyntax permissionValue,
        PermissionSyntax existingEntry, char permissionChar)
    {
        var currentPermissions = existingEntry.Permissions.ValueText ?? string.Empty;
        var mergedPermissions = PermissionSyntaxHelper.NormalizePermissionString(currentPermissions, permissionChar);
        var newPermissionsToken = SyntaxFactory.Identifier(mergedPermissions);
        var updatedEntry = existingEntry.WithPermissions(newPermissionsToken);

        return permissionValue.WithPermissionProperties(
            permissionValue.PermissionProperties.Replace(existingEntry, updatedEntry));
    }

    private static PermissionPropertyValueSyntax AddNewEntry(
        PermissionPropertyValueSyntax permissionValue, ResolvedTableName resolved,
        string rawTableName, char permissionChar)
    {
        var permissions = permissionValue.PermissionProperties;
        var isMultiLine = PermissionSyntaxHelper.IsMultiLineFormat(permissionValue);

        var newEntry = PermissionSyntaxHelper.CreatePermissionSyntax(
            resolved.TableName, resolved.QualifyingNamespace, permissionChar.ToString());
        var insertIndex = PermissionSyntaxHelper.FindInsertionIndex(permissions, newEntry);

        SeparatedSyntaxList<PermissionSyntax> newPermissions;
        if (isMultiLine)
        {
            // Only add indentation trivia when inserting at index > 0.
            // Index 0 is on the same line as "Permissions = " and needs no indentation.
            if (insertIndex > 0)
            {
                var indentation = PermissionSyntaxHelper.GetEntryIndentation(permissionValue);
                newEntry = newEntry.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(indentation));
            }
            return PermissionSyntaxHelper.InsertIntoMultiLineList(permissionValue, permissions, insertIndex, newEntry);
        }
        else
        {
            newPermissions = permissions.Insert(insertIndex, newEntry);
        }

        return permissionValue.WithPermissionProperties(newPermissions);
    }

    private static PropertySyntax? FindPermissionsProperty(PropertyListSyntax? propertyList)
    {
        if (propertyList is null)
            return null;

        return propertyList.Properties
            .OfType<PropertySyntax>()
            .FirstOrDefault(p => p.Name is { Identifier.ValueText: { } valueText } &&
                SemanticFacts.IsSameName(valueText, nameof(PropertyKind.Permissions)));
    }

    private static ResolvedTableName ResolveTableNameForFix(string tableName, string tableNamespace,
        CompilationUnitSyntax? compilationUnit)
    {
        if (compilationUnit is null || string.IsNullOrEmpty(tableNamespace))
            return new ResolvedTableName(tableName.QuoteIdentifierIfNeededWithReflection(), null);

        var fileNamespace = compilationUnit.GetFileNamespace();
        var importedNamespaces = compilationUnit.GetImportedNamespaces();

        return PermissionTableNameResolver.ResolveTableNameParts(
            tableName, tableNamespace, fileNamespace, importedNamespaces);
    }
}
