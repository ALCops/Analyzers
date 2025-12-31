using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(OperatorAndPlaceholderInFilterExpressionCodeFix))]
public sealed class OperatorAndPlaceholderInFilterExpressionCodeFix : CodeFixProvider
{
    private const string StrSubstNoMethodName = "StrSubstNo";
    private static readonly string SetFilterMethodName = "SetFilter";

    private class OperatorAndPlaceholderInFilterExpressionCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;

        public OperatorAndPlaceholderInFilterExpressionCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;

        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.OperatorAndPlaceholderInFilterExpression.Id);

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

    private static OperatorAndPlaceholderInFilterExpressionCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new OperatorAndPlaceholderInFilterExpressionCodeAction(
            PlatformCopAnalyzers.OperatorAndPlaceholderInFilterExpressionCodeAction,
            ct => WrapFilterExpressionInStrSubstNoMethod(document, node, ct),
            nameof(OperatorAndPlaceholderInFilterExpressionCodeFix),
            generateFixAll);
    }

    private static async Task<Document> WrapFilterExpressionInStrSubstNoMethod(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node is not LiteralExpressionSyntax filterLiteral)
            return document;

        if (!TryGetInvocation(filterLiteral, out InvocationExpressionSyntax invocation))
            return document;

        if (!IsSetFilterInvocation(invocation))
            return document;

        ArgumentListSyntax? argList = invocation.ArgumentList;
        if (argList is null)
            return document;

        var args = argList.Arguments;

        // Expect method SetFilter(Field, FilterLiteral, Param1 [, Param2...])
        if (args.Count < 3)
            return document;

        // Build StrSubstNo(FilterLiteral, Param1, Param2, ...)
        InvocationExpressionSyntax strSubstNoInvocation = CreateStrSubstNoInvocation(filterLiteral, args);

        // Replace SetFilter call to: SetFilter(Field, StrSubstNo(...))
        InvocationExpressionSyntax newInvocation = ReplaceSetFilterArguments(invocation, args[0], strSubstNoInvocation);

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        return document.WithSyntaxRoot(root.ReplaceNode(invocation, newInvocation));
    }

    private static InvocationExpressionSyntax CreateStrSubstNoInvocation(
        LiteralExpressionSyntax filterLiteral,
        SeparatedSyntaxList<CodeExpressionSyntax> originalArgs)
    {
        // StrSubstNo('<filter>', Param1, Param2, ...)
        var separatedArgs = default(SeparatedSyntaxList<CodeExpressionSyntax>)
            .Add(filterLiteral);

        for (int i = 2; i < originalArgs.Count; i++)
            separatedArgs = separatedArgs.Add(originalArgs[i]);

        ArgumentListSyntax argumentList = SyntaxFactory.ArgumentList(separatedArgs);

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(StrSubstNoMethodName),
            argumentList);
    }

    private static InvocationExpressionSyntax ReplaceSetFilterArguments(
        InvocationExpressionSyntax originalInvocation,
        CodeExpressionSyntax firstArgument,
        InvocationExpressionSyntax secondArgument)
    {
        var newArgs = default(SeparatedSyntaxList<CodeExpressionSyntax>)
            .Add(firstArgument)
            .Add(secondArgument);

        ArgumentListSyntax newArgumentList = SyntaxFactory.ArgumentList(newArgs);

        return originalInvocation.WithArgumentList(newArgumentList);
    }

    private static bool TryGetInvocation(SyntaxNode? node, out InvocationExpressionSyntax invocation)
    {
        SyntaxNode? current = node;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax i)
            {
                invocation = i;
                return true;
            }

            current = current.Parent;
        }

        invocation = null!;
        return false;
    }

    private static bool IsSetFilterInvocation(InvocationExpressionSyntax invocation)
    {
        IdentifierNameSyntax identifierName = invocation.Expression.GetIdentifierNameSyntax();
        if (identifierName is null)
            return false;

        return string.Equals(
            identifierName.Identifier.Text,
            SetFilterMethodName,
            StringComparison.OrdinalIgnoreCase);
    }
}