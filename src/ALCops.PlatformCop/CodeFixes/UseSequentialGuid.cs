using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(UseSequentialGuidCodeFixProvider))]
public sealed class UseSequentialGuidCodeFixProvider : CodeFixProvider
{
    private class UseSequentialGuidCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public UseSequentialGuidCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UseSequentialGuid.Id);

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

    private static void RegisterInstanceCodeFix(
        CodeFixContext ctx, SyntaxNode syntaxRoot, TextSpan span, Document document)
    {
        SyntaxNode node = syntaxRoot.FindNode(span);

        ctx.RegisterCodeFix(
            CreateCodeAction(node, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static UseSequentialGuidCodeAction CreateCodeAction(
        SyntaxNode node, Document document, bool generateFixAll)
    {
        return new UseSequentialGuidCodeAction(
            PlatformCopAnalyzers.UseSequentialGuidCodeAction,
            ct => ApplyFix(document, node, ct),
            nameof(UseSequentialGuidCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ApplyFix(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node is not InvocationExpressionSyntax invocation)
            return document;

        // Build the replacement invocation with CreateSequentialGuid
        // Handle both forms: CreateGuid() and Guid.CreateGuid()
        InvocationExpressionSyntax newInvocation;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Guid.CreateGuid() → Guid.CreateSequentialGuid()
            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                memberAccess.Expression,
                "CreateSequentialGuid");

            newInvocation = SyntaxFactory.InvocationExpression(newMemberAccess)
                .WithTriviaFrom(invocation);
        }
        else
        {
            // CreateGuid() → Guid.CreateSequentialGuid()
            // CreateSequentialGuid requires the Guid. qualifier
            var guidQualified = SyntaxFactory.MemberAccessExpression(
                SyntaxFactory.IdentifierName("Guid"),
                "CreateSequentialGuid");

            newInvocation = SyntaxFactory.InvocationExpression(guidQualified)
                .WithTriviaFrom(invocation);
        }

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
