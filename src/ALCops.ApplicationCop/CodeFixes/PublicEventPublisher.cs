using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using ALCops.Common.Reflection;
using ALCops.Common;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(PublicEventPublisherCodeFixProvider))]
public sealed class PublicEventPublisherCodeFixProvider : CodeFixProvider
{
    private class PublicEventPublisherCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public PublicEventPublisherCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.PublicEventPublisher.Id);

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

    private static PublicEventPublisherCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new PublicEventPublisherCodeAction(
            ApplicationCopAnalyzers.PublicEventPublisherCodeAction,
            ct => ScopeMethodToLocal(document, node, ct),
            nameof(PublicEventPublisherCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ScopeMethodToLocal(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var methodDeclaration = node.Ancestors()
                                    .OfType<MethodDeclarationSyntax>()
                                    .FirstOrDefault();

        if (methodDeclaration is null)
            return document;

        if (methodDeclaration.AccessModifier.Kind == EnumProvider.SyntaxKind.LocalKeyword)
            return document;

        SyntaxToken procedureKeyword = methodDeclaration.ProcedureKeyword;

        SyntaxToken localToken =
            SyntaxFactory.Token(EnumProvider.SyntaxKind.LocalKeyword)
                         .WithLeadingTrivia(procedureKeyword.LeadingTrivia)
                         .WithTrailingTrivia(SyntaxFactory.TriviaList());

        SyntaxToken newProcedureKeyword =
            procedureKeyword.WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Space));

        MethodDeclarationSyntax newMethodDeclaration =
            methodDeclaration
                .WithAccessModifier(localToken)
                .WithProcedureKeyword(newProcedureKeyword);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}