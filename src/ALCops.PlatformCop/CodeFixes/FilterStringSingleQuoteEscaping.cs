using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(FilterStringSingleQuoteEscapingCodeFix))]
public sealed class FilterStringSingleQuoteEscapingCodeFix : CodeFixProvider
{
    private const string InvalidNotEmptyFilterLiteralTokenText = "'<>'''";
    private const string NonEmptyFilterStringLiteralText = "<>''";

    private class FilterStringSingleQuoteEscapingCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public FilterStringSingleQuoteEscapingCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.FilterStringSingleQuoteEscaping.Id);

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

    private static FilterStringSingleQuoteEscapingCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new FilterStringSingleQuoteEscapingCodeAction(
            PlatformCopAnalyzers.FilterStringSingleQuoteEscapingCodeAction,
            ct => FixSingleQuoteEscaping(document, node, ct),
            nameof(FilterStringSingleQuoteEscapingCodeFix),
            generateFixAll);
    }

    private static async Task<Document> FixSingleQuoteEscaping(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var stringLiteral = node
            .DescendantNodesAndSelf()
            .OfType<StringLiteralValueSyntax>()
            .FirstOrDefault();

        if (stringLiteral is null)
            return document;

        if (!string.Equals(stringLiteral.Value.ValueText, InvalidNotEmptyFilterLiteralTokenText, StringComparison.Ordinal))
            return document;

        var fixedStringLiteral =
            SyntaxFactory.StringLiteralValue(
                SyntaxFactory.Literal(NonEmptyFilterStringLiteralText));

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(stringLiteral, fixedStringLiteral);
        return document.WithSyntaxRoot(newRoot);
    }
}