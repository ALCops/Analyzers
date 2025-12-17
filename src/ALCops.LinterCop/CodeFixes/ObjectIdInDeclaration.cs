using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixes;

[CodeFixProvider(nameof(ObjectIdInDeclarationCodeFixProvider))]
public sealed class ObjectIdInDeclarationCodeFixProvider : CodeFixProvider
{
    private class ObjectIdInDeclarationCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;

        public ObjectIdInDeclarationCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            this.SetPropertyIfExists("SupportsFixAll", generateFixAll);
            this.SetPropertyIfExists("FixAllSingleInstanceTitle", string.Empty);
            this.SetPropertyIfExists("FixAllTitle", Title);
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ObjectIdInDeclaration.Id);

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

        var diagnostic = ctx.Diagnostics
            .FirstOrDefault(d => d.Id == DiagnosticDescriptors.ObjectIdInDeclaration.Id);

        if (diagnostic is null || !diagnostic.Properties.TryGetValue("IdentifierName", out var replacementIdentifierName) || string.IsNullOrEmpty(replacementIdentifierName))
            return;

        ctx.RegisterCodeFix(CreateCodeAction(node, document, SyntaxFactory.IdentifierName(replacementIdentifierName), true), ctx.Diagnostics[0]);
    }

    private static ObjectIdInDeclarationCodeAction CreateCodeAction(SyntaxNode node, Document document, IdentifierNameSyntax replacementIdentifierName, bool generateFixAll)
    {
        return new ObjectIdInDeclarationCodeAction(
            LinterCopAnalyzers.ObjectIdInDeclarationActionTitle,
            ct => ReplaceObjectIdWithObjectName(document, node, replacementIdentifierName, ct),
            nameof(ObjectIdInDeclarationCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ReplaceObjectIdWithObjectName(Document document, SyntaxNode node, IdentifierNameSyntax replacementIdentifierName, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (node is not ObjectNameOrIdSyntax objectNameOrIdSyntax)
            return document;

        var newObjectNameOrIdSyntax = SyntaxFactory.ObjectNameOrId(replacementIdentifierName);
        var newRoot = syntaxRoot.ReplaceNode(objectNameOrIdSyntax, newObjectNameOrIdSyntax);
        return document.WithSyntaxRoot(newRoot);
    }
}