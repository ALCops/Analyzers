using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.FormattingCop.CodeFixes;

[CodeFixProvider(nameof(UseParenthesisForFunctionCallCodeFix))]
public sealed class UseParenthesisForFunctionCallCodeFix : CodeFixProvider
{
    private class UseParenthesisForFunctionCallCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UseParenthesisForFunctionCallCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UseParenthesisForFunctionCall.Id);

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

    private static UseParenthesisForFunctionCallCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new UseParenthesisForFunctionCallCodeAction(
            FormattingCopAnalyzers.UseParenthesisForFunctionCallCodeAction,
            ct => AddParenthesisForFunction(document, node, ct),
            nameof(UseParenthesisForFunctionCallCodeFix),
            generateFixAll);
    }

    private static async Task<Document> AddParenthesisForFunction(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        SyntaxNode newNode;
        switch (node.Kind)
        {
            case var _ when node.Kind == EnumProvider.SyntaxKind.IdentifierName:
                var identifierValue = node.GetIdentifierOrLiteralValue() ?? string.Empty;
                if (string.IsNullOrEmpty(identifierValue))
                    goto default;

                newNode = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(identifierValue)).WithTriviaFrom(node);
                break;

            case var _ when node.Kind == EnumProvider.SyntaxKind.MemberAccessExpression:
                newNode = SyntaxFactory.InvocationExpression((MemberAccessExpressionSyntax)node);
                break;

            default:
                return document;
        }

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}