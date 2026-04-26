using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(TableDataAccessUnusedPermissionsCodeFixProvider))]
public sealed class TableDataAccessUnusedPermissionsCodeFixProvider : CodeFixProvider
{
#if NETSTANDARD2_1
    private sealed class CodeFixProperties
    {
        public string TableName { get; }
        public string UnusedChars { get; }
        public string UsedChars { get; }

        private CodeFixProperties(string tableName, string unusedChars, string usedChars)
        {
            TableName = tableName;
            UnusedChars = unusedChars;
            UsedChars = usedChars;
        }

        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(UnusedChars), out var unusedChars) || string.IsNullOrEmpty(unusedChars))
                return null;

            properties.TryGetValue(nameof(UsedChars), out var usedChars);

            return new CodeFixProperties(tableName, unusedChars, usedChars ?? string.Empty);
        }
    }
#endif

#if NET8_0_OR_GREATER
    private sealed record CodeFixProperties(string TableName, string UnusedChars, string UsedChars)
    {
        public static CodeFixProperties? TryParse(ImmutableDictionary<string, string>? properties)
        {
            if (properties is null)
                return null;

            if (!properties.TryGetValue(nameof(TableName), out var tableName) || string.IsNullOrEmpty(tableName))
                return null;

            if (!properties.TryGetValue(nameof(UnusedChars), out var unusedChars) || string.IsNullOrEmpty(unusedChars))
                return null;

            properties.TryGetValue(nameof(UsedChars), out var usedChars);

            return new CodeFixProperties(tableName, unusedChars, usedChars ?? string.Empty);
        }
    }
#endif

    private class TableDataAccessUnusedPermissionsCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }

        public TableDataAccessUnusedPermissionsCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.TableDataAccessUnusedPermissionsEntireEntry.Id);

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
        var permissionNode = node as PermissionSyntax
            ?? node.FirstAncestorOrSelf<PermissionSyntax>()
            ?? node.DescendantNodes().OfType<PermissionSyntax>().FirstOrDefault();
        if (permissionNode is null)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(permissionNode, props, document, generateFixAll: true),
            diagnostic);
    }

    private static TableDataAccessUnusedPermissionsCodeAction CreateCodeAction(
        PermissionSyntax permissionNode, CodeFixProperties props,
        Document document, bool generateFixAll)
    {
        var title = string.Format(
            ApplicationCopAnalyzers.TableDataAccessUnusedPermissionsCodeAction,
            props.TableName);

        return new TableDataAccessUnusedPermissionsCodeAction(
            title,
            ct => ApplyFix(document, permissionNode, props, ct),
            nameof(TableDataAccessUnusedPermissionsCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document, PermissionSyntax permissionNode,
        CodeFixProperties props, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var permissionValue = permissionNode.Parent as PermissionPropertyValueSyntax;
        if (permissionValue is null)
            return document;

        var propertySyntax = permissionValue.Parent as PropertySyntax;
        if (propertySyntax is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        SyntaxNode newRoot;

        if (string.IsNullOrEmpty(props.UsedChars))
        {
            // Entire entry is unused: remove the entry or the whole property
            newRoot = RemovePermissionEntry(root, permissionValue, propertySyntax, permissionNode);
        }
        else
        {
            // Partial chars unused: reduce the permission chars
            newRoot = ReducePermissionChars(root, permissionNode, props.UsedChars);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode RemovePermissionEntry(
        SyntaxNode root,
        PermissionPropertyValueSyntax permissionValue,
        PropertySyntax propertySyntax,
        PermissionSyntax permissionNode)
    {
        var permissions = permissionValue.PermissionProperties;

        if (permissions.Count <= 1)
        {
            // Last entry: remove the entire Permissions property
            return root.RemoveNode(propertySyntax, SyntaxRemoveOptions.KeepNoTrivia)
                ?? root;
        }

        int index = permissions.IndexOf(permissionNode);
        var newPermissions = permissions.Remove(permissionNode);

        // When removing a non-last entry, the entry that slides into its position
        // retains stale leading trivia (e.g. newline+indent from multi-line layout).
        // Normalize it to a single space so that `Permissions = <remaining entries>` reads cleanly.
        if (index < newPermissions.Count)
        {
            var shifted = newPermissions[index];
            var normalized = shifted.WithLeadingTrivia(SyntaxFactory.TriviaList());
            newPermissions = newPermissions.Replace(shifted, normalized);
        }

        var newPermissionValue = permissionValue.WithPermissionProperties(newPermissions);
        return root.ReplaceNode(permissionValue, newPermissionValue);
    }

    private static SyntaxNode ReducePermissionChars(
        SyntaxNode root,
        PermissionSyntax permissionNode,
        string usedChars)
    {
        var newPermissionsToken = SyntaxFactory.Identifier(usedChars);
        var newPermissionNode = permissionNode.WithPermissions(newPermissionsToken);
        return root.ReplaceNode(permissionNode, newPermissionNode);
    }
}
