using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(SetRangeWithFilterOperatorsCodeFix))]
public sealed class SetRangeWithFilterOperatorsCodeFix : CodeFixProvider
{
    private class SetRangeWithFilterOperatorsCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;

        public SetRangeWithFilterOperatorsCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            this.SetPropertyIfExists("SupportsFixAll", generateFixAll);
            this.SetPropertyIfExists("FixAllSingleInstanceTitle", string.Empty);
            this.SetPropertyIfExists("FixAllTitle", Title);
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.SetRangeWithFilterOperators.Id);

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
        ctx.RegisterCodeFix(CreateCodeAction(node, document, true), ctx.Diagnostics[0]);
    }

    private static SetRangeWithFilterOperatorsCodeAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new SetRangeWithFilterOperatorsCodeAction(
            PlatformCopAnalyzers.SetRangeWithFilterOperatorsCodeAction,
            ct => ReplaceSetRangeWithSetFilter(document, node, ct),
            nameof(SetRangeWithFilterOperatorsCodeFix),
            generateFixAll);
    }

    private static async Task<Document> ReplaceSetRangeWithSetFilter(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocationExpression)
            return document;

        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var newMemberAccess = SyntaxFactory.MemberAccessExpression(
            memberAccess.Expression,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName("SetFilter"));

        var newInvocation = SyntaxFactory.InvocationExpression(newMemberAccess, invocationExpression.ArgumentList);

        var newRoot = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ReplaceNode(invocationExpression, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}