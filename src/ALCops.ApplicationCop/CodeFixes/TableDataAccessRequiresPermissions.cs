using System.Collections.Immutable;
using ALCops.Common.Permissions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
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

    private class TableDataAccessRequiresPermissionsCodeAction : CodeAction.DocumentChangeAction
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

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.TableDataAccessRequiresPermissions.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot,
        TextSpan span, Document document)
    {
        var diagnostic = ctx.Diagnostics[0];
        var props = CodeFixProperties.TryParse(diagnostic.Properties);
        if (props is null)
            return;

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

        // Resolve table name using C#-like namespace resolution
        var compilationUnit = objectSyntax.FirstAncestorOrSelf<CompilationUnitSyntax>();
        var resolvedTableName = ResolveTableNameForFix(props.TableName, props.TableNamespace, compilationUnit);

        ctx.RegisterCodeFix(
            CreateCodeAction(objectIdentity, resolvedTableName, props.PermissionChar, document, generateFixAll: true),
            diagnostic);
    }

    private static TableDataAccessRequiresPermissionsCodeAction CreateCodeAction(
        (SyntaxKind Kind, string? Name) objectIdentity, string tableName, char permissionChar,
        Document document, bool generateFixAll)
    {
        var title = string.Format(
            ApplicationCopAnalyzers.TableDataAccessRequiresPermissionsCodeAction,
            permissionChar,
            tableName);

        return new TableDataAccessRequiresPermissionsCodeAction(
            title,
            ct => ApplyFix(document, objectIdentity, tableName, permissionChar, ct),
            nameof(TableDataAccessRequiresPermissionsCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document,
        (SyntaxKind Kind, string? Name) objectIdentity,
        string tableName, char permissionChar, CancellationToken cancellationToken)
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
            newPropertyList = AddNewPermissionsProperty(propertyList, tableName, permissionChar);
        }
        else
        {
            newPropertyList = UpdateExistingPermissionsProperty(propertyList, permissionsProperty, tableName, permissionChar);
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
        PropertyListSyntax propertyList, string tableName, char permissionChar)
    {
        var permissionEntry = PermissionSyntaxHelper.CreatePermissionSyntax(
            tableName, permissionChar.ToString());
        var permissionValue = SyntaxFactory.PermissionPropertyValue()
            .AddPermissionProperties(permissionEntry);
        var property = SyntaxFactory.Property(
            EnumProvider.PropertyKind.Permissions,
            (PropertyValueSyntax)permissionValue);

        return propertyList.AddProperties(property);
    }

    private static PropertyListSyntax UpdateExistingPermissionsProperty(
        PropertyListSyntax propertyList, PropertySyntax permissionsProperty,
        string tableName, char permissionChar)
    {
        if (permissionsProperty.Value is not PermissionPropertyValueSyntax permissionValue)
            return propertyList;

        var existingEntry = PermissionSyntaxHelper.FindExistingEntry(permissionValue, tableName);
        PermissionPropertyValueSyntax newPermissionValue;

        if (existingEntry is not null)
        {
            // Table already listed: merge the missing permission char
            newPermissionValue = MergePermissionChar(permissionValue, existingEntry, permissionChar);
        }
        else
        {
            // Table not listed: add a new entry
            newPermissionValue = AddNewEntry(permissionValue, tableName, permissionChar);
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
        PermissionPropertyValueSyntax permissionValue, string tableName, char permissionChar)
    {
        var permissions = permissionValue.PermissionProperties;
        var isSorted = PermissionSyntaxHelper.ArePermissionsSorted(permissions);
        var isMultiLine = PermissionSyntaxHelper.IsMultiLineFormat(permissionValue);
        var insertIndex = PermissionSyntaxHelper.FindInsertionIndex(permissions, tableName, isSorted);

        var newEntry = PermissionSyntaxHelper.CreatePermissionSyntax(
            tableName, permissionChar.ToString());

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

    private static string ResolveTableNameForFix(string tableName, string tableNamespace,
        CompilationUnitSyntax? compilationUnit)
    {
        if (compilationUnit is null || string.IsNullOrEmpty(tableNamespace))
            return tableName;

        var fileNamespace = PermissionTableNameResolver.GetFileNamespace(compilationUnit);
        var importedNamespaces = PermissionTableNameResolver.GetImportedNamespaces(compilationUnit);

        return PermissionTableNameResolver.ResolveTableName(
            tableName, tableNamespace, fileNamespace, importedNamespaces);
    }
}
