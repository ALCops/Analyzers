using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.LinterCop.CodeFixer;

[CodeFixProvider(nameof(RecordInstanceIsolationLevelCodeFixProvider))]
public sealed class RecordInstanceIsolationLevelCodeFixProvider : CodeFixProvider
{
    private const string ReadIsolationMethodName = "ReadIsolation";
    private const string IsolationLevelEnumName = "IsolationLevel";
    private const string IsolationLevelEnumValue = "UpdLock";

    private class RecordInstanceIsolationLevelCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;

        public RecordInstanceIsolationLevelCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            this.SetPropertyIfExists("SupportsFixAll", generateFixAll);
            this.SetPropertyIfExists("FixAllSingleInstanceTitle", string.Empty);
            this.SetPropertyIfExists("FixAllTitle", Title);
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.RecordInstanceIsolationLevel.Id);

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

    private static RecordInstanceIsolationLevelCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new RecordInstanceIsolationLevelCodeAction(
            LinterCopAnalyzers.RecordInstanceIsolationLevelCodeActionTitle,
            ct => ReplaceLockTableWithReadIsolation(document, node, ct),
            nameof(RecordInstanceIsolationLevelCodeFixProvider),
            generateFixAll);
    }

    private static async Task<Document> ReplaceLockTableWithReadIsolation(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (node is not InvocationExpressionSyntax invocationExpression)
            return document;

        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var enumMemberAccess = SyntaxFactory.OptionAccessExpression(
            SyntaxFactory.IdentifierName(IsolationLevelEnumName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonColonToken),
            SyntaxFactory.IdentifierName(IsolationLevelEnumValue));

        var argumentList = SyntaxFactory.ArgumentList(
            new SeparatedSyntaxList<CodeExpressionSyntax>().Add(enumMemberAccess));

        var newMemberAccess = SyntaxFactory.MemberAccessExpression(
            memberAccess.Expression,
            SyntaxFactory.Token(EnumProvider.SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(ReadIsolationMethodName));

        var newInvocation = SyntaxFactory.InvocationExpression(newMemberAccess, argumentList);

        var newRoot = syntaxRoot.ReplaceNode(invocationExpression, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}