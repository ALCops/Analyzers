using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(ApplicationAreaOnApiPageCodeFix))]
public sealed class ApplicationAreaOnApiPageCodeFix : CodeFixProvider
{
    private const string ApplicationAreaPropertyName = "ApplicationArea";

    private class ApplicationAreaOnApiPageCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ApplicationAreaOnApiPageCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ApplicationAreaOnApiPage.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);
        ctx.RegisterCodeFix(CreateCodeAction(node, document, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static ApplicationAreaOnApiPageCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new ApplicationAreaOnApiPageCodeAction(
            PlatformCopAnalyzers.ApplicationAreaOnApiPageCodeAction,
            ct => RemoveApplicationAreaProperty(document, node, ct),
            nameof(ApplicationAreaOnApiPageCodeFix),
            generateFixAll);
    }

    private static async Task<Document> RemoveApplicationAreaProperty(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        PropertySyntax? propertySyntax = FindApplicationAreaProperty(node);
        if (propertySyntax is null)
            return document;

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.RemoveNode(propertySyntax, SyntaxRemoveOptions.KeepNoTrivia);
        return document.WithSyntaxRoot(newRoot);
    }

    private static PropertySyntax? FindApplicationAreaProperty(SyntaxNode node)
    {
        if (node is PropertySyntax p && IsApplicationAreaProperty(p))
            return p;

        foreach (var ancestor in node.AncestorsAndSelf())
        {
            if (ancestor is PropertySyntax ap && IsApplicationAreaProperty(ap))
                return ap;
        }

        return node.DescendantNodesAndSelf()
                   .OfType<PropertySyntax>()
                   .FirstOrDefault(IsApplicationAreaProperty);
    }

    private static bool IsApplicationAreaProperty(PropertySyntax propertySyntax) =>
        string.Equals(propertySyntax.Name?.Identifier.ValueText, ApplicationAreaPropertyName, StringComparison.OrdinalIgnoreCase);
}