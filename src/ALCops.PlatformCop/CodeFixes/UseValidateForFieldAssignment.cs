using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(UseValidateForFieldAssignmentCodeFixProvider))]
public sealed class UseValidateForFieldAssignmentCodeFixProvider : CodeFixProvider
{
    private class UseValidateForFieldAssignmentCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UseValidateForFieldAssignmentCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UseValidateForFieldAssignment.Id);

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

        var assignment = node.FirstAncestorOrSelf<AssignmentStatementSyntax>();
        if (assignment is null)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(assignment, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static UseValidateForFieldAssignmentCodeAction CreateCodeAction(AssignmentStatementSyntax assignment, Document document, bool generateFixAll)
    {
        return new UseValidateForFieldAssignmentCodeAction(
            PlatformCopAnalyzers.UseValidateForFieldAssignmentCodeAction,
            ct => ApplyFix(document, assignment, ct),
            nameof(UseValidateForFieldAssignmentCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(Document document, AssignmentStatementSyntax assignment, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (assignment.Target is not MemberAccessExpressionSyntax memberAccess)
            return document;

        // Build: Rec.Validate("FieldName", Value)
        var receiverExpression = memberAccess.Expression;
        var fieldIdentifier = memberAccess.Name;
        var valueExpression = assignment.Source;

        var validateMemberAccess = SyntaxFactory.MemberAccessExpression(
            receiverExpression,
            SyntaxFactory.IdentifierName("Validate"));

        var arguments = default(SeparatedSyntaxList<CodeExpressionSyntax>);
        arguments = arguments.Add(SyntaxFactory.IdentifierName(fieldIdentifier.Identifier));
        arguments = arguments.Add(valueExpression);

        var argumentList = SyntaxFactory.ArgumentList(arguments);

        var validateInvocation = SyntaxFactory.InvocationExpression(validateMemberAccess, argumentList);
        var expressionStatement = SyntaxFactory.ExpressionStatement(validateInvocation,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken))
            .WithTriviaFrom(assignment);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(assignment, expressionStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
