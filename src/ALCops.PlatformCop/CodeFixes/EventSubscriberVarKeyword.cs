using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(EventSubscriberVarKeywordCodeFix))]
public sealed class EventSubscriberVarKeywordCodeFix : CodeFixProvider
{
    private class EventSubscriberVarKeywordCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;


        public EventSubscriberVarKeywordCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;

        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.EventSubscriberVarKeyword.Id);

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

    private static EventSubscriberVarKeywordCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new EventSubscriberVarKeywordCodeAction(
            PlatformCopAnalyzers.EventSubscriberVarKeywordCodeAction,
            ct => AddVarKeywordToParameter(document, node, ct),
            nameof(EventSubscriberVarKeywordCodeFix),
            generateFixAll);
    }

    private static async Task<Document> AddVarKeywordToParameter(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var parameter = node
                        .AncestorsAndSelf()
                        .OfType<ParameterSyntax>()
                        .FirstOrDefault();
        if (parameter is null)
            return document;

        if (parameter.VarKeyword.Kind == EnumProvider.SyntaxKind.VarKeyword)
            return document;

        var varKeyword =
            SyntaxFactory.Token(EnumProvider.SyntaxKind.VarKeyword)
                         .WithTrailingTrivia(SyntaxFactory.Space);

        var newParameter =
            SyntaxFactory.Parameter(
                varKeyword,
                parameter.Name,
                parameter.ColonToken,
                parameter.Type)
            .WithTriviaFrom(parameter);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(parameter, newParameter);
        return document.WithSyntaxRoot(newRoot);
    }
}