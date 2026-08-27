using System.Collections.Immutable;
using ALCops.FormattingCop.Analyzers;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.FormattingCop.CodeFixes;

[CodeFixProvider(nameof(PermissionValuesShouldBeLowercaseCodeFixProvider))]
public sealed class PermissionValuesShouldBeLowercaseCodeFixProvider : CodeFixProvider
{
    private sealed class PermissionValuesShouldBeLowercaseCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }

        public PermissionValuesShouldBeLowercaseCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PermissionValuesShouldBeLowercase.Id);

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

    private static PermissionValuesShouldBeLowercaseCodeAction CreateCodeAction(
        PropertySyntax propertySyntax, Document document, bool generateFixAll)
    {
        return new PermissionValuesShouldBeLowercaseCodeAction(
            FormattingCopAnalyzers.PermissionValuesShouldBeLowercaseCodeAction,
            ct => ApplyFix(document, propertySyntax, ct),
            nameof(PermissionValuesShouldBeLowercaseCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document,
        PropertySyntax propertySyntax, CancellationToken cancellationToken)
    {
        if (propertySyntax.Value is not PermissionPropertyValueSyntax permissionValue)
            return document;

        var tokensToFix = new List<SyntaxToken>();
        foreach (var permission in permissionValue.PermissionProperties)
        {
            var token = permission.Permissions;
            if (PermissionValuesShouldBeLowercase.ContainsUppercase(token.Text))
                tokensToFix.Add(token);
        }

        if (tokensToFix.Count == 0)
            return document;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceTokens(tokensToFix, (original, _) =>
            SyntaxFactory.Identifier(original.Text.ToLowerInvariant())
                .WithTriviaFrom(original));

        return document.WithSyntaxRoot(newRoot);
    }
}
