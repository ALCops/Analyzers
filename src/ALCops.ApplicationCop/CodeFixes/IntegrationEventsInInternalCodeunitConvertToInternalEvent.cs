using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;

namespace ALCops.ApplicationCop.CodeFixes;

[CodeFixProvider(nameof(IntegrationEventInInternalCodeunitConvertToInternalEventFixProvider))]
public sealed class IntegrationEventInInternalCodeunitConvertToInternalEventFixProvider : CodeFixProvider
{
    private const string IntegrationEventAttributeName = "IntegrationEvent";
    private const string InternalEventAttributeName = "InternalEvent";

    private class IntegrationEventInInternalCodeunitAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.Refactor;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public IntegrationEventInInternalCodeunitAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.IntegrationEventInInternalCodeunit.Id);

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

    private static IntegrationEventInInternalCodeunitAction CreateCodeAction(SyntaxNode node, Document document,
        bool generateFixAll)
    {
        return new IntegrationEventInInternalCodeunitAction(
            ApplicationCopAnalyzers.IntegrationEventInInternalCodeunitChangeToInternalEventCodeAction,
            ct => ConvertIntegrationEventToInternalEvent(document, node, ct),
            nameof(IntegrationEventInInternalCodeunitConvertToInternalEventFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ConvertIntegrationEventToInternalEvent(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        var methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
            return document;

        MemberAttributeSyntax? attribute =
            methodDeclaration.Attributes.FirstOrDefault(attr =>
                attr.Name is IdentifierNameSyntax name &&
                name.Identifier.ValueText.IsSameName(IntegrationEventAttributeName));

        if (attribute is null)
            return document;

        var newName = SyntaxFactory.IdentifierName(InternalEventAttributeName)
            .WithTriviaFrom(attribute.Name);

        var newAttribute = attribute.WithName(newName);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(attribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }
}