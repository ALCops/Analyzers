using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(JsonTokenJPathUsesDoubleQuotesCodeFix))]
public sealed class JsonTokenJPathUsesDoubleQuotesCodeFix : CodeFixProvider
{
    private class JsonTokenJPathUsesDoubleQuotesCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;


        public JsonTokenJPathUsesDoubleQuotesCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.JsonTokenJPathUsesDoubleQuotes.Id);

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

    private static JsonTokenJPathUsesDoubleQuotesCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new JsonTokenJPathUsesDoubleQuotesCodeAction(
            PlatformCopAnalyzers.JsonTokenJPathUsesDoubleQuotesCodeAction,
            ct => ReplaceDoubleQoutesWithTwoSingleQoutes(document, node, ct),
            nameof(JsonTokenJPathUsesDoubleQuotesCodeFix),
            generateFixAll);
    }

    private static async Task<Document> ReplaceDoubleQoutesWithTwoSingleQoutes(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        // Find the string literal node that contains the diagnostic span
        var oldLiteral =
            node.DescendantNodes()
                .OfType<StringLiteralValueSyntax>()
                .FirstOrDefault();

        if (oldLiteral is null)
            return document;

        SyntaxToken oldToken = oldLiteral.Value;

        string oldText = oldToken.Text;
        if (string.IsNullOrEmpty(oldText) || oldText.IndexOf('"') < 0)
            return document;

        string newText = oldText.Replace("\"", "''", StringComparison.Ordinal);

        SyntaxToken newToken = SyntaxFactory.ParseToken(newText);
        newToken = newToken
            .WithLeadingTrivia(oldToken.LeadingTrivia)
            .WithTrailingTrivia(oldToken.TrailingTrivia);

        var newLiteral = SyntaxFactory.StringLiteralValue(newToken);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(oldLiteral, newLiteral);
        return document.WithSyntaxRoot(newRoot);
    }
}