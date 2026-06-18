using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.FormattingCop.CodeFixes;

[CodeFixProvider(nameof(UseParenthesisForMethodAssignmentCodeFix))]
public sealed class UseParenthesisForMethodAssignmentCodeFix : CodeFixProvider
{
    private class UseParenthesisForMethodAssignmentCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UseParenthesisForMethodAssignmentCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UseParenthesisForMethodAssignment.Id);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        Document document = ctx.Document;
        TextSpan span = ctx.Span;
        CancellationToken cancellationToken = ctx.CancellationToken;

        SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return;

        SyntaxNode node = syntaxRoot.FindNode(span);
        if (node is not AssignmentStatementSyntax assignment)
            return;

        ctx.RegisterCodeFix(CreateCodeAction(assignment, document, generateFixAll: true), ctx.Diagnostics[0]);
    }

    private static UseParenthesisForMethodAssignmentCodeAction CreateCodeAction(
        AssignmentStatementSyntax assignment, Document document, bool generateFixAll)
    {
        return new UseParenthesisForMethodAssignmentCodeAction(
            FormattingCopAnalyzers.UseParenthesisForMethodAssignmentCodeAction,
            ct => UseParenthesisForMethodCall(document, assignment, ct),
            nameof(UseParenthesisForMethodAssignmentCodeFix),
            generateFixAll);
    }

    private static async Task<Document> UseParenthesisForMethodCall(
        Document document, AssignmentStatementSyntax assignment, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        CodeExpressionSyntax target = assignment.Target.WithoutTrivia();
        CodeExpressionSyntax argument = assignment.Source.WithoutTrivia();

        InvocationExpressionSyntax invocation = SyntaxFactory.InvocationExpression(
            target,
            SyntaxFactory.ArgumentList().AddArguments(argument));

        ExpressionStatementSyntax newStatement = SyntaxFactory
            .ExpressionStatement(invocation, assignment.SemicolonToken)
            .WithLeadingTrivia(assignment.GetLeadingTrivia());

        SyntaxNode newRoot = root.ReplaceNode(assignment, newStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
