using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider("ApplicationAreaRedundancyCodeFixProvider")]
public sealed class ApplicationAreaRedundancyCodeFixProvider : CodeFixProvider
{
    private static readonly string ApplicationAreaName =
        EnumProvider.PropertyKind.ApplicationArea.ToString();

    private class ApplicationAreaRedundancyCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public ApplicationAreaRedundancyCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ApplicationAreaRedundancy.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
         WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
        RegisterInstanceCodeFix(ctx, syntaxRoot, span, document);
    }

    private static void RegisterInstanceCodeFix(CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span, getInnermostNodeForTie: true);
        ctx.RegisterCodeFix((CodeAction)CreateCodeAction(node, document, true), ctx.Diagnostics[0]);
    }

    private static ApplicationAreaRedundancyCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new ApplicationAreaRedundancyCodeAction(
            LinterCopAnalyzers.ApplicationAreaRedundancyCodeAction,
            ct => RemoveRedundantApplicationArea(document, node, ct),
            nameof(ApplicationAreaRedundancyCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> RemoveRedundantApplicationArea(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node.Parent is not PropertyListSyntax originalPropertyList)
            return document;

        var ApplicationAreaProperty = originalPropertyList.GetProperty(ApplicationAreaName);
        if (ApplicationAreaProperty is null)
            return document;

        var newProperties = originalPropertyList.Properties.Remove(ApplicationAreaProperty);
        var newPropertyList = originalPropertyList.WithProperties(newProperties);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(originalPropertyList, newPropertyList);
        return document.WithSyntaxRoot(newRoot);
    }
}