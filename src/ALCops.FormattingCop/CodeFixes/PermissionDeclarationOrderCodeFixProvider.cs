using System.Collections.Immutable;
using ALCops.Common.Permissions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.FormattingCop.CodeFixes;

[CodeFixProvider(nameof(PermissionDeclarationOrderCodeFixProvider))]
public sealed class PermissionDeclarationOrderCodeFixProvider : CodeFixProvider
{
    private class PermissionDeclarationOrderCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }

        public PermissionDeclarationOrderCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PermissionDeclarationOrder.Id);

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
        var node = syntaxRoot.FindNode(span);
        var propertySyntax = node as PropertySyntax
            ?? node.FirstAncestorOrSelf<PropertySyntax>()
            ?? node.DescendantNodes().OfType<PropertySyntax>().FirstOrDefault();
        if (propertySyntax is null)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(propertySyntax, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static PermissionDeclarationOrderCodeAction CreateCodeAction(
        PropertySyntax propertySyntax, Document document, bool generateFixAll)
    {
        return new PermissionDeclarationOrderCodeAction(
            FormattingCopAnalyzers.PermissionDeclarationOrderCodeAction,
            ct => ApplyFix(document, propertySyntax, ct),
            nameof(PermissionDeclarationOrderCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document,
        PropertySyntax propertySyntax, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (propertySyntax.Value is not PermissionPropertyValueSyntax permissionValue)
            return document;

        var permissions = permissionValue.PermissionProperties;
        if (permissions.Count <= 1)
            return document;

        var sorted = PermissionSyntaxHelper.GetSortedPermissions(permissions);
        var indentation = PermissionSyntaxHelper.GetEntryIndentation(permissionValue);

        PermissionPropertyValueSyntax newPermissionValue;
        if (sorted.Count >= 2)
        {
            // Always use multi-line format for 2+ entries
            newPermissionValue = PermissionSyntaxHelper.BuildMultiLinePermissionValue(sorted, indentation);
        }
        else
        {
            newPermissionValue = permissionValue;
        }

        var newProperty = propertySyntax.WithValue(newPermissionValue);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(propertySyntax, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }
}
