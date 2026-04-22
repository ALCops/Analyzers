using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(PartialRecordsBeforeWriteOperationCodeFixProvider))]
public sealed class PartialRecordsBeforeWriteOperationCodeFixProvider : CodeFixProvider
{
    private class PartialRecordsBeforeWriteOperationCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public PartialRecordsBeforeWriteOperationCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PartialRecordsBeforeWriteOperation.Id);

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

        var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (statement is null)
            return;

        ctx.RegisterCodeFix(
            CreateCodeAction(statement, document, generateFixAll: true),
            ctx.Diagnostics[0]);
    }

    private static PartialRecordsBeforeWriteOperationCodeAction CreateCodeAction(
        ExpressionStatementSyntax statement, Document document, bool generateFixAll)
    {
        return new PartialRecordsBeforeWriteOperationCodeAction(
            PlatformCopAnalyzers.PartialRecordsBeforeWriteOperationCodeAction,
            ct => RemoveStatement(document, statement, ct),
            nameof(PartialRecordsBeforeWriteOperationCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> RemoveStatement(
        Document document, ExpressionStatementSyntax statement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot is null)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
