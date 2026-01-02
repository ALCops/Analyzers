using System.Collections.Immutable;
using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.Mef;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Workspaces;

namespace ALCops.PlatformCop.CodeFixes;

[CodeFixProvider(nameof(GuidEmptyStringComparisonCodeFix))]
public sealed class GuidEmptyStringComparisonCodeFix : CodeFixProvider
{
    private const string SystemClassName = "System";
    private const string IsNullGuidFunctionName = "IsNullGuid";

    private class GuidEmptyStringComparisonCodeAction : CodeAction.DocumentChangeAction
    {
        public override CodeActionKind Kind => CodeActionKind.QuickFix;
        public override bool SupportsFixAll { get; }
        public override string? FixAllSingleInstanceTitle => string.Empty;
        public override string? FixAllTitle => Title;


        public GuidEmptyStringComparisonCodeAction(string title,
            Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey, bool generateFixAll)
            : base(title, createChangedDocument, equivalenceKey)
        {
            SupportsFixAll = generateFixAll;
        }
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.GuidEmptyStringComparison.Id);

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

    private static GuidEmptyStringComparisonCodeAction CreateCodeAction(SyntaxNode node, Document document, bool generateFixAll)
    {
        return new GuidEmptyStringComparisonCodeAction(
            PlatformCopAnalyzers.GuidEmptyStringComparisonCodeAction,
            ct => ReplaceGuidEmptyStringComparison(document, node, ct),
            nameof(GuidEmptyStringComparisonCodeFix),
            generateFixAll);
    }

    private static async Task<Document> ReplaceGuidEmptyStringComparison(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        Task<SyntaxNode> syntaxRootTask = document.GetSyntaxRootAsync(cancellationToken);

        if (node is not BinaryExpressionSyntax comparison)
            return document;

        var isLeftEmpty = IsEmptyStringLiteral(comparison.Left);
        var isRightEmpty = IsEmptyStringLiteral(comparison.Right);
        if (isLeftEmpty == isRightEmpty)
            return document;

        var guidExpr = isLeftEmpty ? comparison.Right : comparison.Left;

        CodeExpressionSyntax replacement = CreateSystemIsNullGuidInvocation(guidExpr);

        if (comparison.OperatorToken.Kind == EnumProvider.SyntaxKind.NotEqualsToken)
        {
            replacement = PrependNotKeyword(replacement, comparison);
        }
        else
        {
            replacement = replacement.WithTriviaFrom(comparison);
        }

        var root = await syntaxRootTask.ConfigureAwait(false);
        if (root is null)
            return document;

        var newRoot = root.ReplaceNode(comparison, replacement);
        return document.WithSyntaxRoot(newRoot);
    }

    private static CodeExpressionSyntax PrependNotKeyword(CodeExpressionSyntax expr, SyntaxNode triviaSource)
    {
        // not <expr>
        SyntaxToken notToken =
            SyntaxFactory.Token(EnumProvider.SyntaxKind.NotKeyword)
                         .WithLeadingTrivia(triviaSource.GetLeadingTrivia())
                         .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var exprNoLeading = expr.WithLeadingTrivia(SyntaxFactory.TriviaList());

        return SyntaxFactory.UnaryExpression(
                EnumProvider.SyntaxKind.UnaryNotExpression,
                notToken,
                exprNoLeading)
            .WithTrailingTrivia(triviaSource.GetTrailingTrivia());
    }

    private static bool IsEmptyStringLiteral(CodeExpressionSyntax expr)
    {
        if (expr is not LiteralExpressionSyntax literal)
            return false;

        if (literal.Literal is not StringLiteralValueSyntax stringLiteral)
            return false;

        return string.IsNullOrEmpty(stringLiteral.Value.ValueText) || stringLiteral.Value.ValueText == "''";
    }

    private static InvocationExpressionSyntax CreateSystemIsNullGuidInvocation(CodeExpressionSyntax guidExpr)
    {
        // System.IsNullGuid(guidExpr)
        var systemIdentifier = CreateSystemIdentifier();
        var isNullGuidIdentifier = CreateIsNullGuidIdentifier();

        var memberAccess = SyntaxFactory.MemberAccessExpression(systemIdentifier, isNullGuidIdentifier);

        var separatedArgs = default(SeparatedSyntaxList<CodeExpressionSyntax>);
        separatedArgs = separatedArgs.Add(guidExpr);

        var argumentList = SyntaxFactory.ArgumentList(separatedArgs);

        return SyntaxFactory.InvocationExpression(memberAccess, argumentList);
    }

    private static IdentifierNameSyntax CreateSystemIdentifier() =>
        SyntaxFactory.IdentifierName(SystemClassName);

    private static IdentifierNameSyntax CreateIsNullGuidIdentifier() =>
        SyntaxFactory.IdentifierName(IsNullGuidFunctionName);
}